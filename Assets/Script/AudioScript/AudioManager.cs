using UnityEngine;
using UnityEngine.Audio; // Audio Mixerを使用するために必須
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // シングルトンのインスタンス
    public static AudioManager Instance;

    [Header("Audio Mixer Settings")]
    [Tooltip("作成したAudio Mixerアセットをここに設定")]
    public AudioMixer gameAudioMixer;

    // Audio Mixerで公開したパラメータの文字列（正確に入力が必要）
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";

    [Header("Audio Sources")]
    [Tooltip("BGM用AudioSourceをアタッチ")]
    [SerializeField]
    private AudioSource bgmSource;
    [Tooltip("SFX用AudioSourceをアタッチ")]
    [SerializeField]
    private AudioSource sfxSource;

    // スクリプトの初期化時に一度だけ実行
    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();

            // Audio Mixerが設定されているか確認
            if (gameAudioMixer == null)
            {
                Debug.LogError("AudioMixerが設定されていません。GameAudioMixerアセットをアタッチしてください。");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // AudioSourceの初期設定
    private void InitializeAudioSources()
    {
        // BGM用AudioSourceの初期設定とグループ設定
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        // SFX用AudioSourceの初期設定とグループ設定
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Audio MixerグループをAudioSourceに割り当て
        if (gameAudioMixer != null)
        {
            AudioMixerGroup[] groups = gameAudioMixer.FindMatchingGroups("BGM");
            if (groups.Length > 0) bgmSource.outputAudioMixerGroup = groups[0];

            groups = gameAudioMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0) sfxSource.outputAudioMixerGroup = groups[0];
        }
    }

    // --- BGM管理メソッド ---

    /// <summary>
    /// BGMを再生します（フェードインなしの即時再生）。
    /// </summary>
    /// <param name="clip">再生するAudioClip。</param>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 追加機能: BGMをクロスフェードで切り替える
    public void CrossFadeBGM(AudioClip newClip, float fadeDuration = 1.0f)
    {
        if (newClip == null || bgmSource.clip == newClip) return;

        StartCoroutine(FadeBGMCoroutine(newClip, fadeDuration));
    }

    private IEnumerator FadeBGMCoroutine(AudioClip newClip, float duration)
    {
        float startVolume;
        // 現在のBGM音量（ミキサー設定値）を取得
        gameAudioMixer.GetFloat(BGM_VOLUME_PARAM, out startVolume);

        // 取得したデシベル値を線形値 (0-1) に変換
        // UnityのAudioMixerは-80dBが最小、0dBが最大（1.0）
        float maxLinearVolume = Mathf.Pow(10, startVolume / 20);

        // フェードアウト
        float timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(maxLinearVolume, 0f, timer / (duration / 2f));
            // AudioSource.volumeで一時的に音量を制御（ミキサーのパラメータはそのまま）
            bgmSource.volume = newVolume;
            yield return null;
        }

        // クリップ切り替えとフェードイン開始
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(0f, maxLinearVolume, timer / (duration / 2f));
            bgmSource.volume = newVolume;
            yield return null;
        }

        // 最終的な音量をミキサーのパラメータに戻す
        bgmSource.volume = maxLinearVolume;
    }

    /// <summary>
    /// BGMを停止します。
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // --- SFX（SE）管理メソッド ---

    /// <summary>
    /// SEを再生します（一回限り）。
    /// </summary>
    /// <param name="clip">再生するAudioClip。</param>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        // PlayOneShotでミキサー設定の音量でSEを再生
        sfxSource.PlayOneShot(clip);
    }

    // --- 音量調整メソッド ---

    /// <summary>
    /// BGMの音量を設定します (0.0f～1.0fの線形値で、内部でデシベルに変換)。
    /// </summary>
    /// <param name="linearVolume">0.0fから1.0fの値。</param>
    public void SetBGMVolume(float linearVolume)
    {
        // 線形値 (0-1) をデシベル値 (-80dB-0dB) に変換
        float dB = Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20;

        // Audio Mixerの公開パラメータを設定
        gameAudioMixer.SetFloat(BGM_VOLUME_PARAM, dB);
    }

    /// <summary>
    /// SEの音量を設定します (0.0f～1.0fの線形値で、内部でデシベルに変換)。
    /// </summary>
    /// <param name="linearVolume">0.0fから1.0fの値。</param>
    public void SetSFXVolume(float linearVolume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20;
        gameAudioMixer.SetFloat(SFX_VOLUME_PARAM, dB);
    }

    /// <summary>
    /// Master音量を設定します（MasterVolumeパラメータも公開する必要があります）。
    /// </summary>
    /// <param name="linearVolume">0.0fから1.0fの値。</param>
    public void SetMasterVolume(float linearVolume)
    {
        // MasterVolumeパラメータをAudio Mixerで公開している場合のみ機能
        float dB = Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20;
        gameAudioMixer.SetFloat("MasterVolume", dB); // 仮に"MasterVolume"としています
    }
}