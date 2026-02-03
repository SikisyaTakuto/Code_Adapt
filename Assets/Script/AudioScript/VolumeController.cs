using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    // 保存用のキー名
    private const string BGM_KEY = "BGM_VOLUME";
    private const string SE_KEY = "SE_VOLUME";

    private void Start()
    {
        // 1. 保存されている値を取得（なければデフォルト値 1.0f）
        float savedBGM = PlayerPrefs.GetFloat(BGM_KEY, 1.0f);
        float savedSE = PlayerPrefs.GetFloat(SE_KEY, 1.0f);

        // 2. スライダーの見た目を保存されていた値に合わせる
        bgmSlider.value = savedBGM;
        seSlider.value = savedSE;

        // 3. AudioMixerにも値を適用する
        SetBGMVolume(savedBGM);
        SetSEVolume(savedSE);

        // リスナー登録
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolume);
    }

    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
        // 値を保存
        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSEVolume(float volume)
    {
        audioMixer.SetFloat("SEVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
        // 値を保存
        PlayerPrefs.SetFloat(SE_KEY, volume);
    }

    private void OnDisable()
    {
        // シーン遷移時などに確実に保存を実行
        PlayerPrefs.Save();
    }

    public void OnSliderPointerUp()
    {
        if (SEManager.instance != null)
        {
            SEManager.instance.PlaySE();
        }
    }
}