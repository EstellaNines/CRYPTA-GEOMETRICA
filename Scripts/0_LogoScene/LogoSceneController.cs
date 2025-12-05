using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 控制开场Logo视频播放并自动跳转到下一个场景
/// </summary>
public class LogoSceneController : MonoBehaviour
{
    [Header("视频设置")]
    [Tooltip("要播放的视频片段")]
    public VideoClip logoVideo;
    
    [Header("场景跳转设置")]
    [Tooltip("视频播放完成后要加载的场景名称")]
    public string nextSceneName = "MainMenu";
    
    [Tooltip("是否允许按键跳过视频")]
    public bool allowSkip = true;
    
    [Tooltip("视频播放完成后的额外等待时间（秒）")]
    public float delayAfterVideo = 0.5f;

    private VideoPlayer videoPlayer;
    private bool isVideoFinished = false;

    void Awake()
    {
        // 获取或添加VideoPlayer组件
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        // 配置VideoPlayer
        SetupVideoPlayer();
    }

    void Start()
    {
        // 开始播放视频
        PlayLogoVideo();
    }

    void Update()
    {
        // 检测跳过输入
        if (allowSkip && !isVideoFinished)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                SkipVideo();
            }
        }
    }

    /// <summary>
    /// 配置VideoPlayer组件
    /// </summary>
    void SetupVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.isLooping = false;
        
        // 音频设置 - 解决音频缓冲区溢出问题
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.skipOnDrop = true; // 允许跳帧以保持同步
        
        // 设置视频片段
        if (logoVideo != null)
        {
            videoPlayer.clip = logoVideo;
        }
        
        // 订阅视频播放完成事件
        videoPlayer.loopPointReached += OnVideoFinished;
        
        // 订阅准备完成事件
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    /// <summary>
    /// 播放Logo视频
    /// </summary>
    void PlayLogoVideo()
    {
        if (logoVideo == null)
        {
            Debug.LogWarning("未设置Logo视频，将直接跳转到下一个场景");
            LoadNextScene();
            return;
        }

        // 先准备视频，准备完成后再播放
        videoPlayer.Prepare();
    }
    
    /// <summary>
    /// 视频准备完成回调
    /// </summary>
    void OnVideoPrepared(VideoPlayer vp)
    {
        // 视频准备完成后开始播放
        videoPlayer.Play();
    }

    /// <summary>
    /// 视频播放完成回调
    /// </summary>
    void OnVideoFinished(VideoPlayer vp)
    {
        if (!isVideoFinished)
        {
            isVideoFinished = true;
            StartCoroutine(LoadNextSceneWithDelay());
        }
    }

    /// <summary>
    /// 跳过视频
    /// </summary>
    void SkipVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        if (!isVideoFinished)
        {
            isVideoFinished = true;
            LoadNextScene();
        }
    }

    /// <summary>
    /// 延迟后加载下一个场景
    /// </summary>
    IEnumerator LoadNextSceneWithDelay()
    {
        if (delayAfterVideo > 0)
        {
            yield return new WaitForSeconds(delayAfterVideo);
        }
        
        LoadNextScene();
    }

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("未设置下一个场景名称！");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}
