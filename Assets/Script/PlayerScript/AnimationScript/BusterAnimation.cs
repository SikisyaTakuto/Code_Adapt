using UnityEngine;

// クラス名: BusterAnimation (CharacterController関連を全て削除し、Raycastによる地面判定を追加)
public class BusterAnimation : MonoBehaviour
{
    // 必要なコンポーネント
    private Animator animator;
    public PlayerStatus playerStatus; // 追加：PlayerStatusを参照

    [Header("Ground Check Settings")]
    [Tooltip("判定に使用する球体の半径。足元の幅に合わせます")]
    public float groundCheckRadius = 0.25f;
    [Tooltip("足元からどれくらい下まで地面を探すか")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("地面を離れてから空中とみなすまでの猶予時間。ガタつき防止用")]
    public float groundedTimeout = 0.1f;

    private bool isGrounded = true;
    private float groundedTimer = 0f; // 猶予計算用タイマー
    private bool landingTriggered = false;

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
    private readonly int DieTriggerHash = Animator.StringToHash("Die"); // ★追加

    private bool dieTriggerSent = false; // 二重にトリガーを送らないためのフラグ

    void Start()
    {
        animator = GetComponent<Animator>();

        // --- 参照の自動取得を強化 ---
        if (playerStatus == null)
        {
            playerStatus = GetComponentInParent<PlayerStatus>();
            if (playerStatus == null) playerStatus = GetComponentInChildren<PlayerStatus>();
        }

        if (animator == null)
        {
            Debug.LogError($"[{gameObject.name}] Animatorが見つかりません。");
            enabled = false;
            return;
        }

        if (playerStatus == null)
        {
            Debug.LogError($"[{gameObject.name}] PlayerStatusが見つかりません！Inspectorでセットしてください。");
        }
    }

    void Update()
    {
        // ★死亡チェック
        if (playerStatus != null && playerStatus.IsDead)
        {
            // ログが出るか確認
            if (!dieTriggerSent) Debug.Log($"<color=red>[{gameObject.name}] Death detected in Animation Script!</color>");
            HandleDeathAnimation();
            return;
        }

        CheckIsGrounded();
        HandleMovementAnimationInput();
        HandleAirAnimationInput();
        HandleAttackInput();
        HandleOtherInput();
    }

    private void HandleDeathAnimation()
    {
        if (!dieTriggerSent)
        {
            Debug.Log("<color=yellow>Setting Die Trigger now!</color>");
            animator.SetTrigger(DieTriggerHash);
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
            dieTriggerSent = true;
        }
    }

    public void PlayAttackAnimation(bool isModeTwo)
    {
        int attackHash = isModeTwo ? Attack2TriggerHash : Attack1TriggerHash;
        animator.SetTrigger(attackHash);
    }

    // ----------------------------------------------------
    // 新しい地面判定ロジック
    // ----------------------------------------------------

    /// <summary>Raycastを使用して地面をチェックし、isGroundedとアニメーションパラメーターを更新します。</summary>
    private void CheckIsGrounded()
    {
        bool wasGrounded = isGrounded;

        // キャラクターの足元から球体判定を飛ばす
        // 開始位置はキャラの足元(transform.position)より少し上に設定
        Vector3 sphereOrigin = transform.position + Vector3.up * groundCheckRadius;

        // SphereCastによる判定
        bool hit = Physics.SphereCast(sphereOrigin, groundCheckRadius, Vector3.down, out RaycastHit hitInfo, groundCheckDistance, groundLayer);

        if (hit)
        {
            isGrounded = true;
            groundedTimer = groundedTimeout; // 地面にいればタイマーを常にリセット
        }
        else
        {
            // 地面から外れた場合、猶予時間を減らす
            groundedTimer -= Time.deltaTime;
            if (groundedTimer <= 0)
            {
                isGrounded = false;
            }
        }

        // --- アニメーション制御 ---

        // 接地状態を更新
        animator.SetBool(IsGroundedHash, isGrounded);

        // 地面から離れた瞬間
        if (wasGrounded && !isGrounded)
        {
            landingTriggered = false;
        }

        // 地面に着いた瞬間
        if (!wasGrounded && isGrounded)
        {
            animator.SetTrigger(LandTriggerHash);
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false);
            landingTriggered = true;
        }
    }

    // 判定範囲をSceneビューで見えるようにする（デバッグ用）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 sphereOrigin = transform.position + Vector3.up * groundCheckRadius;
        Gizmos.DrawWireSphere(sphereOrigin + Vector3.down * groundCheckDistance, groundCheckRadius);
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
            //// 地上でのみ攻撃を許可する判定 (RaycastによるisGroundedを使用)
            //if (!isGrounded)
            //{
            //    Debug.Log("Attempted Aerial Attack. (No aerial attack implemented)");
            //    return;
            //}

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