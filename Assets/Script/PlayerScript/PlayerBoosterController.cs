using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの移動・飛行・ブースト・燃料管理・モード切替を統括する制御クラス
/// </summary>
public class PlayerBoosterController : MonoBehaviour
{
    // --- モード別の移動速度設定（各アーマーモードの特徴に応じて調整） ---
    [Header("モード別パラメータ")]
    private float balanceSpeed = 10f;     // バランスモード：平均的な速度
    private float busterSpeed = 3f;       // バスターモード：重量級で遅い
    private float speedSpeed = 20f;       // スピードモード：非常に速い
    private float stealthSpeed = 8f;      // ステルスモード：やや軽快な移動

    // --- モード別の質量設定（Rigidbodyの挙動に影響） ---
    private float balanceMass = 1f;       // バランスモード：標準的な質量
    private float busterMass = 3f;        // バスターモード：重い（ノックバック耐性あり）
    private float speedMass = 0.8f;       // スピードモード：軽い（加速しやすい）
    private float stealthMass = 1.2f;     // ステルスモード：やや軽い

    private ArmorMode currentMode = ArmorMode.Balance; // 現在のアーマーモード

    // --- プレイヤーの通常移動に関する設定 ---
    [Header("移動設定")]
    private float moveSpeed = 10f;        // 現在有効な移動速度（モードに応じて変化）
    private float flyForce = 5.0f;        // 飛行上昇時の加速度（スペースキー）

    // --- 燃料に関するパラメータ ---
    [Header("燃料設定")]
    private float maxFuel = 100f;         // 最大燃料量
    private float fuelRegenRate = 20f;    // 地上での燃料回復速度（秒あたり）
    private float fuelBurnFly = 20f;      // 飛行・ブースト時の燃料消費量（秒あたり）
    public Slider fuelSlider;             // UIスライダーによる燃料残量表示

    // --- ブースト機能に関する設定 ---
    [Header("ブーストアクション設定")]
    private float boostSpeed = 80f;       // ブースト移動の速度
    private float boostDuration = 0.3f;   // ブーストが継続する時間（秒）
    private float boostCooldown = 2f;     // ブースト後に再使用可能になるまでの時間

    private bool isBoosting = false;              // ブースト中かどうか
    private float boostTimer = 0f;                // ブースト経過時間
    private float boostCooldownTimer = 0f;        // クールダウン用タイマー
    private Vector3 boostDirection;               // ブーストの進行方向

    private float currentFuel;                    // 現在の燃料残量
    private Rigidbody rb;                         // Rigidbodyへの参照
    public Transform cameraTransform;             // カメラのTransform（方向計算用）

    // --- 初期化処理 ---
    void Start()
    {
        rb = GetComponent<Rigidbody>();                     // Rigidbody取得
        currentFuel = maxFuel;                              // 燃料初期化
        Cursor.lockState = CursorLockMode.Locked;           // マウスカーソル固定（FPSスタイル）
        SetMode(currentMode);                               // 初期モード設定
    }

    // --- 毎フレームの更新処理 ---
    void Update()
    {
        HandleModeSwitch(); // モード切替入力検出（1〜4キー）
        Move();             // 通常移動入力処理
        Fly();              // 飛行処理（スペースキー）
        HandleFuel();       // 燃料の回復・消費管理
        UpdateFuelUI();     // UIスライダーの更新
        BoostAction();      // ブースト処理（右クリック）
    }

