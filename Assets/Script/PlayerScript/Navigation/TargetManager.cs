using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 目標の管理とシーケンス（順番）の実行を統合したクラス。
/// シーン内の空のオブジェクト（例：Manager）にアタッチしてください。
/// </summary>
public class TargetManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static TargetManager Instance { get; private set; }

    [Header("Objective Sequence")]
    [Tooltip("目標を達成する順番に、シーン内のオブジェクトをドラッグ＆ドロップしてください。")]
    [SerializeField] private List<Transform> objectiveSequence = new List<Transform>();

    // 現在の目標
    public Transform CurrentTarget { get; private set; }

    // 目標の変更を通知するイベント
    public event System.Action<Transform> OnTargetChanged;

    // 内部管理用のキュー
    private Queue<Transform> objectiveQueue = new Queue<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // シーンをまたぐ場合はコメントを解除
        // DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
        // インスペクターで設定されたリストがある場合、自動的にキューを開始
        if (objectiveSequence != null && objectiveSequence.Count > 0)
        {
            StartNewSequence(objectiveSequence);
        }
    }

    /// <summary>
    /// 新しい目標シーケンスを開始します（外部から新しいリストを渡すことも可能）
    /// </summary>
    public void StartNewSequence(List<Transform> objectives)
    {
        objectiveQueue = new Queue<Transform>(objectives);
        CompleteCurrentObjective(); // 最初の目標を取り出す
    }

    /// <summary>
    /// 現在の目標を完了し、次の目標へ進みます。
    /// </summary>
    public void CompleteCurrentObjective()
    {
        if (objectiveQueue.Count > 0)
        {
            CurrentTarget = objectiveQueue.Dequeue();
            OnTargetChanged?.Invoke(CurrentTarget);
            Debug.Log($"<color=cyan>次の目標:</color> {CurrentTarget.name}");
        }
        else
        {
            CurrentTarget = null;
            OnTargetChanged?.Invoke(null);
            Debug.Log("<color=green>すべての目標が完了しました！</color>");
        }
    }

    /// <summary>
    /// (既存互換用) 単一のターゲットを強制的に設定する場合
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        CurrentTarget = newTarget;
        OnTargetChanged?.Invoke(CurrentTarget);
    }
}