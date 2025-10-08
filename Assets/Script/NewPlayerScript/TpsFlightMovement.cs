using UnityEngine;

public class TpsFlightMovement : MonoBehaviour
{
    [Header("移動設定")]
    public float walkSpeed = 5.0f;  // 通常の歩行速度
    public float runSpeed = 10.0f;  // ダッシュ速度
    public float flySpeed = 8.0f;   // 飛行速度

    private Rigidbody rb;
    private bool isFlying = false;

    void Start()
    {
        // Rigidbodyコンポーネントを取得
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("このスクリプトにはRigidbodyが必要です。");
            enabled = false;
        }

        // 物理演算による回転を防ぐ (TPSでの操作に適している)
        rb.freezeRotation = true;
    }

    void Update()
    {
        // === 飛行モード切り替え（スペースキー長押し） ===
        if (Input.GetKey(KeyCode.Space))
        {
            if (!isFlying)
            {
                isFlying = true;
                // 飛行開始時、重力を無効化
                rb.useGravity = false;
                // 既存のY軸速度をリセット（滑空を防ぐ）
                Vector3 currentVelocity = rb.velocity; // Rigidbody.velocity に修正
                rb.velocity = new Vector3(currentVelocity.x, 0, currentVelocity.z); // Rigidbody.velocity に修正
            }
        }
        else
        {
            if (isFlying)
            {
                isFlying = false;
                // 地上モードに戻る際、重力を有効化
                rb.useGravity = true;
            }
        }
    }

    // 物理演算の更新はFixedUpdateで行う
    void FixedUpdate()
    {
        // WASD入力の取得
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D
        float verticalInput = Input.GetAxis("Vertical");   // W/S

        // カメラ（またはプレイヤー）の向きを基準とした移動方向
        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // 現在のY軸速度を保持 (地上モードで使用)
        float currentYVelocity = rb.velocity.y; // Rigidbody.velocity に修正

        // === 移動の実行 ===
        if (isFlying)
        {
            HandleFlyingMovement(moveDirection);
        }
        else
        {
            HandleGroundMovement(moveDirection, currentYVelocity);
        }
    }

    /// <summary>
    /// 飛行モードでの移動処理
    /// </summary>
    private void HandleFlyingMovement(Vector3 moveDir)
    {
        // 自由飛行: WASDによる水平移動 + Y軸方向の移動
        float yInput = 0;
        if (Input.GetKey(KeyCode.Space)) // 上昇
        {
            yInput = 1;
        }
        else if (Input.GetKey(KeyCode.LeftControl)) // 下降（例）
        {
            yInput = -1;
        }

        Vector3 finalVelocity = moveDir.normalized * flySpeed;
        finalVelocity.y = yInput * flySpeed;

        rb.velocity = finalVelocity; // Rigidbody.velocity に修正
    }

    /// <summary>
    /// 地上モードでの移動とダッシュ処理
    /// </summary>
    private void HandleGroundMovement(Vector3 moveDir, float currentYVelocity)
    {
        // ダッシュ判定
        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed = runSpeed; // Shiftキーでダッシュ速度
        }

        // XZ平面の移動速度を設定
        Vector3 horizontalVelocity = moveDir.normalized * currentSpeed;

        // 最終的な速度 (X, Zは入力、YはRigidbodyの物理演算に任せる)
        rb.velocity = new Vector3(horizontalVelocity.x, currentYVelocity, horizontalVelocity.z); // Rigidbody.velocity に修正
    }
}