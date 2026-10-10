# Unity MCP （WorkBuddy）
> 適用版本：Unity 2021.3 LTS ~ 6.x ｜ MCP for Unity（CoplayDev，非 Unity 官方）v10.x
> 本文基於一次真實的 Windows 環境落地流程整理，重點放在官方文件沒說、但一定會踩的坑。   

---


0. 先搞清楚幾件事   
MCP for Unity 不是 Unity 官方插件，是社群專案 CoplayDev/unity-mcp（MIT）。它由兩個部分組成：   

Unity 側插件 com.coplaydev.unity-mcp（C#，裝進你的專案）

Python 側 server mcp-for-unity（由 uvx 自動拉起，AI 用戶端透過它和 Unity 通訊）

兩種傳輸方式：

stdio（預設）：AI 用戶端直接拉起 server 子行程，橋接（Bridge）不會自動啟動，需要手動點擊或透過腳本自動拉起（見第 5 節）。

HTTP：Unity 自己起一個本機 HTTP server，橋接可自動啟動，但需要額外連接埠與設定。

本教學以 WorkBuddy 作為 AI 用戶端、以 stdio 為例，其他客戶端在「客戶端設定」一節通用。

