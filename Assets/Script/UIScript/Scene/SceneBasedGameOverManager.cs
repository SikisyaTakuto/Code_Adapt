using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 現在のシーン名に基づき、適切なゲームオーバーシーンに遷移させるマネージャースクリプト。
/// PlayerControllerからHPが0になったときに呼び出されます。
/// </summary>
public class SceneBasedGameOverManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMapping
    {
        [Tooltip("ゲームプレイ中のシーン名 (例: GameScene, TutorialScene)")]
        public string gameSceneName;
        [Tooltip("対応するゲームオーバーシーン名 (例: GameSceneGameOverScene)")]
        public string gameOverSceneName;
    }

    [Header("シーン マッピング設定")]
    [Tooltip("ゲームシーン名と、それに対応するゲームオーバーシーン名のリストを設定します。")]
    public List<SceneMapping> sceneMappings = new List<SceneMapping>();

    [Tooltip("マッピングが見つからなかった場合のフォールバックシーン名 (任意)")]
    public string defaultGameOverSceneName = "DefaultGameOverScene";

    /// <summary>
    /// プレイヤーの死亡時に、現在のシーンに基づいて適切なゲームオーバーシーンへ遷移させます。
    /// </summary>
    public void GoToGameOverScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        string targetGameOverScene = null;

        // 1. マッピングリストから対応するゲームオーバーシーンを探す
        // LINQを使用して、現在のシーン名と一致するマッピングを探します
        SceneMapping foundMapping = sceneMappings.FirstOrDefault(m => m.gameSceneName == currentSceneName);

        if (foundMapping != null)
        {
            targetGameOverScene = foundMapping.gameOverSceneName;
        }

        // 2. 遷移先の決定
        if (!string.IsNullOrEmpty(targetGameOverScene))
        {
            Debug.Log($"シーン '{currentSceneName}' でゲームオーバー: '{targetGameOverScene}' へ遷移します。");
            // ★ シーン遷移の実行
            SceneManager.LoadScene(targetGameOverScene);
        }
        else if (!string.IsNullOrEmpty(defaultGameOverSceneName))
        {
            Debug.LogWarning($"現在のシーン '{currentSceneName}' のマッピングが見つかりません。デフォルトのシーン '{defaultGameOverSceneName}' へ遷移します。");
            // ★ デフォルトシーンへの遷移
            SceneManager.LoadScene(defaultGameOverSceneName);
        }
        else
        {
            Debug.LogError($"ゲームオーバーシーンが見つかりません。現在のシーン: {currentSceneName}。マッピングとデフォルトシーンの設定を確認してください。");
        }
    }
}