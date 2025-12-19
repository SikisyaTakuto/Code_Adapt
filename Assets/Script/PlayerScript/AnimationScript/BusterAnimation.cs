using UnityEngine;

// クラス名: BusterAnimation (CharacterController関連を全て削除し、Raycastによる地面判定を追加)
public class BusterAnimation : MonoBehaviour
{
    // 必要なコンポーネント
    private Animator animator;
    // CharacterController controller; // 削除

    // --- 地面判定のための追加フィールド ---
    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.3f; // 地面判定のためのレイの長さ
    public LayerMask groundLayer;            // 地面として判定するレイヤー (Inspectorで設定が必要)
    private bool isGrounded = true;          // 現在の接地状態
    private bool landingTriggered = false;   // LandTriggerが発動されたか
    // ------------------------------------

    // 武装モードの管理フラグ
    private bool isWeaponModeTwo = false;

    // パラメーターハッシュ (Animator Controllerに設定が必要)
    private readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private readonly int Attack1TriggerHash = Animator.StringToHash("Attack1Trigger");
    private readonly int Attack2TriggerHash = Animator.StringToHash("Attack2Trigger");
    private readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private readonly int IsRisingHash = Animator.StringToHash("IsRising");
    private readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int LandTriggerHash = Animator.StringToHash("LandTrigger");
    // NOTE: JumpTriggerHashは元のスクリプトにはなかったが、Jump入力処理のためにここでは追加しない。

    void Start()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Required component 'Animator' missing. Disabling script.");
            enabled = false;
            return;
        }

        // 移動/物理関連の変数は全て削除
    }

    void Update()
    {
        // 地面判定を最初に実行し、isGroundedフラグとAnimatorを更新
        CheckIsGrounded();

        // 物理/移動処理を削除し、入力とアニメーション制御のみに特化
        HandleMovementAnimationInput();
        HandleAirAnimationInput();
        HandleAttackInput();
        HandleOtherInput();
    }

    // ----------------------------------------------------
    // 新しい地面判定ロジック
    // ----------------------------------------------------

    /// <summary>Raycastを使用して地面をチェックし、isGroundedとアニメーションパラメーターを更新します。</summary>
    private void CheckIsGrounded()
    {
        bool wasGrounded = isGrounded;

        // キャラクターの底面付近から下向きにレイキャスト
        RaycastHit hit;
        // 判定開始位置をトランスフォームの中心よりわずかに上 (0.1f) に設定
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        // デバッグ表示 
        Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        // Raycastによる地面判定
        // groundLayerが設定されていない場合（デフォルトは0）は、すべてと衝突する
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        // --- 地面接触/離脱のアニメーション制御 ---

        // 地面から離れた瞬間
        if (wasGrounded && !isGrounded)
        {
            Debug.Log("Leaving Ground.");
            animator.SetBool(IsGroundedHash, false);
            landingTriggered = false; // 空中に出たのでリセット
        }

        // 地面に着いた瞬間
        if (!wasGrounded && isGrounded)
        {
            Debug.Log("Landing Detected (Raycast).");
            animator.SetTrigger(LandTriggerHash);
            animator.SetBool(IsGroundedHash, true);
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false);
            landingTriggered = true; // 着地トリガーを発動
        }
    }


    /// <summary>WASDとShiftキーによる移動入力に基づいて、Walk/Dashアニメーションを制御します。</summary>
    private void HandleMovementAnimationInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        bool isMoving = inputDirection.magnitude > 0.1f;
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        // アニメーションパラメーターの設定
        animator.SetBool(IsWalkingHash, isMoving && !isDashing);
        animator.SetBool(IsDashingHash, isDashing);

        // NOTE: 物理移動ロジックと回転ロジックは削除されています。

        if (!isMoving)
        {
            // 入力がない場合はWalkとDashをFalseにする
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
        }
    }

    /// <summary>Space/Altキーの入力に基づいて、Rising/Fallingアニメーションを制御します。</summary>
    private void HandleAirAnimationInput()
    {
        // Spaceキー: 上昇/ジャンプ入力
        if (Input.GetKey(KeyCode.Space))
        {
            animator.SetBool(IsRisingHash, true);
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsGroundedHash, false); // 上昇中は強制的に空中状態
        }
        // Altキー: 急降下入力
        else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            animator.SetBool(IsFallingHash, true);
            animator.SetBool(IsRisingHash, false);
        }
        // 入力がない場合
        else
        {
            // 上昇キーが離されたらRisingをリセット
            animator.SetBool(IsRisingHash, false);

            // 地面にいない、かつRisingアニメーション中でなければ、Fallingアニメーションを有効化する判断
            if (!isGrounded && !animator.GetBool(IsRisingHash))
            {
                // Fallingアニメーションを制御（外部の物理エンジンに依存しない場合）
                animator.SetBool(IsFallingHash, true);
            }
        }
    }

    /// <summary>左クリックによる攻撃アニメーションを制御します。（EキーでAttack1/Attack2を切り替え）</summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 地上でのみ攻撃を許可する判定 (RaycastによるisGroundedを使用)
            if (!isGrounded)
            {
                Debug.Log("Attempted Aerial Attack. (No aerial attack implemented)");
                return;
            }

            int attackHash = isWeaponModeTwo ? Attack2TriggerHash : Attack1TriggerHash;
            string attackName = isWeaponModeTwo ? "Attack2" : "Attack1";

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 攻撃アニメーション中でなければトリガー発動
            if (!stateInfo.IsName("Attack1") && !stateInfo.IsName("Attack2"))
            {
                animator.SetTrigger(attackHash);
                Debug.Log($"New Attack: Triggering {attackName} (Mode {(isWeaponModeTwo ? 2 : 1)}).");
            }
        }
    }

    /// <summary>その他の特殊な入力（E:武装切替、右クリック:ロックオン、Esc:設定）を処理します。</summary>
    private void HandleOtherInput()
    {
        // E: メインとサブの武装切替
        if (Input.GetKeyDown(KeyCode.E))
        {
            isWeaponModeTwo = !isWeaponModeTwo;
            string modeName = isWeaponModeTwo ? "Sub Weapon (Attack2)" : "Main Weapon (Attack1)";
            Debug.Log($"Weapon Switch: Switched to {modeName}.");
        }

        // 右クリック: ロックオン
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Lock-On activated.");
        }

        // Esc: 設定画面を開ける
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Opening Settings Menu.");
        }
    }
}