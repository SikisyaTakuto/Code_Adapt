using UnityEngine;

/// <summary>
/// キャラクターの移動、空中制御、着地判定（Raycastによる先行判定）、単発攻撃アニメーションを制御するスクリプト。
/// </summary>
public class SpeedAnimation : MonoBehaviour
{
    // 必要なコンポーネント
    private Animator animator;
    private CharacterController controller;

    // 移動速度設定 (必要に応じてInspectorで調整)
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float gravity = -20.0f;
    public float riseForce = 10.0f;
    public float rotationSpeed = 10.0f;

    // 着地判定の距離
    [Header("Landing Check")]
    // ★修正点 1: デフォルト値を0.3f (30cm) に設定し、数十センチでの着地アニメーションを想定★
    public float landingDistance = 0.3f;
    private bool landingTriggered = false; // Landingトリガーが既に発動されたかを示すフラグ

    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded = true;

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

    // Jumpアニメーション用のトリガーハッシュ
    private readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null || controller == null)
        {
            Debug.LogError("Required component missing. Disabling script.");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleGroundAndAirState();
        HandleMovement();
        HandleAttackInput();
        HandleOtherInput();
    }

    /// <summary>地面接触と空中状態のパラメーターを制御します。（上昇:Space, 下降:Alt, 着地距離判定）</summary>
    private void HandleGroundAndAirState()
    {
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        // Spaceキー入力の分離: 押した瞬間と押し続けている状態
        bool isRisingInputDown = Input.GetKeyDown(KeyCode.Space);
        bool isRisingInputHeld = Input.GetKey(KeyCode.Space);
        bool isFallingInput = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // --- 1. 上昇 (Spaceキー) の処理（優先） ---
        if (isRisingInputHeld)
        {
            moveDirection.y = riseForce;

            if (isRisingInputDown && isGrounded)
            {
                // 地面でSpaceを押し始めた瞬間: jumpアニメーションをトリガー
                animator.SetTrigger(JumpTriggerHash);
                Debug.Log("Initial Jump Triggered (JumpTrigger).");
            }

            // 連続上昇/飛行状態フラグを設定 
            animator.SetBool(IsRisingHash, true);
            animator.SetBool(IsFallingHash, false);
            landingTriggered = false; // 上昇中は着地トリガーをリセット
            animator.SetBool(IsGroundedHash, false); // 上昇中は強制的に空中状態
            return;
        }

        // --- 2. 地面接触 (Fall -> Idle/Walk への復帰) ---
        if (isGrounded)
        {
            if (!wasGrounded)
            {
                // 地面に触れた瞬間にLandTriggerを起動 (Raycast判定の安全策)
                animator.SetTrigger(LandTriggerHash);
                Debug.Log("Grounded: Resetting Air State.");
            }

            // 地面にいる間の状態リセット
            moveDirection.y = -0.5f;
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false); // Spaceキーが押されていないので、IsRisingをリセット
            landingTriggered = false;
            animator.SetBool(IsGroundedHash, true); // 地面パラメーターをTrueに設定
        }
        // --- 3. 空中にいる場合の処理 (重力、下降、着地判定) ---
        else
        {
            // isGrounded == false の場合にのみ重力を適用
            moveDirection.y += gravity * Time.deltaTime;

            if (isFallingInput)
            {
                moveDirection.y = -riseForce * 2.0f;
                animator.SetBool(IsFallingHash, true);
                animator.SetBool(IsRisingHash, false);
            }
            // 通常の落下
            else if (moveDirection.y < -0.1f)
            {
                animator.SetBool(IsFallingHash, true);
                animator.SetBool(IsRisingHash, false); // IsRisingを確実にリセット
            }
            // 空中静止状態
            else if (moveDirection.y > -0.1f && moveDirection.y < 0.1f)
            {
                animator.SetBool(IsFallingHash, false);
                animator.SetBool(IsRisingHash, false); // IsRisingを確実にリセット
            }

            // 着地アニメーションの先行判定
            HandleLandingCheck();
            animator.SetBool(IsGroundedHash, false);
        }
    }

    /// <summary>着地距離を判定し、Landingアニメーショントリガーを起動します。</summary>
    private void HandleLandingCheck()
    {
        // 現在の上昇/落下状態を取得
        bool currentlyRising = animator.GetBool(IsRisingHash);
        bool currentlyFalling = animator.GetBool(IsFallingHash);

        // 以下の条件で処理を中断する
        // 1. 既にトリガー済み、2. 地面接触済み、3. 上昇キーが押されている（IsRisingがtrue）、4. moveDirection.y が正（上昇中）
        // 5. まだ落下アニメーションに入っていない
        if (landingTriggered || controller.isGrounded || currentlyRising || moveDirection.y > 0 || !currentlyFalling)
        {
            // 飛行中や上昇中はランディングトリガーをリセットし続けることで、バグを防ぐ
            if (currentlyRising || moveDirection.y > 0)
            {
                landingTriggered = false;
            }
            return;
        }

        // ★追加のチェック★: 速度が非常に遅い（例えば -0.5f/s より遅い）場合は、Raycastをスキップして誤発動を防ぐ
        if (moveDirection.y > -0.5f)
        {
            return;
        }


        RaycastHit hit;

        // CharacterControllerの足元からレイを飛ばすためのオフセット (Skin Widthを考慮)
        float originOffset = controller.skinWidth;
        // レイの発射位置をCharacterControllerの底面付近に設定
        Vector3 rayOrigin = transform.position + Vector3.up * originOffset;

        // レイの長さは、足元から landingDistance 分
        float rayLength = landingDistance + originOffset * 2f;

        // デバッグ表示用のレイ
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);

        // Physics.Raycastが地面を検出
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength))
        {
            // ヒットした距離から原点オフセットを引くことで、CharacterControllerの「真の底面」から地面までの距離を算出
            float distanceToGround = hit.distance - originOffset;

            if (distanceToGround <= landingDistance)
            {
                if (!landingTriggered)
                {
                    animator.SetTrigger(LandTriggerHash);
                    landingTriggered = true; // トリガーが発動されたことを記録
                    Debug.Log($"Landing Triggered! Distance: {distanceToGround:F3}m");
                }
            }
        }
    }


    /// <summary>WASDとShiftキーによる移動とアニメーションを制御します。（空中ダッシュ対応）</summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        bool isMoving = inputDirection.magnitude > 0.1f;
        // isGroundedに関係なく、入力があればIsDashingをtrueにする
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        float currentSpeed = isDashing ? dashSpeed : walkSpeed;

        // アニメーションパラメーターを更新
        animator.SetBool(IsWalkingHash, isMoving && !isDashing);
        animator.SetBool(IsDashingHash, isDashing);


        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            Vector3 forwardDirection = targetRotation * Vector3.forward;

            // 水平方向の速度を上書き
            moveDirection.x = forwardDirection.x * currentSpeed;
            moveDirection.z = forwardDirection.z * currentSpeed;
        }
        else
        {
            // 入力がない場合は水平方向の移動を停止
            moveDirection.x = 0;
            moveDirection.z = 0;

            // 入力がない場合はWalkとDashをFalseにする
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
        }

        controller.Move(moveDirection * Time.deltaTime);
    }

    /// <summary>左クリックによる攻撃アニメーションを制御します。（EキーでAttack1/Attack2を切り替え）</summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 攻撃は地上でのみ許可
            if (!controller.isGrounded)
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