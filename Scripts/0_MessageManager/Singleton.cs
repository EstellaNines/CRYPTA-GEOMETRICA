/// <summary>
/// 泛型单例基类（非 MonoBehaviour）
/// 适用于纯 C# 类的单例模式
/// </summary>
/// <typeparam name="T">单例类型</typeparam>
public class Singleton<T> where T : class, new()
{
    private static T instance;
    private static readonly object lockObject = new object();

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// 清除单例实例
    /// </summary>
    public static void ClearInstance()
    {
        lock (lockObject)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 检查实例是否存在
    /// </summary>
    public static bool HasInstance
    {
        get { return instance != null; }
    }
}
