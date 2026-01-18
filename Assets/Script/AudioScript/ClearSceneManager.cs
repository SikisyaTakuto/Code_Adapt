using UnityEngine;

/// <summary>
/// ゲームクリアシーン開始時にBGMを切り替えるスクリプト
/// </summary>
public class ClearSceneManager : MonoBehaviour
{
    void Start()
    {
        // BGMManagerが存在するか確認してから再生
        if (BGMManager.instance != null)
        {
            BGMManager.instance.PlayClearBGM();
        }
        else
        {
            Debug.LogWarning("BGMManagerが見つかりません。");
        }
    }
}