    /// <summary>
    /// WASD移動入力に応じた通常移動処理
    /// </summary>
    void Move()
    {
        float h = Input.GetAxis("Horizontal"); // A/Dキー（左右）
        float v = Input.GetAxis("Vertical");   // W/Sキー（前後）

        Vector3 inputDir = new Vector3(h, 0, v);                     // 入力ベクトル生成
        inputDir = Vector3.ClampMagnitude(inputDir, 1);             // 最大長を1に制限

        if (inputDir.magnitude > 0.1f)
        {
            // カメラの正面と右方向を水平面で取得（Y=0）
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            // カメラ基準の移動方向を計算
            Vector3 moveDir = camForward * v + camRight * h;
            moveDir.Normalize();

            // プレイヤーの回転方向を移動先へスムーズに補間
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 0.15f);

            // 水平方向の速度を更新（Y軸速度は維持）
            Vector3 velocity = moveDir * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            // 入力がない場合はX/Z速度をゼロに
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0;
            velocity.z = 0;
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// スペースキー押下による上昇移動（飛行）
    /// </summary>
    void Fly()
    {
        if (Input.GetKey(KeyCode.Space) && currentFuel > 0)
        {
            rb.AddForce(Vector3.up * flyForce, ForceMode.Acceleration); // 上方向の加速
            currentFuel -= fuelBurnFly * Time.deltaTime;                // 燃料を消費
        }
    }

    /// <summary>
    /// 燃料の自動回復・制限処理
    /// </summary>
    void HandleFuel()
    {
        bool notUsingFuel = !isBoosting && !Input.GetKey(KeyCode.Space); // 使用中でないか

        if (notUsingFuel && currentFuel < maxFuel)
        {
            currentFuel += fuelRegenRate * Time.deltaTime; // 自然回復
        }

        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel); // 範囲制限
    }

    /// <summary>
    /// UIスライダーに現在の燃料量を反映
    /// </summary>
    void UpdateFuelUI()
    {
        if (fuelSlider != null)
        {
            fuelSlider.value = currentFuel / maxFuel;
        }
    }

    /// <summary>
    /// ブーストアクション処理（右クリック入力）
    /// </summary>
    void BoostAction()
    {
        boostCooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(1) && boostCooldownTimer <= 0f && currentFuel > 0)
        {
            // 入力ベクトル（前後左右）
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // カメラ基準の移動ベクトルを生成
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 inputDir = camForward * verticalInput + camRight * horizontalInput;

            // 入力が小さい場合は前方にブースト
            if (inputDir.magnitude < 0.1f)
                inputDir = camForward;
            else
                inputDir.Normalize();

            boostDirection = inputDir;
            isBoosting = true;
            boostTimer = 0f;
            boostCooldownTimer = boostCooldown;
        }

        if (isBoosting)
        {
            // 水平方向にブースト速度適用
            Vector3 velocity = boostDirection * boostSpeed;
            velocity.y = rb.linearVelocity.y; // Y速度維持
            rb.linearVelocity = velocity;

            // 燃料消費
            currentFuel -= fuelBurnFly * Time.deltaTime;
            if (currentFuel <= 0)
            {
                currentFuel = 0;
                isBoosting = false; // 燃料切れで終了
            }

            // 時間経過でブースト終了
            boostTimer += Time.deltaTime;
            if (boostTimer >= boostDuration)
            {
                isBoosting = false;
            }
        }
    }

    /// <summary>
    /// アーマーモード切り替え（1～4キー）
    /// </summary>
    void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(ArmorMode.Balance);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(ArmorMode.Speed);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SetMode(ArmorMode.Stealth);
    }

    /// <summary>
    /// アーマーモードに応じた速度・質量の適用
    /// </summary>
    void SetMode(ArmorMode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case ArmorMode.Balance:
                moveSpeed = balanceSpeed;
                rb.mass = balanceMass;
                break;
            case ArmorMode.Buster:
                moveSpeed = busterSpeed;
                rb.mass = busterMass;
                break;
            case ArmorMode.Speed:
                moveSpeed = speedSpeed;
                rb.mass = speedMass;
                break;
            case ArmorMode.Stealth:
                moveSpeed = stealthSpeed;
                rb.mass = stealthMass;
                break;
        }
    }
}

/// <summary>
/// プレイヤーのアーマーモードの種類（切り替え可能）
/// </summary>
public enum ArmorMode
{
    Balance, // 標準バランス型
    Buster,  // 重装甲・鈍足型
    Speed,   // 高速・軽量型
    Stealth  // 軽装・静音型
}
