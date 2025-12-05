using UnityEngine;

public class AppExitController : MonoBehaviour
{
    public bool quitOnEscape = false;
    public KeyCode quitKey = KeyCode.Escape;

    void Update()
    {
        if (quitOnEscape && Input.GetKeyDown(quitKey))
        {
            Quit();
        }
    }

    [ContextMenu("Quit Now")]
    public void Quit()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false; // 停止运行
        }
        else
        {
            UnityEditor.EditorApplication.Exit(0); // 退出编辑器
        }
#else
        Application.Quit(); // 构建版本直接退出
#endif
    }

    public static void QuitStatic()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            UnityEditor.EditorApplication.Exit(0);
        }
#else
        Application.Quit();
#endif
    }
}
