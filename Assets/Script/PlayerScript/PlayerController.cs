using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Linq; // FindClosestEnemyで使用

/// <summary>
/// プレイヤーの移動、エネルギー管理、攻撃を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // 依存オブジェクト
    private CharacterController controller;
    private TPSCameraController tpsCamController;

    // --- ベースとなる能力値 ---
    [Header("Base Stats")]
    public float moveSpeed = 15.0f;
    public float boostMultiplier = 2.0f;
    public float verticalSpeed = 10.0f;
    public float energyConsumptionRate = 15.0f;
    public float energyRecoveryRate = 10.0f;
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public bool canFly = true;
    public float gravity = -9.81f;

    //[HPゲージ関連の追加]
    [Header("Health Settings")]
    public float maxHP = 100.0f; // 最大HP
    [HideInInspector] public float currentHP; // 現在HP
    public Slider hPSlider; // HPスライダー (UI)

    //エネルギーゲージ関連
    [Header("Energy Gauge Settings")]
    public float maxEnergy = 100.0f;
    [HideInInspector] public float currentEnergy;
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime;
    public Slider energySlider;
    private bool hasTriggeredEnergyDepletedEvent = false;

    // ロックオン関連の設定 
    [Header("Lock-On Settings")]
    public LayerMask enemyLayer;             // 敵のレイヤー
    public float maxLockOnRange = 30.0f;     // ロックオン可能な最大距離
    public float lockOnAngle = 120.0f;       // カメラ前方の視野角 (120度)

    [Header("Lock-On UI Settings")]
    public GameObject lockOnUIPrefab; // ロックオン強調表示用のUI (EnemyLockOnUIスクリプトを持つImage)
    private GameObject currentLockOnUIInstance; // 現在表示中のUIインスタンスの参照

    public Transform currentLockOnTarget { get; private set; } // 現在のロックオン対象

    // 内部状態と移動関連
    private Vector3 velocity;
    private bool isAttacking = false;
    private float attackTimer = 0.0f;
    public float attackFixedDuration = 0.8f;

    // プレイヤー入力制御
    public bool canReceiveInput = true;

    // チュートリアル用イベントとプロパティ
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    public event Action onEnergyDepleted;

    // 自動実装プロパティ
    public float WASDMoveTimer { get; private set; }
    public float JumpTimer { get; private set; }
    public float DescendTimer { get; private set; }


    void Start()
    {
        InitializeComponents();
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
        currentHP = maxHP; // 現在HPを最大HPで初期化
        UpdateHPUI();
    }

    /// <summary>コンポーネントの初期化とエラーチェック</summary>
    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterControllerが見つかりません。");
            enabled = false;
            return;
        }

        tpsCamController = FindObjectOfType<TPSCameraController>();
        if (tpsCamController == null)
        {
            Debug.LogWarning("PlayerController: TPSCameraControllerが見つかりません。");
        }
    }

    void Update()
    {
        // 攻撃中または入力無効化中は移動・攻撃入力をブロック
        if (!canReceiveInput || isAttacking)
        {
            HandleAttackState();
            WASDMoveTimer = JumpTimer = DescendTimer = 0f;
        }
        else // 攻撃中でない場合
        {
            //Lock-On処理を呼び出す
            HandleLockOn();

            // ロックオン対象がいなければカメラ方向に回転
            if (currentLockOnTarget == null)
            {
                tpsCamController?.RotatePlayerToCameraDirection();
            }
        }

        //HandleAttackInputs();
        HandleEnergy();

        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
        controller.Move(finalMove * Time.deltaTime);
    }

    /// <summary>水平方向の移動処理</summary>
    private Vector3 HandleHorizontalMovement()
    {
        // 攻撃中は水平移動を停止
        if (isAttacking) return Vector3.zero;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 moveDirection = tpsCamController != null
            ? tpsCamController.transform.rotation * new Vector3(h, 0, v)
            : transform.right * h + transform.forward * v;

        moveDirection.y = 0;
        moveDirection.Normalize();

        float currentSpeed = moveSpeed;
        bool isConsumingEnergy = false;

        // ブースト処理
        bool isBoosting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentEnergy > 0;
        if (isBoosting)
        {
            currentSpeed *= boostMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }

        // エネルギー枯渇時の速度制限
        if (currentEnergy <= 0.01f)
        {
            currentSpeed = moveSpeed;
        }

        Vector3 horizontalMove = moveDirection * currentSpeed;

        // チュートリアル用タイマー更新
        WASDMoveTimer = horizontalMove.magnitude > moveSpeed * 0.1f ? WASDMoveTimer + Time.deltaTime : 0f;

        if (isConsumingEnergy) lastEnergyConsumptionTime = Time.time;

        return horizontalMove;
    }

    /// <summary>垂直方向の移動処理と重力適用</summary>
    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -0.1f;

        bool isConsumingEnergy = false;
        bool hasVerticalInput = false;

        if (canFly && currentEnergy > 0.01f)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                velocity.y = verticalSpeed;
                JumpTimer += Time.deltaTime;
                DescendTimer = 0f;
                hasVerticalInput = isConsumingEnergy = true;
            }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                velocity.y = -verticalSpeed;
                DescendTimer += Time.deltaTime;
                JumpTimer = 0f;
                hasVerticalInput = isConsumingEnergy = true;
            }
        }

        // 入力がない、またはエネルギーが枯渇した場合の垂直移動の処理
        if (!hasVerticalInput)
        {
            // 上昇/下降入力がない場合はタイマーリセット
            if (Input.GetKeyUp(KeyCode.Space)) JumpTimer = 0f;
            if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt)) DescendTimer = 0f;

            // 重力適用
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        // エネルギー消費
        if (isConsumingEnergy)
        {
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            lastEnergyConsumptionTime = Time.time;
        }

        // エネルギー枯渇時の垂直方向の制御を停止
        if (currentEnergy <= 0.01f && velocity.y > 0)
        {
            velocity.y = 0;
            JumpTimer = DescendTimer = 0f;
        }

        return new Vector3(0, velocity.y, 0);
    }

    /// <summary>エネルギー回復と枯渇イベントの処理</summary>
    private void HandleEnergy()
    {
        // エネルギー回復
        if (Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            currentEnergy += energyRecoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        // エネルギー枯渇イベントの発火とフラグの管理
        if (currentEnergy <= 0.1f && !hasTriggeredEnergyDepletedEvent)
        {
            onEnergyDepleted?.Invoke();
            hasTriggeredEnergyDepletedEvent = true;
        }
        else if (currentEnergy > 0.1f && hasTriggeredEnergyDepletedEvent && Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            hasTriggeredEnergyDepletedEvent = false;
        }

        UpdateEnergyUI();
    }

    /// <summary>攻撃中のプレイヤーの状態を処理（移動ロックなど）</summary>
    void HandleAttackState()
    {
        if (!isAttacking) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            attackTimer = 0.0f;

            // 攻撃終了時: 接地していなければ重力の影響を受け始める
            if (!controller.isGrounded)
            {
                velocity.y = 0; // 攻撃中の固定を解除するが、急降下を防ぐために初期速度は0にする
            }
            else
            {
                velocity.y = -0.1f; // 接地判定を維持
            }
        }
    }

    // ロックオン機能関連のメソッド

    /// <summary>
    /// カメラの視野角内の最も近い敵を検索し、ロックオン対象とする。
    /// </summary>
    private void HandleLockOn()
    {
        // ロックオン解除の入力処理 (例: Rキー)
        if (Input.GetKeyDown(KeyCode.R)) // 例としてRキーを使用
        {
            currentLockOnTarget = null;
            UpdateLockOnUI(null); // ロックオンUIを破棄
            return;
        }

        // ロックオンキー (例: Qキー) が押されたら、新規ロックオンを試みる
        if (Input.GetKeyDown(KeyCode.Q)) // 例としてQキーを使用
        {
            FindAndLockOnClosestEnemy();
        }

        // ロックオン対象が既にいる場合、有効性のチェック
        if (currentLockOnTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentLockOnTarget.position);

            // 1. 距離チェック
            // ロックオンを維持できる距離は検索距離より少し長めに設定
            if (distance > maxLockOnRange * 1.5f)
            {
                Debug.Log("ロックオン対象が遠すぎるため解除");
                currentLockOnTarget = null; // 遠すぎたらロックオン解除
                UpdateLockOnUI(null); // ロックオンUIを破棄
                return;
            }

            // 2. 視野角チェック: lockOnAngle (120度) の範囲外に出たら解除
            if (!IsTargetInCameraViewAngle(currentLockOnTarget, lockOnAngle))
            {
                Debug.Log("ロックオン対象がカメラの視野角外に出たため解除");
                currentLockOnTarget = null;
                UpdateLockOnUI(null); // ロックオンUIを破棄
            }
        }
    }

    /// <summary>
    /// カメラの前方視野角内の最も近い敵を検索し、ロックオン対象に設定する。
    /// </summary>
    private void FindAndLockOnClosestEnemy()
    {
        if (tpsCamController == null)
        {
            Debug.LogWarning("TPSCameraControllerが見つからないため、ロックオンできません。");
            return;
        }

        // SphereCastで敵を検索
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, maxLockOnRange, enemyLayer);

        if (hitColliders.Length == 0)
        {
            currentLockOnTarget = null;
            UpdateLockOnUI(null); // ターゲットが見つからなければUIを破棄
            return;
        }

        Transform bestTarget = null;
        float minDistance = float.MaxValue;

        // 視野角内の最も近い敵を見つける
        foreach (var hitCollider in hitColliders)
        {
            Transform target = hitCollider.transform;
            if (target == transform) continue; // 自分自身を除く

            float distance = Vector3.Distance(transform.position, target.position);

            // 距離と視野角の条件を満たすかチェック (lockOnAngle = 120.0f)
            if (distance <= maxLockOnRange && IsTargetInCameraViewAngle(target, lockOnAngle))
            {
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestTarget = target;
                }
            }
        }

        currentLockOnTarget = bestTarget;

        UpdateLockOnUI(currentLockOnTarget);

        if (currentLockOnTarget != null)
        {
            Debug.Log($"ロックオン: {currentLockOnTarget.name}");
        }
    }

    /// <summary>
    /// 対象が現在のカメラの視野角内にいるかを判定する。
    /// </summary>
    private bool IsTargetInCameraViewAngle(Transform target, float angleLimit)
    {
        if (tpsCamController == null) return false;

        // カメラからターゲットへのベクトル
        Vector3 toTarget = (target.position - tpsCamController.transform.position).normalized;
        // カメラの前方ベクトルとターゲットへのベクトルの角度を計算
        float angle = Vector3.Angle(tpsCamController.transform.forward, toTarget);

        // 許容角度の半分以内であれば視野角内と判定
        return angle <= angleLimit / 2.0f;
    }

    /// <summary>
    /// ロックオンUIの表示を更新する。
    /// </summary>
    private void UpdateLockOnUI(Transform target)
    {
        // ターゲットがない場合 (ロックオン解除時)
        if (target == null)
        {
            if (currentLockOnUIInstance != null)
            {
                // UIインスタンスが存在すれば破棄
                Destroy(currentLockOnUIInstance);
                currentLockOnUIInstance = null;
            }
        }
        // ターゲットがある場合 (新規ロックオンまたは継続時)
        else
        {
            // 初回ロックオン時 (またはUIが破棄された後の再ロックオン時)
            if (currentLockOnUIInstance == null && lockOnUIPrefab != null)
            {
                // UIを生成し、Canvasの子として配置されるように設定
                // LockOnUIPrefabには EnemyLockOnUI がアタッチされている必要がある
                currentLockOnUIInstance = Instantiate(lockOnUIPrefab);

                // EnemyLockOnUIスクリプトを取得し、ターゲットを設定
                EnemyLockOnUI uiScript = currentLockOnUIInstance.GetComponent<EnemyLockOnUI>();
                if (uiScript != null)
                {
                    uiScript.SetTarget(target);
                }
                else
                {
                    Debug.LogError("LockOnUIPrefabにEnemyLockOnUIスクリプトがアタッチされていません。");
                    Destroy(currentLockOnUIInstance); // スクリプトがない場合は破棄
                    currentLockOnUIInstance = null;
                }
            }
            // ターゲットが変わった場合 (もしあれば、ただしFindAndLockOnClosestEnemyでは最も近い敵にしかロックオンしないため、通常は起こらない)
            // ここではターゲットの変更を検知するより、シンプルな構造を維持する。
            // ターゲットが設定されていれば EnemyLockOnUI.cs が自動で追従する。
        }
    }

    // チュートリアル・UI関連のメソッド

    /// <summary>チュートリアル用の入力追跡フラグとタイマーをリセットする。</summary>
    public void ResetInputTracking()
    {
        WASDMoveTimer = JumpTimer = DescendTimer = 0f;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    /// <summary>HPスライダーを更新する。</summary>
    void UpdateHPUI()
    {
        if (hPSlider != null)
        {
            // 現在HPを最大HPで割った値をスライダーの値として設定
            hPSlider.value = currentHP / maxHP;
        }
    }
}