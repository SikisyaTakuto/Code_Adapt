using UnityEngine;
using UnityEngine.SceneManagement; // シーン遷移のために必要

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private string _selectedStageName; // 選択されたステージのシーン名

    // 前のシーンで選択されたステージ名などを保持することも可能
    // public string selectedStageName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 指定されたシーン名にロードします。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"シーン '{sceneName}' をロードします。");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 選択されたステージのシーン名を設定します。
    /// </summary>
    /// <param name="stageName">選択されたステージのシーン名</param>
    public void SetSelectedStage(string stageName)
    {
        _selectedStageName = stageName;
    }

    /// <summary>
    /// 選択されたステージのシーン名を取得します。
    /// </summary>
    /// <returns>選択されたステージのシーン名</returns>
    public string GetSelectedStageName()
    {
        return _selectedStageName;
    }

    /// <summary>
    /// 最終的に選択されたステージに遷移します。
    /// 例えば、ArmorSelectSceneで防具選択後、このメソッドを呼び出してステージへ遷移します。
    /// </summary>
    public void GoToSelectedStage()
    {
        if (!string.IsNullOrEmpty(_selectedStageName))
        {
            Debug.Log($"選択されたステージ {_selectedStageName} へ移動します。");
            SceneManager.LoadScene(_selectedStageName);
        }
        else
        {
            Debug.LogError("選択されたステージがありません。");
        }
    }
}