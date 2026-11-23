using System.Collections;
using UnityEngine;

/// <summary>
/// 统一音频管理器 (AudioManager)
/// 负责管理 Welcoming / Hub 阶段的连续背景音乐 (支持平滑淡入/淡出)，
/// 以及全局通用 UI/UX 交互音效 (Button Click / Pop / Complete 等)。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM (Ambient Music)")]
    [Tooltip("背景音乐 AudioSource (不指定则自动创建)")]
    public AudioSource bgmSource;

    [Tooltip("默认背景音乐片段 (如 Mt. Berkoo 系列)")]
    public AudioClip defaultBgmClip;

    [Tooltip("BGM 目标音量 (0~1)")]
    [Range(0f, 1f)]
    public float bgmMaxVolume = 0.4f;

    [Tooltip("默认淡入/淡出过渡时长 (秒)")]
    public float defaultFadeDuration = 1.5f;

    [Tooltip("是否在游戏启动时自动淡入背景音乐")]
    public bool autoPlayOnStart = true;

    [Header("UX Sound Effects (SFX)")]
    [Tooltip("音效专用 AudioSource (不指定则自动创建)")]
    public AudioSource sfxSource;

    [Tooltip("通用按钮点击音效 (可选)")]
    public AudioClip defaultButtonClickClip;

    [Tooltip("确认/成功音效 (可选)")]
    public AudioClip defaultConfirmClip;

    [Tooltip("SFX 音量 (0~1)")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;

    private Coroutine bgmFadeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 如果希望跨场景常驻，可取消注释下面这行；如果在单场景中也可正常运作
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetupSources();
    }

    void Start()
    {
        if (autoPlayOnStart && defaultBgmClip != null)
        {
            FadeInBGM(defaultBgmClip, defaultFadeDuration);
        }
    }

    private void SetupSources()
    {
        // 1. 初始化 BGM Source
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f; // 2D 环绕背景音
        bgmSource.volume = 0f;       // 由 FadeIn 控制渐强

        // 2. 初始化 SFX Source
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D 交互音效
        sfxSource.volume = sfxVolume;
    }

    #region BGM Controls

    /// <summary>
    /// 平滑淡入背景音乐（若音乐已在播放且片段相同，则保持播放仅平滑调整音量）
    /// </summary>
    public void FadeInBGM(AudioClip clip = null, float duration = -1f)
    {
        if (clip == null) clip = defaultBgmClip;
        if (clip == null) return;

        if (duration < 0f) duration = defaultFadeDuration;

        // 如果已经在播放相同的 Clip 且音量已经正常，则不打断
        if (bgmSource.isPlaying && bgmSource.clip == clip && Mathf.Approximately(bgmSource.volume, bgmMaxVolume))
        {
            return;
        }

        if (bgmSource.clip != clip)
        {
            bgmSource.clip = clip;
            bgmSource.volume = 0f;
        }

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeAudioRoutine(bgmSource, bgmMaxVolume, duration, stopOnZero: false));
    }

    /// <summary>
    /// 平滑淡出背景音乐并暂停
    /// </summary>
    public void FadeOutBGM(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        if (!bgmSource.isPlaying) return;

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeAudioRoutine(bgmSource, 0f, duration, stopOnZero: true));
    }

    private IEnumerator FadeAudioRoutine(AudioSource source, float targetVolume, float duration, bool stopOnZero)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;

        if (stopOnZero && Mathf.Approximately(targetVolume, 0f))
        {
            source.Pause();
        }
    }

    #endregion

    #region SFX Controls

    /// <summary>
    /// 播放通用按钮点击音效
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(defaultButtonClickClip);
    }

    /// <summary>
    /// 播放确认/到达/成功音效
    /// </summary>
    public void PlayConfirm()
    {
        PlaySFX(defaultConfirmClip);
    }

    /// <summary>
    /// 播放指定音效 (OneShot，多个音效可重叠播放且不互相打断)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    #endregion
}
