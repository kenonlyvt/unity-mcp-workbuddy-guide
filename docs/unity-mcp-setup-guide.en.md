# Unity MCP Setup Guide (WorkBuddy / Cursor / Claude Code)

> Compatible: Unity 2021.3 LTS ~ 6.x ｜ MCP for Unity (CoplayDev, **not** official Unity) v10.x
> This guide is based on a real Windows rollout and focuses on the pitfalls the official docs leave out.

---

## 0. Understand these first

- **MCP for Unity is NOT an official Unity plugin.** It is the community project [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) (MIT). It has two parts:
  - the **Unity-side plugin** `com.coplaydev.unity-mcp` (C#, installed in your project), and
  - the **Python-side server** `mcp-for-unity` (launched by `uvx`; the AI client talks to Unity through it).
- **Two transports:**
  - `stdio` (default): the AI client launches the server as a child process. The **Bridge does NOT auto-start** — you must start it manually or via a script (see Section 5).
  - `HTTP`: Unity runs its own local HTTP server; the Bridge can auto-start, but needs extra ports/config.
- This guide uses **WorkBuddy** as the AI client and **stdio** as the example. Other clients (Cursor / VS Code / Claude Code) are covered in the "Client config" section.

---

## 1. Prerequisites: Python & uvx (the easiest place to get stuck)

The MCP Python server depends on `uvx` (the uv script runner). The installer (`Window > MCP for Unity > Auto-Setup`) detects Python and uv/uvx, and stays red if it can't find them.

### 1.1 Install Python + uv

- Install Python 3.10+, check "Add to PATH".
- Install uv: `pip install uv`, or use the [official astral.sh/uv installer](https://docs.astral.sh/uv/). Verify with `uvx --version`.

### 1.2 Make the Unity installer "see" your Python (Windows-specific pit)

Unity's Auto-Setup detects Python in a rigid way and often reports **"Python Not Found"** even when `where python` works. Align all three:

1. **Registry `InstallPath`**: make sure `HKLM\SOFTWARE\Python\PythonCore\<version>\InstallPath` points to the real path (on multi-version machines it may point somewhere nonexistent).
2. **System PATH**: add the Python install dir and its `Scripts` dir.
3. **The wizard's hardcoded search dir**: the installer looks in `C:\Users\<you>\AppData\Local\Programs\Python`. If your Python lives elsewhere (e.g. `E:\Python\Python313`), **create a directory junction** to satisfy it:

   ```powershell
   # run as Administrator
   cmd /c mklink /J "C:\Users\Ken\AppData\Local\Programs\Python" "E:\Python\Python313"
   ```

   > Use `/J` (junction), not `/D` (symlink) — a junction is followed by normal processes without needing Developer Mode / admin.

After this, restart Unity; the Python check in the wizard should turn green.

---

## 2. Configure the MCP server in your AI client

For WorkBuddy, edit (or create) `~/.workbuddy/mcp.json` and add:

```json
{
  "mcpServers": {
    "unityMCP": {
      "command": "uvx",
      "args": ["--from", "mcpforunityserver", "mcp-for-unity", "--transport", "stdio"]
    }
  }
}
```

- On first run, `uvx` auto-downloads `mcpforunityserver` (a pip package whose version matches the Unity-side package, e.g. v10.3.0).
- Save and restart the AI client so it reloads the MCP config and performs the handshake. On success, the server lists the tools it supports (our test exposed **36 tools**: scene queries, GameObject CRUD, asset creation, menu execution, etc.).

> If your client already has other MCP servers (e.g. baota-mcp), **merge them into the same `mcpServers` object** — don't overwrite the existing config.

---

## 3. Install `com.coplaydev.unity-mcp` in your Unity project

### Option A: UPM git dependency (online, officially recommended)

Edit your project's `Packages/manifest.json` and add to `dependencies`:

```json
{
  "dependencies": {
    "com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git"
  }
}
```

Unity will pull and compile it from git automatically.

### Option B: embedded offline package (more reliable behind GFW / to pin a version)

Unzip the package into `Packages/com.coplaydev.unity-mcp/` (sibling of `Assets` and `ProjectSettings`). **No** `manifest.json` change is needed — Unity loads folders under `Packages/` as local packages automatically.

> In practice, git dependencies to GitHub often time out from China, so **Option B is the calmer choice**. We tested the v10.3.0 embedded package on Unity 6 (`6000.3.25f1`) with no issues.

After installation, the **`Window > MCP for Unity`** menu appears, shortcut `Ctrl+Shift+M` (Win/Linux).

---

## 4. Getting Unity 6 (China-network pit)

If you download the international Unity 6 from a China-region Unity Hub, you'll likely hit **CDN 404 / download failure** (the China Hub uses a domestic mirror that can't serve the international installer).

Workable paths (pick one):

- **Use a proxy**: point the downloader at an HTTP proxy (e.g. `127.0.0.1:7897`) and download the matching `UnitySetup64-<version>.exe` and Hub installer directly from unity.com, then double-click to install.
- **Download from unity.com directly**: get the international Hub + Editor installer, then install manually.
- During install, watch **C: drive space**: Unity Editor + modules easily exceed ten GB. If C: is full you'll see weird errors like "Moving file failed" / "Cannot save license" (see Section 6).

> If you see **"Cannot save the license file (Unity_lic.ulf)"**: the old license file is usually permission-locked (only admin can write). Delete `C:\ProgramData\Unity\Unity_lic.ulf` (or the stale file in its directory) via **Administrator CMD**, then reactivate.

---

## 5. Auto-start the Bridge (required for stdio)

Key trap: **in stdio mode the Unity Bridge does NOT start automatically when you open the project** (auto-start is natively supported only for HTTP). If the Bridge is Stopped, the AI client connects but can't drive Unity.

Two options:

### 5.1 Manual (every time)

Open `Window > MCP for Unity` and click **Start Bridge**; wait until the status shows `Running`.

### 5.2 Auto-start via script (recommended)

Drop this repo's [`scripts/McpBridgeAutoStart.cs`](../../scripts/McpBridgeAutoStart.cs) into your project's `Assets/Editor/`. It uses `[InitializeOnLoad]` to auto-call `McpCiBoot.StartStdioForCi()` after the editor loads:

```csharp
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
internal static class McpBridgeAutoStart
{
    static McpBridgeAutoStart()
    {
        EditorApplication.delayCall += () =>
        {
            try { MCPForUnity.Editor.McpCiBoot.StartStdioForCi(); }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning("[MCP AutoStart] 启动 stdio 桥接失败: " + e.Message);
            }
        };
    }
}
#endif
```

- `delayCall` defers one frame to avoid the "API unavailable during domain reload" problem.
- **To go back to manual start**: just delete this file.

Once connected, a status file is written under `~/.unity-mcp/` (containing project name, Unity version, port like 6400), and the AI client can enumerate the tools Unity exposes.

---

## 6. Common errors quick reference

| Symptom | Root cause | Fix |
| --- | --- | --- |
| Installer Python check red | Registry / PATH / search dir not aligned | See 1.2, create a junction |
| `uvx: command not found` | uv missing or not on PATH | Install uv, or tell the installer its path (Choose UV Install Location) |
| Client connects but can't drive Unity | Bridge is Stopped (stdio doesn't auto-start) | See Section 5 — add auto-start script or Start manually |
| Unity 6 download 404 | China Hub can't fetch international package | Use proxy / download from unity.com manually |
| "Moving file failed" / install stalls | C: drive is full (not a temp-dir issue) | Clear Unity caches / Recycle Bin, or free space |
| "Cannot save license" | Old `Unity_lic.ulf` permission-locked | Delete the stale file via Administrator CMD, then reactivate |
| 0 tools / not synced | server version ≠ package version | Align `mcpforunityserver` with `com.coplaydev.unity-mcp` |

