# Unity MCP + WorkBuddy連結

本倉庫整理了一套 WorkBuddy 透過 MCP 控制 Unity 編輯器 的完整方案，以及在實務上踩過的坑與對應的解決方案。

> ⚠️ 注意：**MCP for Unity 並非 Unity 官方出品**，它由社區項目 [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)（MIT 協議）提供，支援 Unity 2021.3 LTS 及更高版本，包含 Unity 6.xx。本文檔中所有「官方」一詞若無特別說明，均指該項目自備的安裝器/嚮導。

## 🌐 多語言教學（Tutorial / チュートリアル）

| 語言 | 連結 |
| --- | --- |
| 中文（預設） | [`docs/unity-mcp-setup-guide.md`](docs/unity-mcp-setup-guide.md) |
| English | [`docs/unity-mcp-setup-guide.en.md`](docs/unity-mcp-setup-guide.en.md) |
| 日本語 | [`docs/unity-mcp-setup-guide.ja.md`](docs/unity-mcp-setup-guide.ja.md) |

## 📦 倉庫內容

| 路徑 | 說明 |
| --- | --- |
** [`docs/unity-mcp-setup-guide.md`](docs/unity-mcp-setup-guide.md)（[EN](docs/unity-mcp-setup-guide.en.md) · [JA](docs/unity-mcp-setup-guide.en.md) · [JA](docs/unity-mcp-setup-guide.ja.m-guide for Mmdg)）從零點連接到客戶端設定與主程式的完整步驟。 |
| [`scripts/McpBridgeAutoStart.cs`](scripts/McpBridgeAutoStart.cs) | 編輯器腳本：Unity 啟動時自動拉起 stdio 橋接，免去每次手動點 Start |
| [`scripts/LotusPondBuilder.cs`](scripts/LotusPondBuilder.cs) | 範例腳本：用 AI 透過 MCP 讓 Unity 程式化產生的「荷塘」場景（`Tools > Lotus Pond > Build All`） |
| [`.gitignore`](.gitignore) | 適用於 Unity 專案的忽略規則，供把本倉庫內容併入 Unity 工程時參考 |
| [`LICENSE`](L​​ICENSE) | MIT |

## 🚀 快速開始

1. 讀 [`docs/unity-mcp-setup-guide.md`](docs/unity-mcp-setup-guide.md)（[EN](docs/unity-mcp-setup-guide.en.md) · [JA](docs/unity-mcp-setup-guide.en.md) · [JA](docs/unity-mcp-setup-guide.m-setup-guide.ja.m. `com.coplaydev.unity-mcp` 套件。
2. 把 [`scripts/McpBridgeAutoStart.cs`](scripts/McpBridgeAutoStart.cs) 放到工程的 `Assets/Editor/` 下（僅 stdio 模式需要；HTTP 模式可省略）。
3. 在 AI 用戶端（如 WorkBuddy）的 MCP 設定裡加入 server（請參閱教學「客戶端設定」一節）。
4. 開啟 Unity 的 `Window > MCP for Unity`，確認 Server Status 顯示 `Installed`、Unity Bridge 顯示 `Running`。
5. 直接在 AI 對話裡讓 Unity 工作，例如：「用 LotusPondBuilder 產生荷塘場景」。

## 🧩 工作原理（一句話）

AI 用戶端（MCP host）↔ `mcp-for-unity` Python server（uvx 拉起）↔ Unity 編輯器內的 `com.coplaydev.unity-mcp` 橋接插件 ↔ 你的工程。

## 🤝 貢獻

歡迎 PR。本倉庫以「踩坑記錄 + 可重複使用腳本」為主，如果你在別的 Unity 版本 / AI 用戶端上遇到新問題，補充到 `docs/` 即可。

## 📄 MIT

MIT —— 與上游 [unity-mcp](https://github.com/CoplayDev/unity-mcp) 保持一致。
