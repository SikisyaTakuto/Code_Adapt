using UnityEngine;
using System.Collections; // コルーチンを使うために必要

public class BGMManager : MonoBehaviour
{
    public static BGMManager instance;

    [SerializeField] private AudioSource audioSource;

    [Header("BGM Settings")]
    [SerializeField] private AudioClip normalBGM;
    [SerializeField] private AudioClip bossBGM;
    [SerializeField] private AudioClip clearBGM;
    [SerializeField] private AudioClip gameOverBGM;

    [Header("Fade Settings")]
    [SerializeField, Range(0.1f, 2.0f)] private float fadeDuration = 1.0f; // フェードにかかる時間

    private Coroutine fadeCoroutine;
    private float defaultVolume;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // インスペクターで設定された音量を初期値として保存
        defaultVolume = audioSource.volume;

        PlayBGM(normalBGM);
    }

    // フェードしながら曲を変える
    public void ChangeBGM(AudioClip newClip)
    {
        if (audioSource.clip == newClip || newClip == null) return;

        // すでにフェード実行中なら止める
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // フェード処理を開始
        fadeCoroutine = StartCoroutine(FadeChangeProcess(newClip));
    }

    private IEnumerator FadeChangeProcess(AudioClip newClip)
    {
        // 1. 音量を徐々に下げる (フェードアウト)
        float startVolume = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0;

        // 2. 曲を切り替えて再生
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // 3. 音量を徐々に上げる (フェードイン)
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0, defaultVolume, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = defaultVolume;

        fadeCoroutine = null;
    }

    // ショートカットメソッドはそのまま使えます
    public void PlayBossBGM() => ChangeBGM(bossBGM);
    public void PlayClearBGM() => ChangeBGM(clearBGM);
    public void PlayGameOverBGM() => ChangeBGM(gameOverBGM);
    public void PlayNormalBGM() => ChangeBGM(normalBGM);

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = defaultVolume;
        audioSource.Play();
    }
}