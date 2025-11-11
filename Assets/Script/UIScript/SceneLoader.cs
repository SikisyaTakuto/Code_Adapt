using UnityEngine;
using UnityEngine.SceneManagement; // シーン管理のために必須
using UnityEngine.UI;

/// <summary>
/// UIボタンからのシーン遷移を制御する汎用スクリプト。
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // --- シーン名の定数（UnityのBuild Settingsに登録された名前と一致させる） ---
    // ここにすべてのシーン名を定義しておくと、コード内でタイプミスを防げます。
    public const string TITLE_SCENE = "TitleScene";
    public const string STAGE_SELECT_SCENE = "StageSelectScene";
    public const string CLEAR_SCENE = "ClearScene";
    public const string GAME_OVER_SCENE = "GameOverScene";

    // 他のシーンもあれば、ここに追加してください。
    // public const string GAME_SCENE_1 = "GameScene1";

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

        // シーン遷移の実行
        SceneManager.LoadScene(sceneName);
        Debug.Log($"シーンを遷移します: {sceneName}");
    }
}