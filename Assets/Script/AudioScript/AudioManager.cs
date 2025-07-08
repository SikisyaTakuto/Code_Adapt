using UnityEngine;
using UnityEngine.Audio;   // Audio Mixerを使うために必要
using UnityEngine.UI;      // UIスライダーを使うために必要

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;       // インスペクターからAudioMixerを割り当てる
    [SerializeField] private AudioSource bgmAudioSource; // インスペクターからBGMのAudioSourceを割り当てる
    [SerializeField] private AudioSource seAudioSource;  // インスペクターからSEのAudioSourceを割り当てる
    [SerializeField] private AudioClip buttonClickClip;  // ボタンクリック時のSE

    // スライダーの参照を追加
    [SerializeField] private Slider bgmSlider; // インスペクターからBGMのスライダーを割り当てる
    [SerializeField] private Slider seSlider;  // インスペクターからSEのスライダーを割り当てる

    private const string BGM_VOLUME_PARAM = "BGMVolume"; // AudioMixerで設定したBGMのパラメータ名
    private const string SE_VOLUME_PARAM = "SEVolume";   // AudioMixerで設定したSEのパラメータ名

    void Start()
    {
        // BGMを再生
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }

        // スライダーの初期値をAudioMixerの現在の値に合わせる
        // スライダーのMin/Max値とAudioMixerのdB値を合わせる必要があるため、計算が必要です。
        // AudioMixerのFloatパラメータは通常-80dB (ほぼ無音) から0dB (原音) の範囲で設定します。
        // スライダーも同様にMin -80, Max 0 に設定します。

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

    // BGMスライダーの値が変更されたときに呼び出されるメソッド
    public void SetBGMVolume(float volume)
    {
        // スライダーの値（-80f〜0f）をAudioMixerのパラメータに設定
        mainMixer.SetFloat(BGM_VOLUME_PARAM, volume);
        Debug.Log($"BGM Volume: {volume} dB");
    }

    // SEスライダーの値が変更されたときに呼び出されるメソッド
    public void SetSEVolume(float volume)
    {
        // スライダーの値（-80f〜0f）をAudioMixerのパラメータに設定
        mainMixer.SetFloat(SE_VOLUME_PARAM, volume);
        Debug.Log($"SE Volume: {volume} dB");

        // SEスライダーを動かしたときに効果音を鳴らす（オプション）
        PlaySE(buttonClickClip);
    }

    // SEを再生する汎用メソッド (既存のまま)
    public void PlaySE(AudioClip clip)
    {
        if (seAudioSource != null && clip != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
    }
}