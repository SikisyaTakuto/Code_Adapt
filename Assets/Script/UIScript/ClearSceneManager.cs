using UnityEngine;
using UnityEngine.SceneManagement; // SceneManagerを使用するために必要

/// <summary>
/// ゲームクリアシーンの管理とタイトルシーンへの遷移を行うスクリプト。
/// </summary>
public class ClearSceneManager : MonoBehaviour
{
    [Tooltip("タイトルシーンのビルド設定での名前。")]
    public string titleSceneName = "TitleScene"; // タイトルシーンの名前をInspectorで設定できるようにする

    void Start()
    {
        // マウスカーソルを再度表示し、ロック状態を解除する
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("マウスカーソルを表示しました。");
    }

    /// <summary>
    /// 「タイトルに戻る」ボタンが押されたときに呼び出されるメソッド。
    /// </summary>
    public void BackToTitle()
    {
        Debug.Log("Back to Title button pressed. Loading TitleScene...");
        // 指定された名前のシーンをロード
        SceneManager.LoadScene(titleSceneName);
    }
}
