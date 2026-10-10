# Unity MCP 配置实战教程（WorkBuddy / Cursor / Claude Code 通用）

> 适用版本：Unity 2021.3 LTS ~ 6.x ｜ MCP for Unity（CoplayDev，非 Unity 官方）v10.x
> 本文基于一次真實的 Windows 環境落地过程整理，重点放在**官方文档没說、但一定會踩坑**。

---

## 0. 先搞清楚幾件事

- **MCP for Unity 不是 Unity 官方插件**，是社區項目 [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)（MIT）。它由两部分組成：
  - **Unity ** `com.coplaydev.unity-mcp`（C#，装进你的工程）
  - **Python server** `mcp-for-unity`（由 `uvx` 自动拉起，AI 客户端通过它和 Unity 通信）
- **两种传输方式**：
  - `stdio`（默认）：AI 客户端直接拉起 server 子进程，**桥接（Bridge）不自动启动**，需要手动点或脚本自动拉起（见第 5 节）。
  - `HTTP`：Unity 自己起一个本地 HTTP server，桥接可自动启动，但需要额外端口与配置。
- 本教程以 **WorkBuddy** 作为 AI 客户端、以 **stdio** 为例，其他客户端（Cursor/VS Code/Claude Code）在「客户端配置」一节通用。 全部轉成繁體
Unity MCP 設定實戰教學（WorkBuddy / Cursor / Claude Code 通用）   
適用版本：Unity 2021.3 LTS ~ 6.x ｜ MCP for Unity（CoplayDev，非 Unity 官方）v10.x
本文基於一次真實的 Windows 環境落地流程整理，重點放在官方文件沒說、但一定會踩的坑。   

0. 先搞清楚幾件事   
MCP for Unity 不是 Unity 官方插件，是社群專案 CoplayDev/unity-mcp（MIT）。它由兩個部分組成：   

Unity 側插件 com.coplaydev.unity-mcp（C#，裝進你的專案）

Python 側 server mcp-for-unity（由 uvx 自動拉起，AI 用戶端透過它和 Unity 通訊）

兩種傳輸方式：

stdio（預設）：AI 用戶端直接拉起 server 子行程，橋接（Bridge）不會自動啟動，需要手動點擊或透過腳本自動拉起（見第 5 節）。

HTTP：Unity 自己起一個本機 HTTP server，橋接可自動啟動，但需要額外連接埠與設定。

本教學以 WorkBuddy 作為 AI 用戶端、以 stdio 為例，其他客戶端（Cursor / VS Code / Claude Code）在「客戶端設定」一節通用。

