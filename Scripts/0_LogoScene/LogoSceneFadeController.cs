using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 带淡入淡出效果的Logo场景控制器
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class LogoSceneFadeController : MonoBehaviour
{
    [Header("视频设置")]
    public VideoClip logoVideo;
    
    [Header("场景跳转设置")]
    public string nextSceneName = "MainMenu";
    public bool allowSkip = true;
    public float delayAfterVideo = 0.5f;
    
    [Header("淡入淡出设置")]
    [Tooltip("淡入时间（秒）")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("淡出时间（秒）")]
    public float fadeOutDuration = 0.5f;
    
    [Tooltip("淡入淡出遮罩（可选，不设置则自动创建）")]
    public Image fadeImage;

    private VideoPlayer videoPlayer;
    private bool isVideoFinished = false;
    private CanvasGroup fadeCanvasGroup;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        SetupVideoPlayer();
        SetupFadeImage();
    }

    void Start()
    {
        StartCoroutine(PlayLogoSequence());
    }

    void Update()
    {
        if (allowSkip && !isVideoFinished)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                StopAllCoroutines();
                StartCoroutine(SkipToNextScene());
            }
        }
    }

    void SetupVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.isLooping = false;
        
        // 音频设置 - 解决音频缓冲区溢出问题
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.skipOnDrop = true; // 允许跳帧以保持同步
        
        if (logoVideo != null)
        {
            videoPlayer.clip = logoVideo;
        }
        
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }
    
    void OnVideoPrepared(VideoPlayer vp)
    {
        // 视频准备完成后开始播放
        videoPlayer.Play();
    }

    void SetupFadeImage()
    {
        // 如果没有指定淡入淡出图片，自动创建
        if (fadeImage == null)
        {
            // 创建Canvas
            GameObject canvasObj = new GameObject("FadeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 创建黑色遮罩
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);
            
            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = Color.black;
            
            RectTransform rect = fadeImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
        
        // 添加CanvasGroup用于控制透明度
        fadeCanvasGroup = fadeImage.GetComponent<CanvasGroup>();
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初始设置为完全不透明
        fadeCanvasGroup.alpha = 1f;
    }

    IEnumerator PlayLogoSequence()
    {
        // 淡入（从黑屏到显示视频）
        yield return StartCoroutine(FadeIn());
        
        // 准备并播放视频
        if (logoVideo != null)
        {
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogWarning("未设置Logo视频");
            yield return new WaitForSeconds(2f);
            OnVideoFinished(videoPlayer);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (!isVideoFinished)
        {
            isVideoFinished = true;
            StartCoroutine(TransitionToNextScene());
        }
    }

    IEnumerator TransitionToNextScene()
    {
        // 等待延迟
        if (delayAfterVideo > 0)
        {
            yield return new WaitForSeconds(delayAfterVideo);
        }
        
        // 淡出
        yield return StartCoroutine(FadeOut());
        
        // 加载下一个场景
        LoadNextScene();
    }

    IEnumerator SkipToNextScene()
    {
        isVideoFinished = true;
        
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        yield return StartCoroutine(FadeOut());
        LoadNextScene();
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - (elapsed / fadeInDuration);
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = elapsed / fadeOutDuration;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }

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
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}
