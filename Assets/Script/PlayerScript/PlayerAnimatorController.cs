using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    // 参照するAnimatorコンポーネント
    private Animator animator;

    // CharacterController (接地判定に利用するため、アタッチ推奨)
    private CharacterController characterController;

    // パラメーターのハッシュ値を格納する変数 (パフォーマンス向上のため)
    private int isRunningHash;
    private int isFallingHash;
    private int isRisingHash;
    private int isDashingHash;
    private int attack1Hash;
    private int attack2Hash;

    // 武器モードの状態（アニメーション制御のため、簡易的に保持）
    private bool isMeleeMode = true; // true: Attack1 (近接), false: Attack2 (ビーム)

    void Start()
    {
        // アタッチされているAnimatorコンポーネントを取得
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            Debug.LogError("Animatorコンポーネントがアタッチされていません！");
            return;
        }

        // パラメーターのハッシュ値を事前に取得
        isRunningHash = Animator.StringToHash("IsRunning");
        isFallingHash = Animator.StringToHash("IsFalling");
        isRisingHash = Animator.StringToHash("IsRising");
        isDashingHash = Animator.StringToHash("IsDashing");
        attack1Hash = Animator.StringToHash("Attack1");
        attack2Hash = Animator.StringToHash("Attack2");
    }

    void Update()
    {
        // Animatorが存在しない場合は処理をスキップ
        if (animator == null) return;

        // --- 入力判定 ---

        bool hasHorizontalInput =
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.D);

        // Space: 上昇入力 (GetKeyで常時判定)
        bool isRisingInput = Input.GetKey(KeyCode.Space);

        // Alt: 下降入力 (GetKeyで常時判定)
        bool isFallingInput = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // Shift: ダッシュ入力
        bool isDashingInput = Input.GetKey(KeyCode.LeftShift);


        // --- 1. 水平移動アニメーションの制御 ---
        animator.SetBool(isRunningHash, hasHorizontalInput);
        animator.SetBool(isDashingHash, isDashingInput && hasHorizontalInput);


        // -----------------------------------------------------------------
        // ★ 2. 垂直方向（飛行/落下）アニメーションの制御 (修正ブロック) ★
        // -----------------------------------------------------------------

        if (isRisingInput)
        {
            // Spaceキーが押されている間、IsRisingをTrueに設定し続ける
            animator.SetBool(isRisingHash, true);
            // 上昇中はIsFallingを解除
            animator.SetBool(isFallingHash, false);
        }
        else if (isFallingInput)
        {
            // Altキーが押されている間、IsFallingをTrueに設定
            animator.SetBool(isFallingHash, true);
            // 下降中はIsRisingを解除
            animator.SetBool(isRisingHash, false);
        }
        else
        {
            // SpaceもAltも押されていない場合、両方をFalseにリセット
            animator.SetBool(isRisingHash, false);
            animator.SetBool(isFallingHash, false);

            // NOTE: CharacterControllerがある場合、isGroundedがFalseであれば
            // 垂直速度が負になった瞬間に fall ステートへ自動遷移させるよう
            // Animator Controllerの遷移を設定することが推奨されます。
        }

        // -----------------------------------------------------------------


        // --- 3. 武器切り替え (Eキー) ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            isMeleeMode = !isMeleeMode;
            Debug.Log($"武器モードを切り替えました: {(isMeleeMode ? "近接 (Attack1)" : "ビーム (Attack2)")}");
        }

        // --- 4. 攻撃アニメーションの制御 (左クリック) ---
        if (Input.GetMouseButtonDown(0))
        {
            if (isMeleeMode)
            {
                // メイン武装 (近接) をトリガー
                animator.SetTrigger(attack1Hash);
            }
            else
            {
                // サブ武装 (ビーム) をトリガー
                animator.SetTrigger(attack2Hash);
            }
        }

        // --- 5. ロックオン/設定 ---
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("右クリック: ロックオン");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Esc: 設定画面を開く");
        }
    }
}