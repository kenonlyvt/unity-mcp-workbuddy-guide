# Unity MCP 設定實戰教學（WorkBuddy）

> 適用版本：Unity 2021.3 LTS ~ 6.x ｜ MCP for Unity（CoplayDev，非 Unity 官方）v10.x
> 本文基於一次真實的 Windows 環境落地流程整理，重點放在**官方文件沒說、但一定會踩的坑**。

---

## 0. 先搞清楚幾件事

- **MCP for Unity 不是 Unity 官方外掛**，是社群專案 [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)（MIT）。它由兩個部分組成：
 - **Unity 側邊插件** `com.coplaydev.unity-mcp`（C#，裝進你的工程）
 - **Python 側 server** `mcp-for-unity`（由 `uvx` 自動拉起，AI 用戶端透過它和 Unity 通訊）
- **兩種傳輸方式**：
 - `stdio`（預設）：AI 用戶端直接拉起 server 子進程，**橋接（Bridge）不會自動啟動**，需要手動點或腳本自動拉起（請參閱第 5 節）。 
- `HTTP`：Unity 自己起一個本機 HTTP server，橋接可自動啟動，但需要額外連接埠與設定。
- 本教學以 **WorkBuddy** 作為 AI 用戶端、以 **stdio** 為例，其他客戶端（Cursor/VS Code/Claude Code）在「客戶端設定」一節通用。

---

## 1. 环境前置：Python 与 uvx（最容易卡的地方）

MCP 的 Python server 依赖 `uvx`（uv 的脚本运行器）。安装器（Window > MCP for Unity > Auto-Setup）会检测 Python 和 uv/uvx，**检测不到就一路红灯**。

### 1.1 装 Python + uv

