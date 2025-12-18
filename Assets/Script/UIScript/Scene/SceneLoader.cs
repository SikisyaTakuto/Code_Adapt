using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UIボタンからのシーン遷移、およびゲーム終了を制御する汎用スクリプト。
/// サウンド処理を削除した軽量版。
/// </summary>
public class SceneLoader : MonoBehaviour
{
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

        Debug.Log($"シーンを遷移します: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ゲームを強制終了します。
    /// UIボタンのOnClickイベントに設定するためのパブリックメソッドです。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("ゲーム終了処理が呼び出されました。");

        // アプリケーションを終了
        Application.Quit();

#if UNITY_EDITOR
        // エディタ上では終了しないため、再生モードを終了させる
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}