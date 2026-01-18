using UnityEngine;

/// <summary>
/// ゲームシーン開始時にBGMをNormalに切り替えるスクリプト
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    void Start()
    {
        // ゲームシーン開始時にNormalBGMを再生
        if (BGMManager.instance != null)
        {
            BGMManager.instance.PlayNormalBGM();
        }
    }
}