- 装 Python 3.10+，勾选 “Add to PATH”。
- 装 uv：`pip install uv` 或走 [astral.sh/uv](https://docs.astral.sh/uv/) 官方安装器。装完确认 `uvx --version` 可用。

### 1.2 让 Unity 安装器“看见”你的 Python（Windows 特有坑）

Unity 的 Auto-Setup 向导检测 Python 的逻辑很死板，容易报 **“Python Not Found”**，即使你 `where python` 能找到。三处都要对齐：

1. **注册表 InstallPath**：确认 `HKLM\SOFTWARE\Python\PythonCore\<版本>\InstallPath` 指向真实路径（有些多版本机器这里会指向不存在的目录）。
2. **系统 PATH**：把 Python 安装目录和 `Scripts` 目录加进 PATH。
3. **向导硬编码的搜索目录**：安装器会去 `C:\Users\<你>\AppData\Local\Programs\Python` 找。如果你的 Python 装在别处（如 `E:\Python\Python313`），**建一个目录联接（junction）** 即可蒙混过关：

   ```powershell
   # 用管理员 PowerShell
   cmd /c mklink /J "C:\Users\Ken\AppData\Local\Programs\Python" "E:\Python\Python313"
   ```

   > 用 `/J`（junction）而不是 `/D`（symlink），前者不需要开发者模式/管理员权限也能被普通进程跟随。

处理完后重启 Unity，向导里的 Python 检测应当变绿。

---

## 2. 在 AI 客户端配置 MCP server

以 WorkBuddy 为例，编辑（或新建）配置文件 `~/.workbuddy/mcp.json`，加入：

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

- 首次运行会由 `uvx` 自动下载 `mcpforunityserver`（pip 包，版本与 Unity 侧包对应，例如 v10.3.0）。
- 保存后重启 AI 客户端，让其重新加载 MCP 配置并建立握手。握手成功时，server 会列出它支持的工具（本仓库实测 36 个工具：场景查询、GameObject 增删改、资源创建、菜单执行等）。

> 若客户端已存在其它 MCP server（如宝塔 baota-mcp），**合并进同一个 `mcpServers` 对象**，不要覆盖掉原有配置。

---

## 3. 在 Unity 工程里安装 `com.coplaydev.unity-mcp`

### 方式 A：UPM git 依赖（联网、官方推荐）

编辑工程根目录 `Packages/manifest.json`，在 `dependencies` 里加：

```json
{
  "dependencies": {
    "com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git"
  }
}
```

保存后 Unity 会自动从 git 拉取并编译。

### 方式 B：内嵌离线包（国内网络 / 想锁版本时更稳）

把包解压到工程的 `Packages/com.coplaydev.unity-mcp/`（与 `Assets`、`ProjectSettings` 同级）。`manifest.json` **无需**改动，Unity 会自动把 `Packages/` 下的文件夹当作本地包加载。

> 实战中，国内访问 GitHub 的 git 依赖经常超时，**方式 B 更省心**。本仓库实测用 v10.3.0 内嵌包在 Unity 6（`6000.3.25f1`）下工作正常。

装好后，菜单栏会出现 **`Window > MCP for Unity`**，快捷键 `Ctrl+Shift+M`（Win/Linux）。

---

## 4. Unity 6 的获取（国内网络坑）

如果你在中国区 Unity Hub 里下载国际版 Unity 6，大概率会遇到 **CDN 404 / 下载失败**（中国区 Hub 走的是国内镜像，拿不到国际版安装包）。

可行路径（任选）：

- **走代理**：给下载器配 HTTP 代理（如 `127.0.0.1:7897`），直接从 unity.com 下载对应版本的 `UnitySetup64-<版本>.exe` 与 Hub 安装包，再手动双击安装。
- **直接在 unity.com 下载**：国际版 Hub + Editor 安装包，下完手动装。
- 安装时注意 **C 盘空间**：Unity Editor + 模块动辄十几 GB。若 C 盘爆满，会出现 “Moving file failed”“Cannot save license” 等诡异错误（见第 6 节）。

> 安装后若提示 “Cannot save the license file (Unity_lic.ulf)”：多半是旧 license 文件权限被锁（只有管理员能写）。用**管理员 CMD** 删除 `C:\ProgramData\Unity\Unity_lic.ulf`（或 `Unity_lic.ulf` 所在目录下的旧文件），再重新激活即可。

---

## 5. 自动拉起 Bridge（stdio 模式必看）

关键陷阱：**stdio 模式下，Unity 的 Bridge 不会在打开工程时自动启动**（自动启动只原生支持 HTTP 模式）。如果 Bridge 是 Stopped，AI 客户端连上来也调不动 Unity。

两个办法：

### 5.1 手动（每次）

打开 `Window > MCP for Unity`，点 **Start Bridge**，看到状态变 `Running`。

### 5.2 脚本自动启动（推荐）

把本仓库的 [`scripts/McpBridgeAutoStart.cs`](../../scripts/McpBridgeAutoStart.cs) 放进工程 `Assets/Editor/`。它用 `[InitializeOnLoad]` 在编辑器加载后自动调用 `McpCiBoot.StartStdioForCi()` 拉起 stdio 桥接：

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

- 用 `delayCall` 延后一帧，避开 Unity 域重载（domain reload）期间 API 不可用的问题。
- **想恢复手动启动**：直接删除这个文件即可。

桥接连上后，会在 `~/.unity-mcp/` 下生成一个状态文件（含工程名、Unity 版本、端口，如 6400），AI 客户端即可枚举到 Unity 暴露的工具。

---

## 6. 常见错误速查

| 现象 | 根因 | 解决办法 |
| --- | --- | --- |
| 安装器 Python 检测红灯 | 注册表/PATH/搜索目录未对齐 | 见 1.2，建 junction |
| `uvx: command not found` | uv 没装或不在 PATH | 装 uv，或将 uvx 路径告诉安装器（Choose UV Install Location） |
| AI 客户端连上但调不动 Unity | Bridge 是 Stopped（stdio 不自动起） | 见第 5 节，放自动启动脚本或手动 Start |
| Unity 6 下载 404 | 中国区 Hub 拿不到国际版包 | 走代理 / 国际版官网手动下载 |
| “Moving file failed” / 装不动 | C 盘满了（不是临时盘问题） | 清 Unity 缓存、回收站，或删多余内容腾空间 |
| “Cannot save license” | 旧 `Unity_lic.ulf` 权限被锁 | 管理员 CMD 删除旧文件再激活 |
| 工具数为 0 / 没同步 | server 版本与包版本不匹配 | 对齐 `mcpforunityserver` 与 `com.coplaydev.unity-mcp` 的版本 |

---

## 7. 进阶示例：让 AI 用 MCP 生成场景

装好 MCP 后，AI 客户端能直接调用 Unity 的 C# 工具。本仓库的 [`scripts/LotusPondBuilder.cs`](../../scripts/LotusPondBuilder.cs) 就是一个例子：它程序化生成荷塘（荷叶/莲花/水面/浮瓣/石块/芦苇），并在菜单暴露 `Tools > Lotus Pond > Build All`。

在 AI 对话里直接说：

> “打开我的 Unity 工程，运行 LotusPondBuilder 的 Build All，生成一个荷塘场景，把相机对准中心。”

AI 会通过 MCP 在 Unity 内执行对应菜单/方法，无需你手动点。这正是 MCP for Unity 的价值：**把编辑器操作变成 AI 可调用、可编排的能力**。

---

## 8. 验证清单

- [ ] `uvx --version` 可用，Python 被安装器识别
- [ ] `~/.workbuddy/mcp.json`（或对应客户端配置）含 `unityMCP`，重启后握手成功
- [ ] Unity 工程已装 `com.coplaydev.unity-mcp`（git 依赖或内嵌）
- [ ] `Window > MCP for Unity` 中 Server Status = Installed，Bridge = Running
- [ ] AI 客户端能列出 Unity 工具（如 `mcpUnity__*` / `unityMCP__*`）
- [ ] 实测一次调用（如执行菜单、创建 GameObject）成功

完成上述，你的 AI 客户端就真正“接管”了 Unity 编辑器。祝玩得开心 🪷
