using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの移動とブースト飛行（燃料管理付き）を制御するクラス
/// </summary>
public class PlayerBoosterController : MonoBehaviour
{
    [Header("移動設定")]
    private float moveSpeed = 10f;           // 通常移動速度
    private float groundBoostForce = 2000f; // 地上でのブーストにかかる力
    private float airBoostForce = 500f;     // 空中でのブーストにかかる力
    private float flyForce = 4.0f;          // 飛行時に加える上昇力

    [Header("燃料設定")]
    private float maxFuel = 1000f;          // 最大燃料量
    private float fuelRegenRate = 20f;      // 地上で燃料が回復する速度（単位：秒あたり）
    private float fuelBurnBoost = 20f;      // ブースト開始時に一度消費する燃料量
    private float fuelBurnFly = 20f;        // 飛行時に1秒間あたり消費する燃料量
    public Slider fuelSlider;               // 燃料残量を表示するUIスライダー

    private bool isBoosting = false;        // 現在ブースト中かどうかのフラグ
    private float boostDuration = 0.5f;     // ブーストの持続時間（秒）
    private float boostTimer = 0f;          // ブースト開始からの経過時間を計測

    private float currentFuel;              // 現在の燃料残量
    private Rigidbody rb;                   // Rigidbodyコンポーネントへの参照（物理制御用）
    public Transform cameraTransform;       // カメラのTransform（移動方向の基準として使用）

    // 初期化処理
    void Start()
    {
        rb = GetComponent<Rigidbody>();                // Rigidbodyコンポーネントを取得
        currentFuel = maxFuel;                         // 燃料を最大値に初期化
        Cursor.lockState = CursorLockMode.Locked;      // マウスカーソルを画面中央に固定
    }

    // 毎フレーム呼ばれる更新処理
    void Update()
    {
        Move();             // プレイヤーの移動入力を処理
        Fly();              // スペースキーによる飛行操作
        Boost();            // 左クリックによるブースト操作
        HandleFuel();       // 燃料の消費・回復を管理
        UpdateFuelUI();     // UIスライダーを最新の燃料残量に更新
    }

    /// <summary>
    /// WASD入力による通常移動処理
    /// </summary>
    void Move()
    {
        float h = Input.GetAxis("Horizontal"); // 水平方向入力（A/Dキー）
        float v = Input.GetAxis("Vertical");   // 前後方向入力（W/Sキー）

        Vector3 inputDir = new Vector3(h, 0, v);           // 入力ベクトルを作成
        inputDir = Vector3.ClampMagnitude(inputDir, 1);    // ベクトルの大きさを最大1に制限

        if (inputDir.magnitude > 0.1f) // ある程度の入力がある場合のみ移動処理
        {
            // カメラのY軸方向（水平面）を取得し正規化
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            // カメラの右方向（水平面）を取得し正規化
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            // カメラ方向に合わせて移動方向を決定
            Vector3 moveDir = camForward * v + camRight * h;
            moveDir.Normalize();

            // プレイヤーのY軸回転をスムーズに移動方向へ向ける
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 0.15f);

            // Rigidbodyの速度を設定（Y軸の速度は現在維持）
            Vector3 velocity = moveDir * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            // 入力なしの場合、水平速度を0にしてY軸の速度は維持
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0;
            velocity.z = 0;
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// スペースキー押下時の飛行（上昇）処理と燃料消費
    /// </summary>
    void Fly()
    {
        // スペースキー押下かつ燃料が残っている場合に上昇処理
        if (Input.GetKey(KeyCode.Space) && currentFuel > 0)
        {
            // Rigidbodyに上方向への加速度を加える
            rb.AddForce(Vector3.up * flyForce, ForceMode.Acceleration);
            // 燃料を時間に応じて減らす
            currentFuel -= fuelBurnFly * Time.deltaTime;
        }
    }

    /// <summary>
    /// 左クリックによるブースト処理（前方向への加速）と燃料消費
    /// </summary>
    void Boost()
    {
        // 左クリックでブースト開始（燃料十分かつ現在ブースト中でない場合）
        if (Input.GetMouseButtonDown(0) && currentFuel >= fuelBurnBoost && !isBoosting)
        {
            isBoosting = true;           // ブースト状態に切り替え
            boostTimer = 0f;             // ブースト時間をリセット
            currentFuel -= fuelBurnBoost; // ブースト開始時に燃料を先に消費
        }

        if (isBoosting)
        {
            // 地上にいるか空中かで加える力を切り替え
            float currentBoostForce = IsGrounded() ? groundBoostForce : airBoostForce;

            // カメラの向き（前方向）に基づいて加速方向を計算
            Vector3 boostDirection = cameraTransform.forward.normalized;

            // Rigidbodyに加速度を加える（Time.deltaTimeでフレームレート補正）
            rb.AddForce(boostDirection * currentBoostForce * Time.deltaTime * 10f, ForceMode.Acceleration);

            // ブースト時間を加算
            boostTimer += Time.deltaTime;
            // ブースト時間が規定値に達したらブースト終了
            if (boostTimer >= boostDuration)
            {
                isBoosting = false;
            }

            // ブースト中は燃料を時間経過に応じて消費し続ける
            currentFuel -= fuelBurnBoost * Time.deltaTime / boostDuration;
        }
    }

    /// <summary>
    /// 燃料の回復処理と最大・最小範囲への制限
    /// </summary>
    void HandleFuel()
    {
        // ブーストも飛行もしていない状態のみ燃料を回復させる
        bool notUsingFuel = !isBoosting && !Input.GetKey(KeyCode.Space);

        if (notUsingFuel && currentFuel < maxFuel)
        {
            currentFuel += fuelRegenRate * Time.deltaTime; // 燃料を時間に応じて回復
        }

        // 燃料の値を0〜maxFuelの範囲内に制限
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
    }

    /// <summary>
    /// プレイヤーが地面に接地しているかどうかを判定する
    /// </summary>
    /// <returns>接地していればtrue、そうでなければfalse</returns>
    bool IsGrounded()
    {
        // プレイヤーの足元から真下へRaycastを飛ばし、1.1m以内に地面があれば接地と判定
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    /// <summary>
    /// UIの燃料スライダーの値を更新する
    /// </summary>
    void UpdateFuelUI()
    {
        if (fuelSlider != null)
        {
            // 燃料の割合（0〜1）をスライダーの値に反映
            fuelSlider.value = currentFuel / maxFuel;
        }
    }
}
