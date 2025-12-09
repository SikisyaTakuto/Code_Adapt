using UnityEngine;

public class BusterAnimation : MonoBehaviour
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

    // 【★新規追加★】着地判定の距離
    [Header("Landing Check")]
    public float landingDistance = 0.1f; // 地面から何メートルでLandingアニメーションを開始するか
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

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null || controller == null)
        {
            Debug.LogError("Required component missing. Disabling script.");
            enabled = false;
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
        animator.SetBool(IsGroundedHash, isGrounded);

        // --- 1. 上昇 (Spaceキー) の処理（優先） ---
        bool isRisingInput = Input.GetKey(KeyCode.Space);
        bool isFallingInput = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (isRisingInput)
        {
            moveDirection.y = riseForce;
            animator.SetBool(IsRisingHash, true);
            animator.SetBool(IsFallingHash, false);
            landingTriggered = false; // 上昇中は着地トリガーをリセット
            return;
        }

        // --- 2. 地面にいる場合の処理 ---
        if (isGrounded)
        {
            moveDirection.y = -0.5f;
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false);
            landingTriggered = false; // 地面にいるときはリセット
        }
        // --- 3. 空中にいる場合の処理 (重力、下降、着地判定) ---
        else
        {
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
                animator.SetBool(IsRisingHash, false);
            }
            // 空中静止状態
            else if (moveDirection.y > -0.1f && moveDirection.y < 0.1f)
            {
                animator.SetBool(IsFallingHash, false);
                animator.SetBool(IsRisingHash, false);
            }

            // 【★修正箇所★】レイキャストによる着地距離判定
            HandleLandingCheck();
        }
    }

    /// <summary>着地距離を判定し、Landingアニメーショントリガーを起動します。</summary>
    private void HandleLandingCheck()
    {
        // 既にLandingトリガーが発動されている、またはまだ下降していない場合はチェックしない
        if (landingTriggered || moveDirection.y > 0) return;

        RaycastHit hit;
        // キャラクターの中心から下向きにレイを飛ばす
        // controller.height / 2f はCharacterControllerの中心までの距離
        // controller.radius はカプセルの半径
        float rayStartHeight = controller.height / 2f;

        // レイの原点をキャラクターの足元（正確には中心より少し上）に設定
        Vector3 rayOrigin = transform.position + Vector3.up * rayStartHeight;

        // レイの長さは landingDistance + わずかな余裕
        float rayLength = landingDistance + 0.1f;

        // デバッグ表示用のレイ
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength))
        {
            // レイキャストがヒットした場合、ヒットした点までの距離を算出
            // 距離 (hit.distance) は Raycastの始点(rayOrigin)からヒット点まで。
            // 地面からの実質的な距離を求めるために、CharacterControllerの足元からの距離に変換する調整が必要だが、
            // 簡単のため、hit.distance が landingDistance 内に入ったかを判定する。

            // より厳密には、CharacterControllerの足元からの距離を計算する必要があります。
            // (hit.point.y - transform.position.y)

            // キャラクターコントローラの足元から地面までの距離が landingDistance 以下になったら
            if (hit.distance - rayStartHeight <= landingDistance)
            {
                if (!landingTriggered)
                {
                    animator.SetTrigger(LandTriggerHash);
                    landingTriggered = true; // トリガーが発動されたことを記録
                    Debug.Log($"Landing Triggered! Distance: {hit.distance - rayStartHeight:F3}m");
                }
            }
        }
    }


    /// <summary>WASDとShiftキーによる移動とアニメーションを制御します。</summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        bool isMoving = inputDirection.magnitude > 0.1f;
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        float currentSpeed = isDashing ? dashSpeed : walkSpeed;

        animator.SetBool(IsWalkingHash, isMoving && !isDashing);
        animator.SetBool(IsDashingHash, isDashing);

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            Vector3 forwardDirection = targetRotation * Vector3.forward;
            moveDirection.x = forwardDirection.x * currentSpeed;
            moveDirection.z = forwardDirection.z * currentSpeed;
        }
        else
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
        }

        controller.Move(moveDirection * Time.deltaTime);

        if (!isMoving)
        {
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
        }
    }

    /// <summary>左クリックによる攻撃アニメーションを制御します。（EキーでAttack1/Attack2を切り替え）</summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!isGrounded)
            {
                Debug.Log("Attempted Aerial Attack. (No aerial attack implemented)");
                return;
            }

            int attackHash = isWeaponModeTwo ? Attack2TriggerHash : Attack1TriggerHash;
            string attackName = isWeaponModeTwo ? "Attack2" : "Attack1";

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

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