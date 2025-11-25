using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// UIスライダーをAudio Managerと連携させるためのスクリプト。
/// </summary>
public class VolumeSliderController : MonoBehaviour
{
    // スライダーの種類をInspectorで設定
    public enum VolumeType { BGM, SFX }
    [Tooltip("このスライダーで調整する音量の種類")]
    public VolumeType type = VolumeType.BGM;

    private Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError("Sliderコンポーネントがアタッチされていません。");
            return;
        }

        // AudioManagerが利用可能になるまで待機
        StartCoroutine(InitializeSlider());
    }

    private System.Collections.IEnumerator InitializeSlider()
    {
        // AudioManagerがDontDestroyOnLoadされるため、Instanceが設定されるまで待機することが安全
        while (AudioManager.Instance == null)
        {
            yield return null;
        }

        // 1. スライダーの初期値を現在の音量に設定
        float currentVolume = 1.0f;
        if (type == VolumeType.BGM)
        {
            currentVolume = AudioManager.Instance.GetBGMVolume();
        }
        else if (type == VolumeType.SFX)
        {
            currentVolume = AudioManager.Instance.GetSFXVolume();
        }

        slider.value = currentVolume;

        // 2. スライダーの値が変更されたときのリスナーを設定
        slider.onValueChanged.AddListener(OnSliderValueChanged);

        // 3. スライダーの初期設定（0-1の範囲）を再確認
        slider.minValue = 0.0001f; // 0にするとLog10で無限大になり設定が崩れるため微小値にする
        slider.maxValue = 1.0f;
    }

    /// <summary>
    /// スライダーの値が変更されたときに呼び出される処理
    /// </summary>
    private void OnSliderValueChanged(float newValue)
    {
        if (AudioManager.Instance == null) return;

        // 3. Audio Managerの音量設定メソッドを呼び出す
        if (type == VolumeType.BGM)
        {
            AudioManager.Instance.SetBGMVolume(newValue);
        }
        else if (type == VolumeType.SFX)
        {
            AudioManager.Instance.SetSFXVolume(newValue);
        }
    }
}