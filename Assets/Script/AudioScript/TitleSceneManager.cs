using UnityEngine;

/// <summary>
/// タイトルシーン開始時にBGMをNormalに戻すスクリプト
/// </summary>
public class TitleSceneManager : MonoBehaviour
{
    void Start()
    {
        // シーンが読み込まれた瞬間にBGMを通常のものに切り替える
        if (BGMManager.instance != null)
        {
            BGMManager.instance.PlayNormalBGM();
        }
    }
}