# Editor Discord Integration

Shows your Discord status as "Unity" while you're working in the Unity Editor. Supports a custom image, project name, current scene, and Unity version.

## Setup

1. **Import the package** — Drop `EditorDiscordIntegration.unitypackage` in your project and import the content, or manually download, unzip, and drop the files into `Assets/Editor/`
2. Go to [discord.com/developers/applications](https://discord.com/developers/applications) and create a new application
3. Copy the **Application ID**
4. In Unity, open **Window > Editor Discord Integration**
5. Paste your App ID and hit **Connect**

## Image

You have two options for the presence image:

- **Discord asset key** — upload an image under **Rich Presence → Art Assets** in your Discord app, then paste the asset name (e.g. `unity_logo`)
- **Public URL** — paste any public HTTPS image URL (e.g. a raw GitHub link)

## Options

| Option | Description |
|---|---|
| Auto Connect on Open | Automatically connects when Unity loads |
| Show Project Name | Shows `PlayerSettings.productName`, or a custom name |
| Show Current Scene | Shows the active scene name, updates on scene change |
| Show Unity Version | Shows the current Unity version |

If both **Show Current Scene** and **Show Unity Version** are enabled, they appear on one line separated by a bullet, e.g. `MainMenu • Unity 6000.0.30f1`.

## Files

| File | Description |
|---|---|
| `EditorDiscordIPC.cs` | Low-level Discord IPC pipe connection and message framing |
| `EditorDiscordPresence.cs` | Manages the connection and activity state |
| `EditorDiscordPresenceWindow.cs` | The editor window UI |

## Notes

- Settings are saved via `EditorPrefs` - per machine, not per project
- The Discord timer resets are avoided by anchoring to a fixed session start timestamp on connect
- Only reconnects on scene change if **Show Current Scene** is enabled, avoiding unnecessary updates
