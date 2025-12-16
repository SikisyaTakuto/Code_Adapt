// [System.Serializable]を付けることで、UnityのInspectorで設定可能になります。
using UnityEngine;

[System.Serializable]
public class Mission
{
    // ミッションのタイトル（UIに表示される内容）
    public string title;
    // ミッションを完了したかどうか
    [HideInInspector] public bool isCompleted = false;

    // (例: ミッションタイプや目標数などを追加すると、さらに汎用性が増します)
}