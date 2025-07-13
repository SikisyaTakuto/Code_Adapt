using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Coroutineのために必要

public class SceneLoader : MonoBehaviour
{
    // シーン切り替え時のSEの再生時間（SEの長さによる）
    [SerializeField] private float sePlayDuration = 0.5f;

    public void LoadStageSelectScene()
    {
        // まずSEを再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
            // SEが再生されるのを待ってからシーンをロードするコルーチンを開始
            StartCoroutine(LoadSceneAfterSE("StageSelectScene"));
        }
        else
        {
            Debug.LogError("AudioManager.Instance is null. Cannot play button click SE.");
            // AudioManagerがない場合でもシーンはロードする
            SceneManager.LoadScene("StageSelectScene");
        }
    }

    private IEnumerator LoadSceneAfterSE(string sceneName)
    {
        // SEの再生を待つ
        yield return new WaitForSeconds(sePlayDuration);

        Debug.Log("ゲームスタートボタンが押されました。ステージセレクト画面に移行します。");
        SceneManager.LoadScene(sceneName);
    }
}