using UnityEngine;

/// <summary>
/// このオブジェクトを目標として設定し、プレイヤーが作動させたときに目標を完了します。
/// </summary>
public class TargetObject : MonoBehaviour
{
    [Tooltip("この目標を完了した後、関連するゲームプレイ処理を行うドアなど。")]
    public BossDoorController targetDoor;

    public Animator animator;

    public string animationTriggerName = "PullLever";

    private void Awake()
    {
        // もしInspectorで設定されていなければ、このゲームオブジェクトから取得を試みる
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // レバーを操作できるトリガーコライダーがアタッチされていることを前提とします

    /// <summary>
    /// プレイヤーがレバーを下ろすなどのアクションを実行したときに呼び出されます。
    /// </summary>
    public void PlayerInteractionCompleted()
    {

            // Animatorのトリガーを設定してアニメーションを再生
            animator.SetTrigger(animationTriggerName);
        

        Debug.Log($"{gameObject.name} を操作しました");
        targetDoor.OpenDoor(); // Fキーでレバーを下ろす
        TargetManager.Instance.CompleteCurrentObjective();
        enabled = false;
    }

    // ------------------------------------------------------------------
    // プレイヤーが近づいたときの処理
    // ------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        // PlayerにアタッチされたPlayerInteractionコンポーネントを探す
        PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

        if (playerInteraction != null)
        {
            // プレイヤーに、自分が操作可能なオブジェクトであることを通知する
            playerInteraction.SetInteractable(this);

            // UIプロンプトを表示する (デバッグログで代替)
            Debug.Log($"操作プロンプトを表示: {playerInteraction.interactKey}キーでレバーを下ろす");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // PlayerにアタッチされたPlayerInteractionコンポーネントを探す
        PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

        if (playerInteraction != null)
        {
            // プレイヤーに、自分が操作可能なオブジェクトではなくなったことを通知する
            playerInteraction.ClearInteractable(this);

            // UIプロンプトを非表示にする (デバッグログで代替)
            Debug.Log("操作プロンプトを非表示");
        }
    }
}