# Unity MCP + WorkBuddy连接

本仓库整理了一套 WorkBuddy 通过 MCP 控制 Unity 编辑器 的完整方案，以及在实践中踩过的坑与对应的解决办法。

> ⚠️ 注意：**MCP for Unity 并非 Unity 官方出品**，它由社区项目 [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)（MIT 协议）提供，支持 Unity 2021.3 LTS 及更高版本，包含 Unity 6.x。本文档中所有“官方”一词若无特别说明，均指该项目自带的安装器/向导。

## 🌐 多语言教程（Tutorial / チュートリアル）

| 语言 | 链接 |
| --- | --- |
| 简体中文（默认） | [`docs/unity-mcp-setup-guide.md`](docs/unity-mcp-setup-guide.md) |
| English | [`docs/unity-mcp-setup-guide.en.md`](docs/unity-mcp-setup-guide.en.md) |
| 日本語 | [`docs/unity-mcp-setup-guide.ja.md`](docs/unity-mcp-setup-guide.ja.md) |

## 📦 仓库内容

| 路径 | 说明 |
| --- | --- |
| [`docs/unity-mcp-setup-guide.md`](docs/unity-mcp-setup-guide.md)（[EN](docs/unity-mcp-setup-guide.en.md) · [JA](docs/unity-mcp-setup-guide.ja.md)） | **主教程**：从零配置 MCP for Unity 并接入 AI 客户端的完整步骤与排错 |
| [`scripts/McpBridgeAutoStart.cs`](scripts/McpBridgeAutoStart.cs) | 编辑器脚本：Unity 启动时自动拉起 stdio 桥接，免去每次手动点 Start |
| [`scripts/LotusPondBuilder.cs`](scripts/LotusPondBuilder.cs) | 示例脚本：用 AI 通过 MCP 让 Unity 程序化生成的「荷塘」场景（`Tools > Lotus Pond > Build All`） |
| [`.gitignore`](.gitignore) | 适用于 Unity 项目的忽略规则，供把本仓库内容并入 Unity 工程时参考 |
| [`LICENSE`](LICENSE) | MIT |

## 🚀 快速开始

1. 读 [`docs/unity-mcp-setup-guide.md`](docs/unity-mcp-setup-guide.md)（[EN](docs/unity-mcp-setup-guide.en.md) · [JA](docs/unity-mcp-setup-guide.ja.md)），按步骤装好 Python / uvx、在 Unity 里装好 `com.coplaydev.unity-mcp` 包。
2. 把 [`scripts/McpBridgeAutoStart.cs`](scripts/McpBridgeAutoStart.cs) 放到工程的 `Assets/Editor/` 下（仅 stdio 模式需要；HTTP 模式可省略）。
3. 在 AI 客户端（如 WorkBuddy）的 MCP 配置里加入 server（见教程「客户端配置」一节）。
4. 打开 Unity 的 `Window > MCP for Unity`，确认 Server Status 显示 `Installed`、Unity Bridge 显示 `Running`。
5. 直接在 AI 对话里让 Unity 干活，例如：“用 LotusPondBuilder 生成荷塘场景”。

## 🧩 工作原理（一句话）

AI 客户端（MCP host）↔ `mcp-for-unity` Python server（uvx 拉起）↔ Unity 编辑器内的 `com.coplaydev.unity-mcp` 桥接插件 ↔ 你的工程。

## 🤝 贡献

欢迎 PR。本仓库以「踩坑记录 + 可复用脚本」为主，如果你在别的 Unity 版本 / AI 客户端上遇到新问题，补充到 `docs/` 即可。

## 📄 许可证

MIT —— 与上游 [unity-mcp](https://github.com/CoplayDev/unity-mcp) 保持一致。
