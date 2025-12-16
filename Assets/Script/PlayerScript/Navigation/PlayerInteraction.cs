using UnityEngine;

/// <summary>
/// プレイヤーの操作入力 (Fキーなど) を受け付け、
/// 近くにある操作可能なオブジェクト (TargetObject) とのインタラクションを処理します。
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // === 設定 ===
    [Header("Interaction Key")]
    [Tooltip("操作に使用するキー (例: KeyCode.F)")]
    public KeyCode interactKey = KeyCode.F;

    // === 内部状態 ===
    private TargetObject currentInteractable; // 現在、操作可能な範囲内にいるTargetObject

    // ---------------------------------------------------
    // 検出されたTargetObjectの追跡
    // ---------------------------------------------------

    /// <summary>
    /// 操作可能なオブジェクトが範囲内に入ったときに TargetObject から呼ばれる。
    /// </summary>
    public void SetInteractable(TargetObject interactable)
    {
        currentInteractable = interactable;
    }

    /// <summary>
    /// 操作可能なオブジェクトが範囲外に出たときに TargetObject から呼ばれる。
    /// </summary>
    public void ClearInteractable(TargetObject interactable)
    {
        // 範囲外に出たオブジェクトが、現在追跡中のオブジェクトと一致する場合のみクリア
        if (currentInteractable == interactable)
        {
            currentInteractable = null;
        }
    }

    // ---------------------------------------------------
    // 入力処理
    // ---------------------------------------------------

    void Update()
    {
        // Fキーが押され、かつ操作可能なオブジェクトが近くにある場合
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            // 操作可能なオブジェクトを作動させる
            currentInteractable.PlayerInteractionCompleted();

            // 操作が完了したら、現在の操作対象をクリアする (連続操作を防ぐ)
            currentInteractable = null;
        }
    }
}