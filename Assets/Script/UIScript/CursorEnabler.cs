using UnityEngine;

/// <summary>
/// マウスカーソルの表示とロックを解除するスクリプト。
/// シーン開始時に実行される。
/// </summary>
public class CursorEnabler : MonoBehaviour
{
    void Start()
    {
        // マウスカーソルを可視化する
        Cursor.visible = true;

        // カーソルロックモードを解除し、カーソルを画面中央に固定しないようにする
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("マウスカーソルを有効化しました。");
    }
}