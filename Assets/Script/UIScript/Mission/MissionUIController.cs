using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 個々のミッションUI要素（チェックマークとテキスト）を管理します。
/// </summary>
public class MissionUIController : MonoBehaviour
{
    [Tooltip("ミッションのチェックマーク（例：? を表示するTextコンポーネント）")]
    public Text checkmarkText;

    [Tooltip("ミッションのテキストを表示するTextコンポーネント")]
    public Text missionText;

    [Tooltip("ミッションリスト全体を格納する親パネル（表示/非表示用）")]
    public GameObject missionPanel;

    // 現在表示中のミッションUIオブジェクトのリスト
    private List<GameObject> missionObjects = new List<GameObject>();
    private MissionData currentMissionData;

    /// <summary>
    /// UIを初期化し、全てのミッションUIを非表示にします。
    /// </summary>
    public void InitializeUI(MissionData initialData)
    {
        // UIの初期状態として、例えば最初のミッションデータを設定できます
        UpdateMissionUI(initialData);

        // チェックマークを最初は非表示にする
        if (checkmarkText != null)
        {
            checkmarkText.text = ""; // または Image.enabled = false;
        }
    }

    /// <summary>
    /// 指定されたミッションデータでUIを更新します。
    /// </summary>
    /// <param name="data">表示するミッションデータ。</param>
    public void UpdateMissionUI(MissionData data)
    {
        currentMissionData = data;

        if (missionText != null)
        {
            missionText.text = data.missionText;
        }

        // 完了状態を反映
        SetCompleted(data.isCompleted);
    }

    /// <summary>
    /// ミッションの完了状態をUIに反映します。
    /// </summary>
    /// <param name="isComplete">完了したかどうか。</param>
    public void SetCompleted(bool isComplete)
    {
        if (checkmarkText != null)
        {
            // 完了していれば '?' を表示、そうでなければ空欄
            checkmarkText.text = isComplete ? "?" : "";
            // または、isComplete ? Color.green : Color.white などで色を変えることもできます
        }

        // ミッションテキスト自体も、完了したら灰色にするなどの演出も可能
        if (isComplete && missionText != null)
        {
            missionText.color = Color.gray;
        }
    }
}