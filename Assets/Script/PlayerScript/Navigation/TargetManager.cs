using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 現在の目標Transformを管理し、目標の変更を通知するシングルトンクラス。
/// </summary>
public class TargetManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static TargetManager Instance { get; private set; }

    // 現在の目標（TargetMarkerUIが追いかける対象）
    public Transform CurrentTarget { get; private set; }

    // 目標の変更を通知するイベント
    public event System.Action<Transform> OnTargetChanged;

    // ★ 追加: 目標のキュー (順番待ちリスト) ★
    private Queue<Transform> objectiveQueue = new Queue<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも永続化する場合
    }

    /// <summary>
    /// 新しい単一の目標を設定します。
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        CurrentTarget = newTarget;
        OnTargetChanged?.Invoke(CurrentTarget);

        if (newTarget != null)
        {
            Debug.Log($"新しい目標が設定されました: {newTarget.name}");
        }
        else
        {
            Debug.Log("目標がすべて完了しました。");
        }
    }

    // ★ 追加: 複数の目標を順番に設定するキュー ★
    /// <summary>
    /// 複数の目標を順番に処理するためのキューを設定し、最初の目標を開始します。
    /// </summary>
    public void SetNewObjectiveQueue(List<Transform> objectives)
    {
        objectiveQueue = new Queue<Transform>(objectives);
        CompleteCurrentObjective(); // キューの先頭から最初の目標を取り出す
    }

    /// <summary>
    /// 現在の目標を完了し、キューから次の目標があればそれを設定します。
    /// </summary>
    public void CompleteCurrentObjective()
    {
        // ログはTargetObject側で出力されるため、ここでは省略

        if (objectiveQueue.Count > 0)
        {
            // キューから次の目標を取り出し、CurrentTargetに設定
            Transform nextTarget = objectiveQueue.Dequeue();
            SetTarget(nextTarget);
        }
        else
        {
            // キューが空になったら、目標をnullに設定
            SetTarget(null);
        }
    }
}