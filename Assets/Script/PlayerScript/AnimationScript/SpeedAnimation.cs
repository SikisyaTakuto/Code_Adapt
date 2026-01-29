using UnityEngine;

public class SpeedAnimation : MonoBehaviour
{
    private Animator animator;
    public PlayerStatus playerStatus;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
    private bool isGrounded = true;

    [Header("Idle Settings")]
    [Tooltip("何秒入力がないとIdle2に移行するか")]
    public float idle2Threshold = 10.0f;
    private float idleTimer = 0f; // 何もしていない時間をカウント

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
    private readonly int Idle2Hash = Animator.StringToHash("Idle2"); // 追加
    private readonly int DieTriggerHash = Animator.StringToHash("Die"); // ★追加

    private bool dieTriggerSent = false; // 二重送信防止用

    void Start()
    {
        animator = GetComponent<Animator>();

        // もし自分自身になければ、親や子から探すように変更
        if (playerStatus == null)
        {
            playerStatus = GetComponentInParent<PlayerStatus>();
            if (playerStatus == null) playerStatus = GetComponentInChildren<PlayerStatus>();
        }

        if (animator == null) { Debug.LogError("Animatorが見つかりません"); enabled = false; return; }
        if (playerStatus == null) { Debug.LogWarning("PlayerStatusが紐付いていません。インスペクターでドラッグ&ドロップするか、同じオブジェクトに付けてください"); }
    }

    void Update()
    {
        // 死亡フラグを毎フレーム監視し、変化があったら即ログを出す
        if (playerStatus != null)
        {
            if (playerStatus.IsDead)
            {
                // ログが出るか確認
                Debug.Log($"<color=red>Death detected!</color> IsDead: {playerStatus.IsDead}, Animator: {animator.name}");
                HandleDeathAnimation();
                return;
            }
        }

        CheckIsGrounded();
        HandleGroundAndAirState();
        HandleMovement();
        HandleIdleTimer();
    }

    private void HandleDeathAnimation()
    {
        if (!dieTriggerSent)
        {
            Debug.Log("<color=yellow>Setting Die Trigger now!</color>");
            animator.SetTrigger(DieTriggerHash);
            animator.SetBool(Idle2Hash, false);
            dieTriggerSent = true;
        }
    }

    // ★追加：放置時間を計測して Idle2 を制御する
    private void HandleIdleTimer()
    {
        // 移動・上昇・攻撃などの入力があるかチェック
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 ||
                         Input.GetAxisRaw("Vertical") != 0 ||
                         Input.GetKey(KeyCode.Space) ||
                         Input.GetMouseButton(0));

        if (hasInput || !isGrounded)
        {
            // 何か操作している、または空中にいる時はタイマーリセット
            idleTimer = 0f;
            animator.SetBool(Idle2Hash, false);
        }
        else
        {
            // 地上で何もしていない時だけタイマーを進める
            idleTimer += Time.deltaTime;

            if (idleTimer >= idle2Threshold)
            {
                animator.SetBool(Idle2Hash, true);
            }
        }
    }

    public void PlayAttackAnimation(bool isModeTwo)
    {
        if (animator == null) return;
        // 攻撃した瞬間にIdle2を解除
        idleTimer = 0f;
        animator.SetBool(Idle2Hash, false);

        int attackHash = isModeTwo ? Attack2TriggerHash : Attack1TriggerHash;
        animator.SetTrigger(attackHash);
    }

    private void CheckIsGrounded()
    {
        bool wasGrounded = isGrounded;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundLayer);

        animator.SetBool(IsGroundedHash, isGrounded);

        if (wasGrounded && !isGrounded)
        {
            animator.SetBool(IsGroundedHash, false);
        }
        if (!wasGrounded && isGrounded)
        {
            animator.SetTrigger(LandTriggerHash);
            animator.SetBool(IsGroundedHash, true);
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false);
        }
    }

    private void HandleGroundAndAirState()
    {
        bool isRisingInputHeld = Input.GetKey(KeyCode.Space);
        bool isFallingInput = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (isRisingInputHeld)
        {
            animator.SetBool(IsRisingHash, true);
            animator.SetBool(IsFallingHash, false);
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                animator.SetTrigger(JumpTriggerHash);
            }
        }
        else if (isFallingInput)
        {
            animator.SetBool(IsFallingHash, true);
            animator.SetBool(IsRisingHash, false);
        }
        else
        {
            animator.SetBool(IsRisingHash, false);
            if (!isGrounded) animator.SetBool(IsFallingHash, true);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = (new Vector3(horizontal, 0, vertical)).magnitude > 0.1f;
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        animator.SetBool(IsWalkingHash, isMoving && !isDashing);
        animator.SetBool(IsDashingHash, isDashing);
    }
}