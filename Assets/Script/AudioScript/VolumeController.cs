using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer; // 手順1で作ったMixerをアサイン
    [SerializeField] private Slider bgmSlider;      // BGM用スライダーをアサイン
    [SerializeField] private Slider seSlider;       // SE用スライダーをアサイン

    private void Start()
    {
        // スライダーの初期値を設定 (PlayerPrefsなどで保存している場合はそこから読み込む)
        // AudioMixerのVolumeはデシベル(dB)なので、スライダーは0.0001?1の範囲にするのが一般的です
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolume);
    }

    public void SetBGMVolume(float volume)
    {
        // スライダーの値(0?1)をデシベル(-80?20)に変換
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
    }

    public void SetSEVolume(float volume)
    {
        audioMixer.SetFloat("SEVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
    }

    // スライダーから手を離した時に呼び出す関数
    public void OnSliderPointerUp()
    {
        if (SEManager.instance != null)
        {
            SEManager.instance.PlaySE();
        }
    }
}