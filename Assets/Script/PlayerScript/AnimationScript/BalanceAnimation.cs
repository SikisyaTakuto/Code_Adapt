using UnityEngine;

public class BalanceAnimation : MonoBehaviour
{
    private Animator animator;
    public PlayerStatus playerStatus; // 追加：PlayerStatusを参照

    [Header("Ground Check Settings")]
    [Tooltip("判定に使用する球体の半径。足元の幅に合わせます")]
    public float groundCheckRadius = 0.25f;
    [Tooltip("足元からどれくらい下まで地面を探すか")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("地面を離れてから空中とみなすまでの猶予時間（コヨーテタイム）")]
    public float groundedTimeout = 0.1f;

    private bool isGrounded = true;
    private float groundedTimer = 0f; // 猶予計算用

    [Header("Idle Settings")]
    [Tooltip("何秒入力がないとIdle2に移行するか")]
    public float idle2Threshold = 10.0f;
    private float idleTimer = 0f;

    [Header("Idle Objects")]
    [Tooltip("Idle2の間だけ表示させたいモニターなどのオブジェクト")]
    public GameObject idleMonitorObject;

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
    private readonly int Idle2Hash = Animator.StringToHash("Idle2");
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
            Debug.LogWarning($"[{gameObject.name}] PlayerStatusが見つかりません。Inspectorでセットしてください。");
        }

        // 初期状態ではモニターを非表示にする
        if (idleMonitorObject != null) idleMonitorObject.SetActive(false);
    }

    void Update()
    {
        // ★死亡チェック
        if (playerStatus != null && playerStatus.IsDead)
        {
            if (!dieTriggerSent) Debug.Log($"<color=red>[{gameObject.name}] Death detected!</color>");
            HandleDeathAnimation();
            return;
        }

        CheckIsGrounded();
        HandleMovementAnimationInput();
        HandleAirAnimationInput();
        HandleIdleTimer();
    }

    private void HandleDeathAnimation()
    {
        if (!dieTriggerSent)
        {
            Debug.Log("<color=yellow>Setting Die Trigger now!</color>");
            animator.SetTrigger(DieTriggerHash);

            // 死亡時は全ての状態をリセット
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsDashingHash, false);
            SetIdleMode(false);

            dieTriggerSent = true;
        }
    }

    private void HandleIdleTimer()
    {
        // 入力の有無をチェック
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 ||
                         Input.GetAxisRaw("Vertical") != 0 ||
                         Input.GetKey(KeyCode.Space) ||
                         Input.GetMouseButton(0));

        if (hasInput || !isGrounded)
        {
            idleTimer = 0f;
            SetIdleMode(false);
        }
        else
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idle2Threshold)
            {
                SetIdleMode(true);
            }
        }
    }

    // ★追加：アニメーションとモニター表示を同期させる
    private void SetIdleMode(bool active)
    {
        animator.SetBool(Idle2Hash, active);

        if (idleMonitorObject != null)
        {
            // 現在の表示状態と異なる場合のみ実行して負荷を抑える
            if (idleMonitorObject.activeSelf != active)
            {
                idleMonitorObject.SetActive(active);
            }
        }
    }

    public void PlayAttackAnimation(bool isModeTwo)
    {
        if (animator == null) return;

        // 攻撃時に放置状態を解除
        idleTimer = 0f;
        SetIdleMode(false);

        int attackHash = isModeTwo ? Attack2TriggerHash : Attack1TriggerHash;
        animator.SetTrigger(attackHash);
    }

    private void CheckIsGrounded()
    {
        bool wasGrounded = isGrounded;

        // 球体判定の開始地点
        Vector3 sphereOrigin = transform.position + Vector3.up * groundCheckRadius;

        // SphereCastを実行
        bool hit = Physics.SphereCast(sphereOrigin, groundCheckRadius, Vector3.down, out RaycastHit hitInfo, groundCheckDistance, groundLayer);

        if (hit)
        {
            isGrounded = true;
            groundedTimer = groundedTimeout; // 接地中は常にタイマーをリセット
        }
        else
        {
            groundedTimer -= Time.deltaTime;
            if (groundedTimer <= 0)
            {
                isGrounded = false;
            }
        }

        animator.SetBool(IsGroundedHash, isGrounded);

        // 着地した瞬間のみトリガーを実行
        if (!wasGrounded && isGrounded)
        {
            animator.SetTrigger(LandTriggerHash);
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false);
        }
    }

    // エディタ上で判定範囲を表示（デバッグ用）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 sphereOrigin = transform.position + Vector3.up * groundCheckRadius;
        Gizmos.DrawWireSphere(sphereOrigin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }

    private void HandleMovementAnimationInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = (new Vector3(h, 0, v)).magnitude > 0.1f;
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        animator.SetBool(IsWalkingHash, isMoving && !isDashing);
        animator.SetBool(IsDashingHash, isDashing);
    }

    private void HandleAirAnimationInput()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded) animator.SetTrigger(JumpTriggerHash);
            animator.SetBool(IsRisingHash, true);
            animator.SetBool(IsFallingHash, false);
        }
        else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            animator.SetBool(IsFallingHash, true);
            animator.SetBool(IsRisingHash, false);
        }
        else
        {
            animator.SetBool(IsRisingHash, false);
            if (!isGrounded) animator.SetBool(IsFallingHash, true);
            else { animator.SetBool(IsFallingHash, false); animator.SetBool(IsRisingHash, false); }
        }
    }
}