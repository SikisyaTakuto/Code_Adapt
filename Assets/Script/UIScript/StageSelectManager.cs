using UnityEngine;
using UnityEngine.SceneManagement; // シーン遷移のために必要

public class StageSelectManager : MonoBehaviour
{
    public static StageSelectManager Instance { get; private set; }

    [Header("遷移設定")]
    [Tooltip("防具選択シーンのシーン名")]
    public string armorSelectSceneName = "ArmorSelectScene"; // デフォルト値

    private string selectedStageName; // 選択されたステージのシーン名

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // シーン遷移後もこの情報を保持したい場合はDontDestroyOnLoadを使う
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ステージが選択されたときに呼び出されます。
    /// </summary>
    /// <param name="stageName">選択されたステージのシーン名</param>
    public void SelectStage(string stageName)
    {
        selectedStageName = stageName;
        Debug.Log($"選択されたステージ: {selectedStageName}");

        // 選択されたステージ情報を GameManager に保存（もしあれば）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedStage(selectedStageName);
        }

        // 防具選択シーンへ遷移
        SceneManager.LoadScene(armorSelectSceneName);
    }

    /// <summary>
    /// 選択されたステージのシーン名を取得します。
    /// </summary>
    /// <returns>選択されたステージのシーン名</returns>
    public string GetSelectedStageName()
    {
        return selectedStageName;
    }
}