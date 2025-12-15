using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ミッションの進行を管理し、UI表示を制御します。
/// </summary>
public class MissionManager : MonoBehaviour
{
    [Header("ミッション設定")]
    [Tooltip("ゲーム内の全てのミッションリスト")]
    public List<MissionData> allMissions = new List<MissionData>();

    [Header("UI参照")]
    [Tooltip("UI表示を制御するコントローラー (Hierarchyのミッションオブジェクトにアタッチ)")]
    public MissionUIController missionUIController;

    [Header("デバッグ/状態")]
    [Tooltip("現在進行中のミッションのインデックス")]
    public int currentMissionIndex = 0;

    private void Start()
    {
        if (allMissions.Count > 0 && missionUIController != null)
        {
            // 最初のミッションでUIを初期化
            missionUIController.UpdateMissionUI(allMissions[currentMissionIndex]);
        }
        else
        {
            Debug.LogError("ミッションデータまたはUIコントローラーが設定されていません！");
        }
    }

    /// <summary>
    /// 外部のゲームロジック（例：敵の撃破、特定エリアへの到達）からミッション完了を試みるために呼び出されます。
    /// </summary>
    /// <param name="completedMissionID">完了したと判断されたミッションのID。</param>
    public void TryCompleteMission(string completedMissionID)
    {
        if (currentMissionIndex >= allMissions.Count)
        {
            Debug.Log("全てのミッションを完了しました。");
            return;
        }

        MissionData currentMission = allMissions[currentMissionIndex];

        // 現在のミッションIDと一致するか確認
        if (currentMission.missionID == completedMissionID && !currentMission.isCompleted)
        {
            CompleteCurrentMission();
        }
    }

    /// <summary>
    /// 現在のミッションを完了状態にし、次のミッションに進みます。
    /// </summary>
    private void CompleteCurrentMission()
    {
        MissionData currentMission = allMissions[currentMissionIndex];
        currentMission.isCompleted = true;

        // UIにチェックマークを付ける（現在のUIを更新）
        missionUIController.SetCompleted(true);

        Debug.Log($"ミッション '{currentMission.missionText}' を完了しました。");

        // 次のミッションに進む
        currentMissionIndex++;

        // 次のミッションが存在するかチェック
        if (currentMissionIndex < allMissions.Count)
        {
            MissionData nextMission = allMissions[currentMissionIndex];

            // UIを次のミッションに更新
            // ※ 画像のようにリスト表示したい場合は、UIの構造を工夫する必要があります（下記参照）
            // ここでは、UIを次のミッションに完全に置き換える（またはリストの次の要素を有効にする）シンプルな方法を採用
            missionUIController.UpdateMissionUI(nextMission);
            Debug.Log($"次のミッション: '{nextMission.missionText}'");
        }
        else
        {
            Debug.Log("全ミッション完了！");
        }
    }
}