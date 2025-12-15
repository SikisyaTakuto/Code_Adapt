using UnityEngine;
using System.Collections.Generic;

// Unityエディタのインスペクターで設定できるようにSerializable属性を付ける
[System.Serializable]
public class MissionData
{
    [Tooltip("ミッションの表示テキスト")]
    public string missionText;

    [Tooltip("ミッションのクリア条件を識別するためのID。実際のゲームロジックで利用する。")]
    public string missionID;

    [HideInInspector]
    public bool isCompleted = false;
}