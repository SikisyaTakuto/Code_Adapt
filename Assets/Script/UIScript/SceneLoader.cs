using UnityEngine;
using UnityEngine.SceneManagement; // シーン管理のために必要

public class SceneLoader : MonoBehaviour
{
    // このメソッドをボタンのOnClickイベントに設定します
    public void LoadStageSelectScene()
    {
        Debug.Log("ゲームスタートボタンが押されました。ステージセレクト画面に移行します。");
        SceneManager.LoadScene("StageSelectScene"); // StageSelectSceneのシーン名を指定
    }
}