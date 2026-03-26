using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace DiscordIntegration
{
    /// <summary>
    /// Editor window for configuring and controlling Discord Rich Presence.
    /// Open via: Window > Editor Discord Integration
    /// </summary>
    public class EditorDiscordPresenceWindow : EditorWindow
    {
        private string _appId;
        private bool _autoConnect;
        private string _imageKey;
        private string _imageText;
        private bool _showProjectName;
        private string _customProjectName;
        private bool _showScene;
        private bool _showVersion;
        private Texture2D _previewTexture;
        private string _lastPreviewUrl;
        private bool _previewLoading;

        [MenuItem("Window/Editor Discord Integration")]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorDiscordPresenceWindow>("Editor Discord Integration");
            window.minSize = new Vector2(340, 516);
        }

        private void OnEnable()
        {
            _appId = EditorPrefs.GetString(EditorDiscordPresence.Prefs.AppId, "");
            _autoConnect = EditorPrefs.GetBool(EditorDiscordPresence.Prefs.AutoConnect, false);
            _imageKey = EditorPrefs.GetString(EditorDiscordPresence.Prefs.ImageKey, "");
            var savedText = EditorPrefs.GetString(EditorDiscordPresence.Prefs.ImageText, "");
            _imageText = string.IsNullOrEmpty(savedText) ? "Unity" : savedText;
            _showProjectName = EditorPrefs.GetBool(EditorDiscordPresence.Prefs.ShowProjectName, true);
            _customProjectName = EditorPrefs.GetString(EditorDiscordPresence.Prefs.CustomProjectName, "");
            _showScene = EditorPrefs.GetBool(EditorDiscordPresence.Prefs.ShowScene, false);
            _showVersion = EditorPrefs.GetBool(EditorDiscordPresence.Prefs.ShowVersion, false);
        }

        private void OnGUI()
        {
            // ── Header ──────────────────────────────────────────────────────────
            GUILayout.Label("Editor Discord Rich Presence", EditorStyles.boldLabel);
            DrawStatusIndicator();
            EditorGUILayout.Space(8);

            // ── Connection ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("App ID from discord.com/developers/applications", MessageType.None);
            EditorGUILayout.Space(2);
            _appId = EditorGUILayout.TextField("App ID", _appId);
            _autoConnect = EditorGUILayout.Toggle("Auto Connect on Open", _autoConnect);

            EditorGUILayout.Space(10);

            // ── Image ────────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Image", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Paste a public image URL (e.g. raw GitHub), or a Discord asset key from Rich Presence → Art Assets.", MessageType.None);
            EditorGUILayout.Space(2);
            _imageKey = EditorGUILayout.TextField("URL / Asset Key", _imageKey);
            _imageText = EditorGUILayout.TextField("Hover Text", _imageText);
            EditorGUILayout.Space(4);

            var isUrl = _imageKey.StartsWith("http://") || _imageKey.StartsWith("https://");
            if (!string.IsNullOrEmpty(_imageKey))
            {
                if (isUrl)
                {
                    FetchPreview(_imageKey);
                    if (_previewLoading)
                    {
                        EditorGUILayout.LabelField("Loading preview...", EditorStyles.miniLabel);
                    }
                    else if (_previewTexture != null)
                    {
                        var rect = GUILayoutUtility.GetRect(80, 80, GUILayout.ExpandWidth(false));
                        GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("⚠ No preview available for asset keys", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(10);

            // ── What to show ────────────────────────────────────────────────────
            EditorGUILayout.LabelField("What to show", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            _showProjectName = EditorGUILayout.Toggle("Show Project Name", _showProjectName);
            if (_showProjectName)
            {
                EditorGUI.indentLevel++;
                _customProjectName = EditorGUILayout.TextField("Custom Name", _customProjectName);
                var hint = string.IsNullOrWhiteSpace(_customProjectName)
                    ? $"Will show: \"{PlayerSettings.productName}\" (from Project Settings)"
                    : $"Will show: \"{_customProjectName.Trim()}\"";
                EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            _showScene = EditorGUILayout.Toggle("Show Current Scene", _showScene);
            _showVersion = EditorGUILayout.Toggle("Show Unity Version", _showVersion);

            if (_showScene && _showVersion)
                EditorGUILayout.LabelField("Both will appear on one line, e.g.  MainMenu  •  Unity 6000.0.30f1", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // ── Buttons ─────────────────────────────────────────────────────────
            var buttonLabel = EditorDiscordPresence.IsConnected ? "Refresh Status" : "Connect";
            if (GUILayout.Button(buttonLabel, GUILayout.Height(30)))
            {
                SavePrefs();

                if (EditorDiscordPresence.IsConnected)
                    EditorDiscordPresence.UpdateActivity();
                else
                    EditorDiscordPresence.Initialize();
            }

            if (EditorDiscordPresence.IsConnected)
            {
                if (GUILayout.Button("Disconnect"))
                    EditorDiscordPresence.Disconnect();
            }
        }

        private void OnInspectorUpdate() => Repaint();

        private void DrawStatusIndicator()
        {
            var connected = EditorDiscordPresence.IsConnected;
            var prevColor = GUI.color;
            GUI.color = connected ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            GUILayout.Label(connected ? "● Connected" : "● Not connected", EditorStyles.miniLabel);
            GUI.color = prevColor;

            if (EditorDiscordPresence.LastUpdated.HasValue)
            {
                var elapsed = DateTime.Now - EditorDiscordPresence.LastUpdated.Value;
                string timeAgo;
                if (elapsed.TotalSeconds < 60)
                    timeAgo = $"{(int)elapsed.TotalSeconds}s ago";
                else
                    timeAgo = $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s ago";

                EditorGUILayout.LabelField($"Last updated: {timeAgo}", EditorStyles.miniLabel);
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(EditorDiscordPresence.Prefs.AppId, _appId);
            EditorPrefs.SetBool(EditorDiscordPresence.Prefs.AutoConnect, _autoConnect);
            EditorPrefs.SetString(EditorDiscordPresence.Prefs.ImageKey, _imageKey);
            EditorPrefs.SetString(EditorDiscordPresence.Prefs.ImageText, _imageText);
            EditorPrefs.SetBool(EditorDiscordPresence.Prefs.ShowProjectName, _showProjectName);
            EditorPrefs.SetString(EditorDiscordPresence.Prefs.CustomProjectName, _customProjectName);
            EditorPrefs.SetBool(EditorDiscordPresence.Prefs.ShowScene, _showScene);
            EditorPrefs.SetBool(EditorDiscordPresence.Prefs.ShowVersion, _showVersion);
        }

        private void FetchPreview(string url)
        {
            if (url == _lastPreviewUrl) return;
            _lastPreviewUrl = url;
            _previewTexture = null;
            _previewLoading = true;

            var request = UnityWebRequestTexture.GetTexture(url);
            var op = request.SendWebRequest();
            op.completed += _ =>
            {
                _previewLoading = false;
                if (request.result == UnityWebRequest.Result.Success)
                    _previewTexture = DownloadHandlerTexture.GetContent(request);
                Repaint();
            };
        }
    }
}