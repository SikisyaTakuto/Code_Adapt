using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // Audio Mixerを使うために必要

public class UISystem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject settingsPanel; // 設定パネル全体（背景やスライダーを含む）
    [SerializeField] private Slider bgmSlider;         // BGM用スライダー
    [SerializeField] private Text bgmVolumeText;       // BGMの音量表示テキスト
    [SerializeField] private Slider seSlider;          // SE用スライダー
    [SerializeField] private Text seVolumeText;        // SEの音量表示テキスト

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer mainMixer;     // Audio Mixerへの参照

    // Audio Mixerの公開パラメータ名（Inspectorで設定したものと一致させる）
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SE_VOLUME_PARAM = "SEVolume";

    void Start()
    {
        // 初期状態では設定パネルを非表示にする
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // スライダーにリスナーを追加
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        if (seSlider != null)
        {
            seSlider.onValueChanged.AddListener(SetSEVolume);
        }

        // Audio Mixerの現在の値を取得し、スライダーとテキストに反映
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// 設定パネルの表示/非表示を切り替える
    /// （設定ボタンのOnClickイベントに設定）
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    /// <summary>
    /// BGM音量を設定し、テキストを更新する
    /// （BGMスライダーのOnValueChangedイベントに設定）
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat(BGM_VOLUME_PARAM, volume);
            UpdateVolumeText(bgmVolumeText, volume);
        }
    }

    /// <summary>
    /// SE音量を設定し、テキストを更新する
    /// （SEスライダーのOnValueChangedイベントに設定）
    /// </summary>
    public void SetSEVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat(SE_VOLUME_PARAM, volume);
            UpdateVolumeText(seVolumeText, volume);
        }
    }

    /// <summary>
    /// スライダーとテキストに現在の音量設定をロードして適用する
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {
        float currentBGMVolume;
        if (mainMixer != null && mainMixer.GetFloat(BGM_VOLUME_PARAM, out currentBGMVolume))
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = currentBGMVolume; // スライダーの値を同期
            }
            UpdateVolumeText(bgmVolumeText, currentBGMVolume); // テキストを更新
        }

        float currentSEVolume;
        if (mainMixer != null && mainMixer.GetFloat(SE_VOLUME_PARAM, out currentSEVolume))
        {
            if (seSlider != null)
            {
                seSlider.value = currentSEVolume; // スライダーの値を同期
            }
            UpdateVolumeText(seVolumeText, currentSEVolume); // テキストを更新
        }
    }

    /// <summary>
    /// 音量表示テキストを更新するヘルパーメソッド
    /// </summary>
    private void UpdateVolumeText(Text volumeText, float volume)
    {
        if (volumeText != null)
        {
            // dB値をパーセンテージまたは分かりやすい数値に変換して表示
            // 例: -80dBを0%、0dBを100%として表示
            // dB値はリニアではないため、単純なパーセンテージ変換は感覚と異なる場合がある
            // ここでは簡易的に0.1f刻みで四捨五入して表示
            volumeText.text = $"{Mathf.Round(volume * 10f) / 10f} dB";
            // もしパーセンテージで表示したい場合は以下の計算を参考にしてください
            // float normalizedVolume = Mathf.InverseLerp(bgmSlider.minValue, bgmSlider.maxValue, volume);
            // volumeText.text = $"{Mathf.RoundToInt(normalizedVolume * 100)}%";
        }
    }
}