using UnityEngine;

/// <summary>
/// このオブジェクトを目標として設定し、プレイヤーが作動させたときに目標を完了します。
/// </summary>
public class TargetObject : MonoBehaviour
{
    [Tooltip("この目標を完了した後、関連するゲームプレイ処理を行うドアなど。")]
    public DoorController targetDoor;

    // レバーを操作できるトリガーコライダーがアタッチされていることを前提とします

    /// <summary>
    /// プレイヤーがレバーを下ろすなどのアクションを実行したときに呼び出されます。
    /// </summary>
    public void PlayerInteractionCompleted()
    {
        Debug.Log($"{gameObject.name} の目標を完了しました。");

        // 関連するゲームプレイ処理を実行（例：ドアを開ける）
        if (targetDoor != null)
        {
            // targetDoor.OpenDoor(); // ドアコントローラーが定義されていることを前提
        }

        // 目標管理システムに、この目標が完了したことを通知し、次の目標へ切り替える
        TargetManager.Instance.CompleteCurrentObjective();

        // 完了後、このコンポーネントを無効化し、二重起動を防ぐ
        enabled = false;

        // 完了したら、操作プロンプトも消すために通知（通常はPlayerInteraction側で処理される）
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