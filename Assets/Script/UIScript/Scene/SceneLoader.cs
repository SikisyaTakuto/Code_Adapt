using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Audio; // AudioMixerGroup を使用するために追加

/// <summary>
/// UIボタンからのシーン遷移、およびゲーム終了を制御する汎用スクリプト。
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("オーディオ設定")]
    [Tooltip("ボタンクリック時に再生する効果音")]
    public AudioClip clickSound;

    // ★追加: AudioMixerGroup をインスペクターから設定するためのフィールド
    [Tooltip("AudioSourceをルーティングするAudioMixerGroup")]
    public AudioMixerGroup mixerGroup;

    // ★追加: 音量がゼロかどうかを確認するためのAudioMixerParameter名
    // 通常は "MasterVolume" や "SFXVolume" など、ミキサーで設定した露出パラメーター名を使用
    [Header("ボリューム設定 (Optional)")]
    [Tooltip("音量をチェックしたいAudioMixerのExposed Parameter名 (例: SFXVolume)")]
    public string volumeParameterName = "SFXVolume";

    private AudioSource audioSource;
    private AudioMixer audioMixer; // パラメーターチェックのために使用

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // ★追加: AudioMixerGroupが設定されていればAudioSourceに割り当てる
        if (mixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = mixerGroup;
            // AudioMixerの参照を取得（音量チェックに使用）
            audioMixer = mixerGroup.audioMixer;
        }
    }

    /// <summary>
    /// 指定された名前のシーンに遷移します。
    /// UIボタンのOnClickイベントに設定するためのパブリックメソッドです。
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("遷移先のシーン名が設定されていません。");
            return;
        }

        StartCoroutine(LoadSceneAfterSound(sceneName));
    }

    /// <summary>
    /// 効果音を再生してからシーンをロードするコルーチン。
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>
    private IEnumerator LoadSceneAfterSound(string sceneName)
    {
        bool isSoundMuted = IsVolumeMuted(); // ★追加: 音量チェック

        // 1. 効果音の再生
        if (audioSource != null && clickSound != null && !isSoundMuted)
        {
            audioSource.PlayOneShot(clickSound);
            Debug.Log($"効果音を再生します: {clickSound.name}");

            // 2. 音が鳴り終わるのを待つ
            // 再生時間 + 少しの余裕 (例: 0.1秒) を待つ
            yield return new WaitForSeconds(clickSound.length + 0.1f);
        }
        else if (isSoundMuted)
        {
            Debug.Log("効果音はミュートされています。");
        }


        // 3. シーン遷移の実行
        SceneManager.LoadScene(sceneName);
        Debug.Log($"シーンを遷移します: {sceneName}");
    }

    // =======================================================
    // ★★★ ゲーム終了メソッド ★★★
    // =======================================================

    /// <summary>
    /// ゲームを強制終了します。
    /// UIボタンのOnClickイベントに設定するためのパブリックメソッドです。
    /// </summary>
    public void QuitGame()
    {
        StartCoroutine(QuitGameAfterSound());
    }

    /// <summary>
    /// 効果音を再生してからゲームを終了するコルーチン。
    /// </summary>
    private IEnumerator QuitGameAfterSound()
    {
        bool isSoundMuted = IsVolumeMuted();

        // 1. 効果音の再生
        if (audioSource != null && clickSound != null && !isSoundMuted)
        {
            audioSource.PlayOneShot(clickSound);
            Debug.Log($"効果音を再生します: {clickSound.name}");

            // 2. 音が鳴り終わるのを待つ
            yield return new WaitForSeconds(clickSound.length + 0.1f);
        }
        else if (isSoundMuted)
        {
            Debug.Log("効果音はミュートされています。");
        }

        // 3. ゲーム終了の実行
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("ゲーム終了処理が呼び出されました。ビルドされたアプリケーションでのみ終了します。");
#endif
    }

    // =======================================================
    // ユーティリティ
    // =======================================================

    /// <summary>
    /// AudioMixerのボリュームがミュート状態（-80dB以下）かどうかをチェックします。
    /// </summary>
    private bool IsVolumeMuted()
    {
        if (audioMixer == null || string.IsNullOrEmpty(volumeParameterName))
        {
            // Mixerが設定されていない、またはパラメーター名がない場合はチェックをスキップ
            return false;
        }

        float volumeValue;
        if (audioMixer.GetFloat(volumeParameterName, out volumeValue))
        {
            // スライダーが0の場合、対応するExposed Parameterは通常 -80.0f
            // -79dB以下であればミュートと見なす
            return volumeValue < -79f;
        }

        // パラメーターが見つからなかった場合はミュートではないと見なす
        return false;
    }
}