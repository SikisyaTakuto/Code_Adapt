using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゲーム開始時に目標キューを設定するスクリプト。
/// シーン内の空のオブジェクトにアタッチしてください。
/// </summary>
public class LevelInitializer : MonoBehaviour
{
    // Inspectorで設定: シーン内の目標オブジェクトを順番にドラッグ＆ドロップ
    [Tooltip("目標を達成する順番に、シーン内のTargetObjectを接続してください。")]
    public List<TargetObject> objectiveSequence;

    void Start()
    {
        if (TargetManager.Instance == null)
        {
            Debug.LogError("TargetManagerがシーンに見つかりません。設定を確認してください。");
            return;
        }

        if (objectiveSequence == null || objectiveSequence.Count == 0)
        {
            Debug.LogError("目標シーケンスが設定されていません。InspectorでTargetObjectを接続してください。");
            return;
        }

        // TargetObjectのTransformだけを抽出したリストを作成
        List<Transform> initialObjectives = new List<Transform>();
        foreach (var obj in objectiveSequence)
        {
            initialObjectives.Add(obj.transform);
        }

        // 目標キューを設定し、最初の目標の表示を開始する
        TargetManager.Instance.SetNewObjectiveQueue(initialObjectives);
    }
}