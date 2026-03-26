using System;
using System.IO.Pipes;
using System.Text;

namespace DiscordIntegration
{
    /// <summary>
    /// Handles the low-level Discord IPC pipe connection and message framing.
    /// Discord uses a named pipe at discord-ipc-0 with a simple 8-byte header protocol.
    /// </summary>
    public class EditorDiscordIPC
    {
        private NamedPipeClientStream _pipe;

        public bool IsConnected { get; private set; }

        // ── Connection ──────────────────────────────────────────────────────────

        /// <summary>
        /// Connects to Discord and sends the initial handshake.
        /// </summary>
        public bool Connect(string appId)
        {
            try
            {
                _pipe = new NamedPipeClientStream(".", "discord-ipc-0", PipeDirection.InOut, PipeOptions.None);
                _pipe.Connect(2000);

                SendFrame(0, $"{{\"v\":1,\"client_id\":\"{appId}\"}}");
                ReadFrame();

                IsConnected = true;
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Editor Discord Integration] Connection failed: {e.Message}");
                IsConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            try { _pipe?.Close(); _pipe?.Dispose(); } catch { }
            IsConnected = false;
        }

        // ── Activity ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a SET_ACTIVITY command to Discord.
        /// All parameters are optional — pass null to omit them.
        /// </summary>
        public void SetActivity(string details, string state, string largeImageKey, string largeImageText, DateTimeOffset? startTimestamp = null)
        {
            if (!IsConnected) return;

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"cmd\":\"SET_ACTIVITY\",");
            sb.Append($"\"args\":{{\"pid\":{System.Diagnostics.Process.GetCurrentProcess().Id},\"activity\":{{");
            
            if (startTimestamp.HasValue)
                sb.Append($"\"timestamps\":{{\"start\":{startTimestamp.Value.ToUnixTimeSeconds()}}},");

            if (!string.IsNullOrEmpty(details)) sb.Append($"\"details\":\"{Esc(details)}\",");
            if (!string.IsNullOrEmpty(state)) sb.Append($"\"state\":\"{Esc(state)}\",");

            sb.Append("\"assets\":{");
            if (!string.IsNullOrEmpty(largeImageKey))
            {
                sb.Append($"\"large_image\":\"{Esc(largeImageKey)}\"");
                if (!string.IsNullOrEmpty(largeImageText))
                    sb.Append($",\"large_image_text\":\"{Esc(largeImageText)}\"");
            }
            sb.Append("}");

            sb.Append($"}}}},\"nonce\":\"{Guid.NewGuid()}\"}}");

            try
            {
                SendFrame(1, sb.ToString()); // Opcode 1 = FRAME
                ReadFrame();
            }
            catch
            {
                IsConnected = false;
            }
        }

        // ── Internal helpers ────────────────────────────────────────────────────
        private void SendFrame(int opcode, string json)
        {
            var data = Encoding.UTF8.GetBytes(json);
            var buf = new byte[8 + data.Length];
            BitConverter.GetBytes(opcode).CopyTo(buf, 0);
            BitConverter.GetBytes(data.Length).CopyTo(buf, 4);
            data.CopyTo(buf, 8);
            _pipe.Write(buf, 0, buf.Length);
            _pipe.Flush();
        }

        private string ReadFrame()
        {
            var header = new byte[8];
            _pipe.Read(header, 0, 8);
            var length = BitConverter.ToInt32(header, 4);
            var data = new byte[length];
            _pipe.Read(data, 0, length);
            return Encoding.UTF8.GetString(data);
        }

        private static string Esc(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}