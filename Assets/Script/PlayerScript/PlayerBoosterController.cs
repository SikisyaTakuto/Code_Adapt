using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの移動とブースト飛行（燃料管理付き）
/// </summary>
public class PlayerBoosterController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("移動設定")]
    public float moveForce = 30f;
    public float maxSpeed = 25f;
    public float rotationSpeed = 100f;
    public float airControlMultiplier = 0.5f; // 空中時の移動力低下

    [Header("ジャンプ・ホバリング")]
    public float jumpForce = 8f;
    public float hoverForce = 5f;
    public float maxHoverTime = 3f;
    private float currentHoverTime;

    [Header("ブースト設定")]
    public float boostMultiplier = 2.5f;
    public float boostFuelMax = 5f;
    private float boostFuel;
    public float boostFuelConsumption = 1f;
    public float boostFuelRecharge = 0.5f;

    [Header("接地判定")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("UI")]
    public Slider boostSlider; // ブースト残量
    public Slider hoverSlider; // ホバリング残量

    // 横回転力.
    [SerializeField] float rotPower = 30000f;
    // 回転速度制限.
    [SerializeField] float rotationSqrLimit = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        boostFuel = boostFuelMax;
        currentHoverTime = maxHoverTime;

        if (boostSlider != null)
        {
            boostSlider.maxValue = boostFuelMax; // 初期設定
            boostSlider.value = boostFuel;
        }

        if (hoverSlider != null)
        {
            hoverSlider.maxValue = maxHoverTime;
            hoverSlider.value = currentHoverTime;
        }
    }

    void Update()
    {
        // カメラ操作の回転（Yaw）
        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up * mouseX * rotationSpeed * Time.deltaTime);

        // 接地確認
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // ジャンプ・ホバリング処理
        if (Input.GetKey(KeyCode.Space))
        {
            if (!isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            else if (currentHoverTime > 0f)
            {
                rb.AddForce(Vector3.up * hoverForce, ForceMode.Acceleration);
                currentHoverTime -= Time.deltaTime;
            }
        }
        else if(isGrounded)
        {
             currentHoverTime = maxHoverTime;
        }

        // ブースト燃料処理
        if (Input.GetMouseButton(0) && boostFuel > 0f)
        {
            boostFuel -= boostFuelConsumption * Time.deltaTime;
        }
        else
        {
            boostFuel += boostFuelRecharge * Time.deltaTime;
        }
        boostFuel = Mathf.Clamp(boostFuel, 0f, boostFuelMax);

        // 移動入力
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 moveInput = transform.forward * v + transform.right * h;
        Vector3 moveDir = moveInput.normalized;

        if (boostSlider != null) boostSlider.value = boostFuel;
        if (hoverSlider != null) hoverSlider.value = currentHoverTime;

        // ブースト力を適用
        float currentForce = moveForce;
        if (Input.GetMouseButton(0) && boostFuel > 0f)
        {
            currentForce *= boostMultiplier;
        }

        // 空中であれば移動力を減少させる
        if (!isGrounded)
        {
            currentForce *= airControlMultiplier;
        }

        // 入力方向の速度（速度制限チェック）
        float inputVelocity = Vector3.Dot(rb.linearVelocity, moveDir);
        if (inputVelocity < maxSpeed)
        {
            rb.AddForce(moveDir * currentForce, ForceMode.Acceleration);
        }

        // A/Dキーによる回転処理（物理ベース）
        RotationUpdate();
    }

    /// <summary>
    /// A/Dキーでの物理トルク回転処理
    /// </summary>
    // ------------------------------------------------------------
    void RotationUpdate()
    {
        float sqrAng = rb.angularVelocity.sqrMagnitude;
        if (sqrAng > rotationSqrLimit) return;

        if (Input.GetKey(KeyCode.A))
        {
            rb.AddTorque(-transform.up * rotPower, ForceMode.Force);
        }

        if (Input.GetKey(KeyCode.D))
        {
            rb.AddTorque(transform.up * rotPower, ForceMode.Force);
        }
    }
}