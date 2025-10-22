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

    // --- エネルギーゲージ関連 ---
    [Header("Energy Gauge Settings")]
    public float maxEnergy = 100.0f;
    [HideInInspector] public float currentEnergy;
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime;
    public Slider energySlider;
    private bool hasTriggeredEnergyDepletedEvent = false;

    // --- 内部状態と移動関連 ---
    private Vector3 velocity;
    private bool isAttacking = false;
    private float attackTimer = 0.0f;
    public float attackFixedDuration = 0.8f;

    // --- 近接攻撃関連 ---
    [Header("Melee Attack Settings")]
    public float meleeAttackRadius = 1.0f;
    public float meleeAttackCooldown = 0.5f;
    private float lastMeleeAttackTime = -Mathf.Infinity;
    private int currentMeleeCombo = 0;
    public int maxMeleeCombo = 5;
    public float comboResetTime = 1.0f;
    private float lastMeleeInputTime;
    public float autoLockOnMeleeRange = 5.0f;
    public bool preferLockedMeleeTarget = true;
    private Transform currentLockedMeleeTarget;
    public LayerMask enemyLayer;

    // --- ビーム攻撃関連 ---
    [Header("Beam Attack Settings")]
    public float beamAttackRange = 30.0f;
    public float beamCooldown = 0.5f;
    private float lastBeamAttackTime = -Mathf.Infinity;
    public GameObject beamEffectPrefab;
    public Transform beamSpawnPoint;
    public float beamWidth = 0.5f;
    public float beamDisplayDuration = 0.5f;
    public float autoLockOnRange = 40.0f;
    public bool preferLockedTarget = true;
    private Transform currentLockedBeamTarget;

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
        CheckWarnings();
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

    /// <summary>設定漏れの警告チェック</summary>
    private void CheckWarnings()
    {
        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("PlayerController: Beam Spawn Pointが設定されていません。");
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
        else // 攻撃中でない場合のみ、カメラ方向への回転を行う
        {
            tpsCamController?.RotatePlayerToCameraDirection();
        }

        HandleAttackInputs();
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

    /// <summary>攻撃入力の処理</summary>
    private void HandleAttackInputs()
    {
        if (Input.GetMouseButtonDown(0)) PerformMeleeAttack();
        else if (Input.GetMouseButtonDown(1)) PerformBeamAttack();
    }

    // ターゲット検索の共通ロジック
    private Transform FindClosestEnemy(float range)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);
        if (hits.Length == 0) return null;

        return hits.OrderBy(col => Vector3.Distance(transform.position, col.transform.position))
                   .Select(col => col.transform)
                   .FirstOrDefault(t => t != transform);
    }

    void PerformMeleeAttack()
    {
        if (Time.time < lastMeleeAttackTime + meleeAttackCooldown) return;

        lastMeleeAttackTime = lastMeleeInputTime = Time.time;
        isAttacking = true;
        attackTimer = 0.0f;

        currentLockedMeleeTarget = FindClosestEnemy(autoLockOnMeleeRange);

        if (currentLockedMeleeTarget != null && preferLockedMeleeTarget)
        {
            Vector3 lookAtTarget = currentLockedMeleeTarget.position;
            lookAtTarget.y = transform.position.y;
            transform.LookAt(lookAtTarget);
        }

        PerformMeleeDamageCheck(); // 突進せずに即座に攻撃判定

        currentMeleeCombo = (currentMeleeCombo % maxMeleeCombo) + 1;
        onMeleeAttackPerformed?.Invoke();

        Debug.Log($"近接攻撃実行! (コンボ: {currentMeleeCombo})");
    }

    void PerformMeleeDamageCheck()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, meleeAttackRadius, transform.forward, meleeAttackRange, enemyLayer);

        foreach (RaycastHit hit in hits)
        {
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>() ?? hit.collider.GetComponentInChildren<EnemyHealth>();
            enemyHealth?.TakeDamage(meleeDamage);
        }
    }

    void PerformBeamAttack()
    {
        if (Time.time < lastBeamAttackTime + beamCooldown || currentEnergy < beamAttackEnergyCost)
        {
            return;
        }

        currentEnergy -= beamAttackEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;
        isAttacking = true;
        attackTimer = 0.0f;

        currentLockedBeamTarget = FindClosestEnemy(autoLockOnRange);

        Vector3 startPoint = beamSpawnPoint.position;
        Vector3 endPoint;
        RaycastHit hitInfo;
        Transform hitEnemyTransform = null;
        Ray attackRay = default;

        if (currentLockedBeamTarget != null && preferLockedTarget)
        {
            // ロックオン時の処理
            Vector3 lookAtTarget = currentLockedBeamTarget.position;
            lookAtTarget.y = transform.position.y;
            transform.LookAt(lookAtTarget);
            attackRay = new Ray(startPoint, (currentLockedBeamTarget.position - startPoint).normalized);
        }
        else
        {
            // ノンロックオン (カメラ基準) の処理
            attackRay = tpsCamController.GetCameraRay();
        }

        // Raycastでヒット判定
        if (Physics.Raycast(attackRay, out hitInfo, beamAttackRange, enemyLayer))
        {
            endPoint = hitInfo.point;
            hitEnemyTransform = hitInfo.collider.transform;
        }
        else
        {
            endPoint = attackRay.origin + attackRay.direction * beamAttackRange;
        }

        StartCoroutine(ShowBeamEffectAndLine(startPoint, endPoint, hitEnemyTransform));
        onBeamAttackPerformed?.Invoke();
    }

    IEnumerator ShowBeamEffectAndLine(Vector3 startPoint, Vector3 endPoint, Transform hitEnemyTransform)
    {
        GameObject beamVisualizer = new GameObject("BeamVisualizer");
        LineRenderer lineRenderer = beamVisualizer.AddComponent<LineRenderer>();

        // Line Renderer の設定を簡略化
        lineRenderer.startWidth = lineRenderer.endWidth = beamWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = Color.cyan };
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.blue;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        // ビームエフェクトの生成
        if (beamEffectPrefab != null)
        {
            GameObject beamEffectInstance = Instantiate(beamEffectPrefab, startPoint, Quaternion.identity, beamVisualizer.transform);
            beamEffectInstance.transform.LookAt(endPoint);
        }

        // 敵にダメージを与える
        EnemyHealth enemyHealth = hitEnemyTransform?.GetComponent<EnemyHealth>() ?? hitEnemyTransform?.GetComponentInChildren<EnemyHealth>();
        enemyHealth?.TakeDamage(beamDamage);

        yield return new WaitForSeconds(beamDisplayDuration);
        Destroy(beamVisualizer);
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
}