using UnityEngine;

public class BalanceAnimation : MonoBehaviour
{
    private Animator animator;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
    private bool isGrounded = true;

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

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) { enabled = false; return; }

        // 初期状態ではモニターを非表示にする
        if (idleMonitorObject != null)
        {
            idleMonitorObject.SetActive(false);
        }
    }

    void Update()
    {
        CheckIsGrounded();
        HandleMovementAnimationInput();
        HandleAirAnimationInput();
        HandleIdleTimer();
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
            animator.SetBool(IsFallingHash, false);
            animator.SetBool(IsRisingHash, false);
        }
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