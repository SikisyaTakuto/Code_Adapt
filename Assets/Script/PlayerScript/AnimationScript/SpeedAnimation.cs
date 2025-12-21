using UnityEngine;

public class SpeedAnimation : MonoBehaviour
{
    private Animator animator;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
    private bool isGrounded = true;

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
        if (animator == null) { enabled = false; return; }
    }

    void Update()
    {
        CheckIsGrounded();
        HandleGroundAndAirState();
        HandleMovement();
        // HandleAttackInput(); // 削除：コントローラー側で制御
        // HandleOtherInput();  // 削除：コントローラー側で制御
    }

    // ★追加：コントローラーから呼ばれる攻撃アニメ再生メソッド
    public void PlayAttackAnimation(bool isModeTwo)
    {
        if (animator == null) return;
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