---

## 7. Advanced: let the AI generate a scene via MCP

Once MCP is up, the AI client can call Unity's C# tools directly. This repo's [`scripts/LotusPondBuilder.cs`](../../scripts/LotusPondBuilder.cs) is an example: it procedurally builds a lotus pond (leaves / flowers / water / floating petals / rocks / reeds) and exposes `Tools > Lotus Pond > Build All` in the menu.

Just tell the AI in chat:

> "Open my Unity project, run LotusPondBuilder's Build All, and generate a lotus-pond scene with the camera aimed at the center."

The AI executes the menu/method inside Unity through MCP — no manual clicking. That's the real value of MCP for Unity: **turning editor operations into AI-callable, composable capabilities**.

---

## 8. Verification checklist

- [ ] `uvx --version` works and Python is recognized by the installer
- [ ] `~/.workbuddy/mcp.json` (or your client's config) contains `unityMCP`, and the handshake succeeds after restart
- [ ] Unity project has `com.coplaydev.unity-mcp` (git dependency or embedded)
- [ ] In `Window > MCP for Unity`: Server Status = Installed, Bridge = Running
- [ ] AI client lists Unity tools (e.g. `mcpUnity__*` / `unityMCP__*`)
- [ ] A real call works (execute a menu, create a GameObject)

When all are checked, your AI client has truly "taken over" the Unity editor. Have fun 🪷
