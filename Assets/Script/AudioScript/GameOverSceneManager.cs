using UnityEngine;

/// <summary>
/// ゲームオーバーシーン開始時にBGMを切り替えるスクリプト
/// </summary>
public class GameOverSceneManager : MonoBehaviour
{
    void Start()
    {
        // BGMManagerが存在するか確認してから再生
        if (BGMManager.instance != null)
        {
            BGMManager.instance.PlayGameOverBGM();
        }
        else
        {
            Debug.LogWarning("BGMManagerが見つかりません。");
        }
    }
}