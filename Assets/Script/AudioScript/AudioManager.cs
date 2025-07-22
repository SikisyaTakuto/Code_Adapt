using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource seAudioSource;
    [SerializeField] private AudioClip buttonClickClip; // ボタンクリック時のSE

    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SE_VOLUME_PARAM = "SEVolume";

<<<<<<< HEAD
    public static AudioManager Instance { get; private set; } // シングルトンのインスタンス

    void Awake()
    {
        // ① Instanceがまだ設定されていない（最初のAudioManagerオブジェクト）
        if (Instance == null)
        {
            Instance = this; // このオブジェクトをシングルトンインスタンスとして設定
            DontDestroyOnLoad(gameObject); // このGameObjectをシーン遷移で破棄しないように設定
        }
        // ② 既にInstanceが存在する（別のシーンにAudioManagerが置かれていた場合など）
        else
        {
            Destroy(gameObject); // 新しく作られた（重複する）AudioManagerオブジェクトを破棄
=======
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいで存在させる
        }
        else
        {
            Destroy(gameObject);
>>>>>>> New
        }
    }

    void Start()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }

        float currentBGMVolume;
        if (mainMixer.GetFloat(BGM_VOLUME_PARAM, out currentBGMVolume))
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = currentBGMVolume;
            }
        }

        float currentSEVolume;
        if (mainMixer.GetFloat(SE_VOLUME_PARAM, out currentSEVolume))
        {
            if (seSlider != null)
            {
                seSlider.value = currentSEVolume;
            }
        }
    }

    public void SetBGMVolume(float volume)
    {
        mainMixer.SetFloat(BGM_VOLUME_PARAM, volume);
        Debug.Log($"BGM Volume: {volume} dB");
    }

    public void SetSEVolume(float volume)
    {
        mainMixer.SetFloat(SE_VOLUME_PARAM, volume);
        Debug.Log($"SE Volume: {volume} dB");
        PlaySE(buttonClickClip); // スライダーを動かしたときに効果音を鳴らす（元からある機能）
    }

    public void PlayButtonClickSE()
    {
        PlaySE(buttonClickClip);
    }

    public void PlaySE(AudioClip clip)
    {
        if (seAudioSource != null && clip != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SE AudioSource or AudioClip is null. Cannot play SE.");
        }
    }
}