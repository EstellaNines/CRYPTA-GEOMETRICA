#if UNITY_EDITOR
using UnityEditor;

public static class AppExitMenu
{
    [MenuItem("自制工具/系统/退出 (Ctrl+Shift+Q) %#q", priority = 10)]
    public static void QuitFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false; // 停止运行
        }
        else
        {
            EditorApplication.Exit(0); // 直接退出编辑器
        }
    }

    [MenuItem("自制工具/系统/停止播放 (Ctrl+Q) %q", priority = 11)]
    public static void StopPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
