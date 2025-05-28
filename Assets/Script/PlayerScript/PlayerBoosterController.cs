using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの移動とブースト飛行（燃料管理付き）
/// </summary>
public class PlayerBoosterController : MonoBehaviour
{
    [Header("移動設定")]
    private float moveSpeed = 5f;           // 通常の移動速度
    private float boostForce = 200f;         // ブースト加速の力（前方向）
    private float flyForce = 10f;           // 飛行上昇の力

    [Header("燃料設定")]
    private float maxFuel = 100f;           // 最大燃料量
    private float fuelRegenRate = 20f;      // 地上での燃料回復速度
    private float fuelBurnBoost = 30f;      // ブースト時に消費する燃料量（1回）
    private float fuelBurnFly = 20f;        // 飛行時の燃料消費（1秒あたり）
    public Slider fuelSlider;               // UIスライダー（燃料残量表示）

    private float currentFuel;              // 現在の燃料量
    private Rigidbody rb;                   // Rigidbodyコンポーネントへの参照
    public Transform cameraTransform;       // 追従してるカメラのTransform
    void Start()
    {
        rb = GetComponent<Rigidbody>();                // Rigidbodyの取得
        currentFuel = maxFuel;                         // 初期燃料を最大に設定
        Cursor.lockState = CursorLockMode.Locked;      // マウスカーソルを画面中央に固定
    }

    void Update()
    {
        Move();             // 通常移動
        Fly();              // 飛行操作（スペースキー）
        Boost();            // ブースト操作（左クリック）
        HandleFuel();       // 燃料の回復と制限処理
        UpdateFuelUI();     // UIスライダーの更新
    }

    /// <summary>
    /// WASDによる移動処理
    /// </summary>
    void Move()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        Vector3 inputDir = new Vector3(h, 0, v);
        inputDir = Vector3.ClampMagnitude(inputDir, 1);

        if (inputDir.magnitude > 0.1f)
        {
            // カメラのY軸の向きを取得（水平回転のみ）
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            // カメラの向きに応じて移動方向を決定
            Vector3 moveDir = camForward * v + camRight * h;
            moveDir.Normalize();

            // プレイヤーを移動方向に向ける（Y軸のみ回転）
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 0.15f);

            // Rigidbodyの速度を設定（Y軸速度は維持）
            Vector3 velocity = moveDir * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            // 入力なしなら横移動速度は0、Y軸速度維持
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0;
            velocity.z = 0;
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// スペースキーでの上昇（飛行）処理と燃料消費
    /// </summary>
    void Fly()
    {
        if (Input.GetKey(KeyCode.Space) && currentFuel > 0)
        {
            rb.AddForce(Vector3.up * flyForce, ForceMode.Acceleration);         // 上昇力を加える
            currentFuel -= fuelBurnFly * Time.deltaTime;                        // 時間に応じて燃料を消費
        }
    }

    /// <summary>
    /// 左クリックによる前方向へのブースト処理と燃料消費
    /// </summary>
    void Boost()
    {
        if (Input.GetMouseButtonDown(0) && currentFuel > 0)
        {
            rb.AddForce(transform.forward * boostForce, ForceMode.Impulse);     // ブースト力を一気に加える
            currentFuel -= fuelBurnBoost;                                       // 一度に一定量燃料を消費
        }
    }

    /// <summary>
    /// 燃料の回復と制限処理（地上にいるときのみ回復）
    /// </summary>
    void HandleFuel()
    {
        if (IsGrounded() && currentFuel < maxFuel)
        {
            currentFuel += fuelRegenRate * Time.deltaTime;                      // 地上なら徐々に回復
        }

        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);                     // 0〜最大燃料で制限
    }

    /// <summary>
    /// 地面に接地しているかどうかをRaycastで判定
    /// </summary>
    /// <returns>地面に触れているかどうか</returns>
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    /// <summary>
    /// UIスライダーに燃料残量を反映
    /// </summary>
    void UpdateFuelUI()
    {
        if (fuelSlider != null)
        {
            fuelSlider.value = currentFuel / maxFuel; // 0〜1の割合で設定
        }
    }
}
