using UnityEngine;

// クラス名: BalanceAnimation
public class BalanceAnimation : MonoBehaviour
{
    // 必要なコンポーネント
    private Animator animator;
    private CharacterController controller;
    private Transform mainCameraTransform; // カメラのTransformをキャッシュ

    // 移動速度設定 (必要に応じてInspectorで調整)
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float jumpForce = 8.0f; // ★追加: ジャンプ力★
    public float gravity = -20.0f;
    public float riseForce = 10.0f; // 滞空中の上昇力
    public float rotationSpeed = 10.0f;

    // 着地判定の距離
    [Header("Landing Check")]
    public float landingDistance = 0.1f; // 地面から何メートルでLandingアニメーションを開始するか
    private bool landingTriggered = false; // Landingトリガーが既に発動されたかを示すフラグ

    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded = true;

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

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null || controller == null)
        {
            Debug.LogError("Required component missing (Animator or CharacterController). Disabling script.");
            enabled = false;
            return;
        }

        // カメラのTransformをStartで一度だけキャッシュする
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("FATAL: Camera tagged 'MainCamera' not found. Character movement rotation will be fixed to World Z.");
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

    /// <summary>地面接触と空中状態のパラメーターを制御します。（ジャンプ/上昇:Space, 下降:Alt, 着地距離判定）</summary>
    private void HandleGroundAndAirState()
    {
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        // --- 入力判定 ---
        bool isSpaceKeyDown = Input.GetKeyDown(KeyCode.Space); // ジャンプまたは上昇開始
        bool isRisingInput = Input.GetKey(KeyCode.Space);      // 上昇継続
        bool isFallingInput = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // --- 1. 地面接触から地上状態への復帰 ---
        if (isGrounded)
        {
            if (!wasGrounded)
            {
                // 地面についた瞬間にLandアニメーションを一度だけ実行
                animator.SetTrigger(LandTriggerHash);
                Debug.Log("Ground State: LANDING TRIGGERED.");
            }

            // 地面にいる間の状態リセット
            moveDirection.y = -0.5f; // 地面に押し付けるための微小な負の速度
            landingTriggered = false; // 地上なのでリセット

            animator.SetBool(IsGroundedHash, true);
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false); // 地上では上昇状態をリセット

            // --- 1a. 地上でのジャンプ処理 ---
            if (isSpaceKeyDown)
            {
                moveDirection.y = jumpForce; // ジャンプ力を適用
                animator.SetBool(IsRisingHash, true);
                animator.SetBool(IsGroundedHash, false);
                Debug.Log("Ground State: JUMPING.");
            }
        }
        // --- 2. 空中状態（isGrounded == false） ---
        else
        {
            animator.SetBool(IsGroundedHash, false); // 強制的に空中状態

            // --- 2a. 滞空中の上昇継続処理 (Spaceキー長押し) ---
            if (isRisingInput && moveDirection.y > 0) // 上昇モーション中にSpaceが押されている場合
            {
                moveDirection.y = riseForce; // 上昇力を維持
                animator.SetBool(IsRisingHash, true);
                animator.SetBool(IsFallingHash, false);
                landingTriggered = false;
                Debug.Log("Air State: RISING (Continuation).");
            }
            // --- 2b. 急降下入力 (Altキー) ---
            else if (isFallingInput)
            {
                moveDirection.y = -riseForce * 2.0f; // 急降下
                animator.SetBool(IsFallingHash, true);
                animator.SetBool(IsRisingHash, false);
                Debug.Log("Air State: FAST FALLING.");
            }
            // --- 2c. 通常の落下状態 (重力適用) ---
            else
            {
                // 重力適用
                moveDirection.y += gravity * Time.deltaTime;

                if (moveDirection.y < -0.1f)
                {
                    // 落下中
                    animator.SetBool(IsFallingHash, true);
                    animator.SetBool(IsRisingHash, false);
                }
                else if (moveDirection.y > 0.1f)
                {
                    // 重力で減速中の上昇/微上昇状態
                    animator.SetBool(IsFallingHash, false);
                    animator.SetBool(IsRisingHash, true); // アニメーターのRising状態を継続
                }
                else
                {
                    // 空中静止または微量な移動
                    animator.SetBool(IsFallingHash, false);
                    animator.SetBool(IsRisingHash, false);
                }

                // 着地アニメーションの先行判定 (Raycastを利用)
                HandleLandingCheck();
                // Debug.Log($"Air State: Falling/Floating (Y={moveDirection.y:F2}).");
            }
        }
    }

    /// <summary>着地距離を判定し、Landingアニメーショントリガーを起動します。（レイキャスト先行判定）</summary>
    private void HandleLandingCheck()
    {
        // 既にLandingトリガーが発動されている、まだ上昇中、または isGrounded が既に true の場合はチェックしない
        if (landingTriggered || moveDirection.y > 0 || controller.isGrounded) return;

        RaycastHit hit;

        float originOffset = controller.skinWidth;
        Vector3 rayOrigin = transform.position + Vector3.up * originOffset;

        float rayLength = landingDistance + originOffset * 2f;

        // レイキャストのデバッグ表示 
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength))
        {
            float distanceToGround = hit.distance - originOffset;

            if (distanceToGround <= landingDistance)
            {
                if (!landingTriggered)
                {
                    // 地面につく少し前にLandTriggerを発動
                    animator.SetTrigger(LandTriggerHash);
                    landingTriggered = true;
                    Debug.Log("Raycast: LANDING PRE-TRIGGERED.");
                }
            }
        }
    }


    /// <summary>WASDとShiftキーによる移動とアニメーションを制御します。（カメラ追従方式）</summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        bool isMoving = inputDirection.magnitude > 0.1f;
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving && controller.isGrounded; // 地上にいる時のみダッシュ

        float currentSpeed = isDashing ? dashSpeed : walkSpeed;

        // アニメーションパラメーターの設定
        animator.SetBool(IsWalkingHash, isMoving && !isDashing && controller.isGrounded);
        animator.SetBool(IsDashingHash, isDashing && controller.isGrounded);


        if (isMoving)
        {
            if (mainCameraTransform != null)
            {
                // 1. カメラのY軸回転のみを考慮した前方・右方ベクトルを計算
                Vector3 cameraForward = Vector3.ProjectOnPlane(mainCameraTransform.forward, Vector3.up).normalized;
                Vector3 cameraRight = Vector3.ProjectOnPlane(mainCameraTransform.right, Vector3.up).normalized;

                // 2. 入力とカメラの方向を合成して、ワールド座標系での目的の移動方向を決定
                Vector3 desiredMoveDirection = (cameraForward * vertical) + (cameraRight * horizontal);
                desiredMoveDirection.Normalize();

                // 3. キャラクターの向きを desiredMoveDirection に向かって回転
                Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                // 4. 最終的な水平移動速度は、キャラクターの現在の前方方向と速度に基づいて計算
                moveDirection.x = transform.forward.x * currentSpeed;
                moveDirection.z = transform.forward.z * currentSpeed;
            }
            else
            {
                // カメラがない場合 (ワールドZ軸を前方とする)
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                Vector3 forwardMovement = transform.forward * currentSpeed * inputDirection.magnitude;
                moveDirection.x = forwardMovement.x;
                moveDirection.z = forwardMovement.z;
            }
        }
        else
        {
            // 入力がない場合は水平方向の移動を停止
            if (controller.isGrounded)
            {
                moveDirection.x = 0;
                moveDirection.z = 0;
            }

            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
        }

        // 最終的な移動の適用
        controller.Move(moveDirection * Time.deltaTime);

        // プレイヤーの動きの方向を緑色のレイで描画
        Debug.DrawRay(transform.position, moveDirection.normalized * 2f, Color.green);
        // デバッグログ: moveDirectionの中身を確認できます。
        // Debug.Log($"Move Dir: X={moveDirection.x:F2}, Y={moveDirection.y:F2}, Z={moveDirection.z:F2}");
    }

    /// <summary>左クリックによる攻撃アニメーションを制御します。（EキーでAttack1/Attack2を切り替え）</summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!controller.isGrounded)
            {
                return; // 空中攻撃を許可しない
            }

            int attackHash = isWeaponModeTwo ? Attack2TriggerHash : Attack1TriggerHash;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName("Attack1") && !stateInfo.IsName("Attack2"))
            {
                animator.SetTrigger(attackHash);
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
            Debug.Log($"Weapon Mode Switched: Weapon Mode 2 = {isWeaponModeTwo}");
        }

        // 右クリック: ロックオン
        if (Input.GetMouseButtonDown(1))
        {
            // ロックオン処理
            Debug.Log("Lock On Attempted.");
        }

        // Esc: 設定画面を開ける
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 設定メニュー表示処理
            Debug.Log("Escape Key Pressed (Settings/Menu).");
        }
    }
}