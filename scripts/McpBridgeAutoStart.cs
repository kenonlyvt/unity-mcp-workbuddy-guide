// 自动启动 MCP for Unity 的 stdio 桥接（供 AI 客户端如 WorkBuddy 连接）。
// 由 WorkBuddy 于 2026-10-10 添加。删除本文件即恢复手动启动。
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
internal static class McpBridgeAutoStart
{
    static McpBridgeAutoStart()
    {
        // 用 delayCall 延后一帧，避开域重载期间 Unity API 不可用的问题
        EditorApplication.delayCall += () =>
        {
            try
            {
                MCPForUnity.Editor.McpCiBoot.StartStdioForCi();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning("[MCP AutoStart] 启动 stdio 桥接失败: " + e.Message);
            }
        };
    }
}
#endif
