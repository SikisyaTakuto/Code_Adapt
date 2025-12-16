using UnityEngine;

/// <summary>
/// このオブジェクトを目標として設定し、プレイヤーが作動させたときに目標を完了します。
/// </summary>
[RequireComponent(typeof(Collider))] // トリガー検出のためColliderを必須とする
public class TargetObject : MonoBehaviour
{
    // === 外部参照と設定 ===

    [Tooltip("この目標を完了した後、関連するゲームプレイ処理を行うドアなど。")]
    // ※このスクリプトがシーンにある前提
    public BossDoorController targetDoor;

    public Animator animator;

    public string animationTriggerName = "PullLever";

    // 状態管理のためのフラグ (一度しか操作できないようにする)
    private bool isActivated = false;

    private void Awake()
    {
        // もしInspectorで設定されていなければ、このゲームオブジェクトから取得を試みる
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // ColliderがIs Triggerであるか確認 (必須ではないが推奨)
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name} のColliderは Is Trigger に設定されていません。検出が機能しない可能性があります。");
        }
    }

    // ------------------------------------------------------------------
    // プレイヤーからの操作完了通知
    // ------------------------------------------------------------------

    /// <summary>
    /// PlayerInteractionスクリプトがFキー入力を受けたときに呼び出すメソッド。
    /// </summary>
    public void PlayerInteractionCompleted()
    {
        // 既に操作済みであれば何もしない
        if (isActivated) return;
        isActivated = true;

        // Animatorのトリガーを設定してアニメーションを再生
        if (animator != null)
        {
            animator.SetTrigger(animationTriggerName);
        }

        Debug.Log($"{gameObject.name} を操作しました");

        // 関連処理の実行 (ドアを開けるなど)
        if (targetDoor != null)
        {
            targetDoor.OpenDoor();
        }

        // MissionManagerに現在のミッション完了を通知
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CompleteCurrentMission();
        }
        else
        {
            Debug.LogError("MissionManager (Singleton) がシーン内に見つかりません。");
        }

        // 操作が完了したため、Colliderなどを無効化して再度の操作を防止することもできます
        // GetComponent<Collider>().enabled = false;
        // gameObject.SetActive(false); 
    }

    // ------------------------------------------------------------------
    // プレイヤーが近づいたときの処理
    // ------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        // 既にアクティベート済み、またはプレイヤーではない場合は処理しない
        if (isActivated || !other.CompareTag("Player")) return;

        // PlayerにアタッチされたPlayerInteractionコンポーネントを探す
        PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

        if (playerInteraction != null)
        {
            // プレイヤーに、自分が操作可能なオブジェクトであることを通知する
            playerInteraction.SetInteractable(this);

            Debug.Log($"操作プロンプトを表示: {playerInteraction.interactKey}キーで操作");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // プレイヤーではない場合は処理しない
        if (!other.CompareTag("Player")) return;

        // PlayerにアタッチされたPlayerInteractionコンポーネントを探す
        PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

        if (playerInteraction != null)
        {
            // プレイヤーに、自分が操作可能なオブジェクトではなくなったことを通知する
            playerInteraction.ClearInteractable(this);

            Debug.Log("操作プロンプトを非表示");
        }
    }
}