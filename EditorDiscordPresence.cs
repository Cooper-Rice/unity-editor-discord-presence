using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiscordIntegration
{
    /// <summary>
    /// Automatically connects to Discord when Unity opens (if Auto Connect is enabled).
    /// Reads settings from EditorPrefs and sends the Rich Presence activity.
    /// Only refreshes on scene changes when "Show Current Scene" is toggled on.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorDiscordPresence
    {
        private static EditorDiscordIPC _ipc;

        public static bool IsConnected => _ipc?.IsConnected ?? false;
        public static DateTime? LastUpdated { get; private set; }
        public static DateTimeOffset? SessionStart { get; private set; }

        static EditorDiscordPresence()
        {
            EditorApplication.quitting += Disconnect;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

            if (EditorPrefs.GetBool(Prefs.AutoConnect, false))
                Initialize();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public static void Initialize()
        {
            var appId = EditorPrefs.GetString(Prefs.AppId, "");
            if (string.IsNullOrEmpty(appId)) return;

            _ipc?.Disconnect();
            _ipc = new EditorDiscordIPC();

            if (_ipc.Connect(appId))
            {
                UpdateActivity();
                SessionStart = DateTimeOffset.UtcNow;
                Debug.Log("[Editor Discord Integration] Connected!");
            }
        }

        public static void UpdateActivity()
        {
            if (_ipc == null || !_ipc.IsConnected) return;

            // ── Details (top line) ───────────────────────────────────────────────
            string details = null;
            if (EditorPrefs.GetBool(Prefs.ShowProjectName, true))
            {
                var custom = EditorPrefs.GetString(Prefs.CustomProjectName, "").Trim();
                details = string.IsNullOrEmpty(custom)
                    ? PlayerSettings.productName
                    : custom;
            }

            // ── State (bottom line) ──────────────────────────────────────────────
            var showScene   = EditorPrefs.GetBool(Prefs.ShowScene, false);
            var showVersion = EditorPrefs.GetBool(Prefs.ShowVersion, false);

            string scenePart   = showScene   ? GetCurrentSceneName() : null;
            string versionPart = showVersion ? $"Unity {Application.unityVersion}" : null;

            string state = null;
            if (scenePart != null && versionPart != null)
                state = $"{scenePart}  •  {versionPart}";
            else if (scenePart != null)
                state = scenePart;
            else if (versionPart != null)
                state = versionPart;

            // ── Image ────────────────────────────────────────────────────────────
            var imageKey  = EditorPrefs.GetString(Prefs.ImageKey, "").Trim();
            var imageText = EditorPrefs.GetString(Prefs.ImageText, "Unity").Trim();

            _ipc.SetActivity(
                details:        details,
                state:          state,
                largeImageKey:  string.IsNullOrEmpty(imageKey) ? null : imageKey,
                largeImageText: string.IsNullOrEmpty(imageText) ? null : imageText,
                startTimestamp: SessionStart
            );

            LastUpdated = DateTime.Now;
        }

        public static void Disconnect()
        {
            _ipc?.Disconnect();
            _ipc = null;
            LastUpdated = null;
            SessionStart = null;
            Debug.Log("[Editor Discord Integration] Disconnected.");
        }

        // ── Internal ────────────────────────────────────────────────────────────

        private static void OnSceneChanged(
            UnityEngine.SceneManagement.Scene prev,
            UnityEngine.SceneManagement.Scene next)
        {
            if (EditorPrefs.GetBool(Prefs.ShowScene, false))
                UpdateActivity();
        }

        private static string GetCurrentSceneName()
        {
            var scene = EditorSceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.name) ? "Untitled Scene" : scene.name;
        }

        // ── Pref key constants ──────────────────────────────────────────────────

        public static class Prefs
        {
            public const string AppId             = "DiscordIntegration_AppId";
            public const string AutoConnect       = "DiscordIntegration_AutoConnect";
            public const string ShowProjectName   = "DiscordIntegration_ShowProjectName";
            public const string CustomProjectName = "DiscordIntegration_CustomProjectName";
            public const string ShowScene         = "DiscordIntegration_ShowScene";
            public const string ShowVersion       = "DiscordIntegration_ShowVersion";
            public const string ImageKey          = "DiscordIntegration_ImageKey";
            public const string ImageText         = "DiscordIntegration_ImageText";
        }
    }
}