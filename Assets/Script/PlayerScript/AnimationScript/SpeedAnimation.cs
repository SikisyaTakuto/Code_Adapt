using UnityEngine;

/// <summary>
/// キャラクターのアニメーションとアクションを、入力とAnimatorパラメーターを通じて制御するスクリプト。
/// 物理移動（CharacterController）のロジックは全て削除されていますが、Raycastによる地面接触判定が追加されています。
/// </summary>
public class SpeedAnimation : MonoBehaviour
{
    // 必要なコンポーネント
    private Animator animator;

    // --- 地面判定のための追加フィールド ---
    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.3f; // 地面判定のためのレイの長さ
    public LayerMask groundLayer;            // 地面として判定するレイヤー
    private bool isGrounded = true;          // 現在の接地状態
    private bool landingTriggered = false;   // LandTriggerが発動されたか
    // ------------------------------------

    // 武装モードの管理フラグ
    private bool isWeaponModeTwo = false;

    // パラメーターハッシュ 
    private readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private readonly int Attack1TriggerHash = Animator.StringToHash("Attack1Trigger");
    private readonly int Attack2TriggerHash = Animator.StringToHash("Attack2Trigger");
    private readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private readonly int IsRisingHash = Animator.StringToHash("IsRising");
    private readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int LandTriggerHash = Animator.StringToHash("LandTrigger");
    private readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");

    void Start()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Required component 'Animator' missing. Disabling script.");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 地面判定を最初に実行
        CheckIsGrounded();

        HandleGroundAndAirState();
        HandleMovement();
        HandleAttackInput();
        HandleOtherInput();
    }

    /// <summary>Raycastを使用して地面をチェックし、isGroundedとアニメーションパラメーターを更新します。</summary>
    private void CheckIsGrounded()
    {
        bool wasGrounded = isGrounded;

        // キャラクターの底面から下向きにレイキャスト
        RaycastHit hit;
        // transform.position は通常キャラクターの中心。地面判定は足元から行うため、適宜調整が必要
        // Rigidbodyを使用していないため、単純に現在の位置からレイを飛ばす
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        // デバッグ表示
        Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        // Raycastによる地面判定
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


    /// <summary>空中状態のアニメーションパラメーターを制御します。（Space:上昇, Alt:下降, JumpTrigger）</summary>
    private void HandleGroundAndAirState()
    {
        bool isRisingInputDown = Input.GetKeyDown(KeyCode.Space);
        bool isRisingInputHeld = Input.GetKey(KeyCode.Space);
        bool isFallingInput = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // IsGroundedHashはCheckIsGrounded()で更新されているため、ここでは入力による空中状態のアニメーションを制御

        // --- 1. 上昇/ジャンプ入力 ---
        if (isRisingInputHeld)
        {
            animator.SetBool(IsRisingHash, true);
            animator.SetBool(IsFallingHash, false);

            if (isRisingInputDown && isGrounded) // 地上にいる状態で押し始めたらJumpをトリガー
            {
                animator.SetTrigger(JumpTriggerHash);
                Debug.Log("Jump/Rise Input (JumpTrigger).");
            }
        }
        // --- 2. 急降下入力 ---
        else if (isFallingInput)
        {
            animator.SetBool(IsFallingHash, true);
            animator.SetBool(IsRisingHash, false);
        }
        // --- 3. 入力がない場合 ---
        else
        {
            // 上昇キーが離されたらRisingをリセット
            animator.SetBool(IsRisingHash, false);

            // 地面にいない、かつRisingアニメーション中でなければ、Fallingアニメーションを有効化する判断
            if (!isGrounded && !animator.GetBool(IsRisingHash))
            {
                // NOTE: 外部で重力による落下速度が制御されている場合、このFallingフラグは不要かもしれません。
                // 今回はシンプルに「空中にいて上昇していない」＝「落下中」と見なします。
                animator.SetBool(IsFallingHash, true);
            }
        }
    }

    /// <summary>WASDとShiftキーによる移動のアニメーションを制御します。</summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        bool isMoving = inputDirection.magnitude > 0.1f;
        // isGrounded判定を削除し、入力があればダッシュとみなす
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        // アニメーションパラメーターを更新
        animator.SetBool(IsWalkingHash, isMoving && !isDashing);
        animator.SetBool(IsDashingHash, isDashing);

        if (!isMoving)
        {
            // 入力がない場合はWalkとDashをFalseにする
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
        }
        // NOTE: 物理移動と回転ロジックは意図的に削除されています。
    }

    /// <summary>左クリックによる攻撃アニメーションを制御します。（EキーでAttack1/Attack2を切り替え）</summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 地上でのみ攻撃を許可する判定を復活 (RaycastによるisGroundedを使用)
            if (!isGrounded)
            {
                Debug.Log("Attempted Aerial Attack. (No aerial attack implemented)");
                return;
            }

            int attackHash = isWeaponModeTwo ? Attack2TriggerHash : Attack1TriggerHash;
            string attackName = isWeaponModeTwo ? "Attack2" : "Attack1";

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 攻撃中は次の攻撃トリガーを受け付けないようにチェック
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