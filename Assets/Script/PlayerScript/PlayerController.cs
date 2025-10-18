using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// プレイヤーの移動、エネルギー管理、近接攻撃、ビーム攻撃を制御する。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // 依存オブジェクト
    private CharacterController controller;
    private TPSCameraController tpsCamController;

    // --- ベースとなる能力値 ---
    [Header("Base Stats")]
    [Tooltip("ベースの移動速度。")]
    public float moveSpeed = 15.0f;

    [Tooltip("ブースト時の移動速度の乗数。")]
    public float boostMultiplier = 2.0f;

    [Tooltip("垂直方向の移動速度（上昇/下降）。")]
    public float verticalSpeed = 10.0f;

    [Tooltip("ブースト/垂直移動時のエネルギー消費率 (1秒あたり)。")]
    public float energyConsumptionRate = 15.0f;

    [Tooltip("非消費時のエネルギー回復率 (1秒あたり)。")]
    public float energyRecoveryRate = 10.0f;

    [Tooltip("近接攻撃が届く最大距離。")]
    public float meleeAttackRange = 2.0f;

    [Tooltip("近接攻撃の基本ダメージ。")]
    public float meleeDamage = 50.0f;

    [Tooltip("ビーム攻撃の基本ダメージ。")]
    public float beamDamage = 50.0f;

    [Tooltip("ビーム攻撃1回あたりのエネルギーコスト。")]
    public float beamAttackEnergyCost = 30.0f;

    [Tooltip("飛行機能が有効かどうかのフラグ。")]
    public bool canFly = true;

    [Tooltip("プレイヤーに適用される重力の強さ。")]
    public float gravity = -9.81f;

    // --- エネルギーゲージ関連の変数 ---
    [Header("Energy Gauge Settings")]
    [Tooltip("最大エネルギー量。")]
    public float maxEnergy = 100.0f;

    [HideInInspector]
    public float currentEnergy;
    [Tooltip("エネルギー消費後、回復を開始するまでの遅延時間。")]
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime; // 最後にエネルギーを消費した時間
    public Slider energySlider;
    private bool hasTriggeredEnergyDepletedEvent = false;

    // --- 内部状態と移動関連 ---
    private Vector3 velocity;
    private bool isAttacking = false;
    private float attackTimer = 0.0f;
    [Tooltip("攻撃中にプレイヤーが移動をロックされる時間。")]
    public float attackFixedDuration = 0.8f;

    // --- 近接攻撃関連の変数 ---
    [Header("Melee Attack Settings")]
    [Tooltip("近接攻撃のヒット判定の半径。")]
    public float meleeAttackRadius = 1.0f;

    [Tooltip("近接攻撃のクールダウン時間。")]
    public float meleeAttackCooldown = 0.5f;
    private float lastMeleeAttackTime = -Mathf.Infinity;
    private int currentMeleeCombo = 0;
    public int maxMeleeCombo = 5;
    public float comboResetTime = 1.0f;
    private float lastMeleeInputTime;
    public float autoLockOnMeleeRange = 5.0f;
    public bool preferLockedMeleeTarget = true;
    private Transform currentLockedMeleeTarget;
    public float meleeDashSpeed = 20.0f;
    public float meleeDashDistance = 2.0f;
    public float meleeDashDuration = 0.1f;
    public LayerMask enemyLayer;

    // --- ビーム攻撃関連の変数 ---
    [Header("Beam Attack Settings")]
    [Tooltip("ビーム攻撃の最大射程。")]
    public float beamAttackRange = 30.0f;

    [Tooltip("ビーム攻撃のクールダウン時間。")]
    public float beamCooldown = 0.5f;
    private float lastBeamAttackTime = -Mathf.Infinity;
    public GameObject beamEffectPrefab;
    public Transform beamSpawnPoint;
    public float beamWidth = 0.5f;
    public float beamDisplayDuration = 0.5f;
    public float autoLockOnRange = 40.0f;
    public bool preferLockedTarget = true;
    private Transform currentLockedBeamTarget;

    [Tooltip("プレイヤーが入力できるかどうかのフラグ。チュートリアルなどで使用。")]
    public bool canReceiveInput = true;

    // チュートリアル用イベント
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    public event Action onEnergyDepleted; // エネルギー枯渇時に発火するイベント

    // チュートリアル用タイマー
    private float _wasdMoveTimer = 0f;
    public float WASDMoveTimer { get { return _wasdMoveTimer; } }
    private float _jumpTimer = 0f;
    public float JumpTimer { get { return _jumpTimer; } }
    private float _descendTimer = 0f;
    public float DescendTimer { get { return _descendTimer; } }

    void Start()
    {
        InitializeComponents();
        // InitializeStats() は削除されたため、ここで currentEnergy の設定のみ行う
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
        CheckWarnings();
    }

    /// <summary>
    /// コンポーネントの初期化とエラーチェック
    /// </summary>
    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterControllerが見つかりません。このスクリプトはCharacterControllerが必要です。");
            enabled = false;
        }

        tpsCamController = FindObjectOfType<TPSCameraController>();
        if (tpsCamController == null)
        {
            // TPSCameraControllerは移動の計算に必要だが、見つからなくても処理を止めない
            Debug.LogWarning("PlayerController: TPSCameraControllerが見つかりません。カメラベースの移動方向計算ができません。");
        }
    }

    /*
    /// <summary>
    /// 現在の能力値をベース能力値で初期化 (Armor関連の削除に伴い、このメソッドは削除)
    /// </summary>
    private void InitializeStats()
    {
        // 削除
    }
    */

    /// <summary>
    /// 設定漏れの警告チェック
    /// </summary>
    private void CheckWarnings()
    {
        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("PlayerController: Beam Spawn Pointが設定されていません。Hierarchyに空のゲームオブジェクトを作成し、このフィールドにドラッグ＆ドロップしてください。");
        }
    }

    void Update()
    {
        // 攻撃中または入力無効化中は移動・攻撃入力をブロック
        if (!canReceiveInput || isAttacking)
        {
            HandleAttackState(); // 攻撃固定時間の更新は続ける
            // タイマーは更新しない
            _wasdMoveTimer = 0f;
            _jumpTimer = 0f;
            _descendTimer = 0f;
            return;
        }

        if (tpsCamController != null)
        {
            // 移動入力がある場合、カメラの方向に入力方向を合わせる
            tpsCamController.RotatePlayerToCameraDirection();
        }

        HandleAttackInputs();
        HandleEnergy();

        // 最終的な移動の適用
        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
        controller.Move(finalMove * Time.deltaTime);
    }

    /// <summary>
    /// 攻撃入力の処理 (Armor Mode変更の入力を削除)
    /// </summary>
    private void HandleAttackInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PerformMeleeAttack();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            PerformBeamAttack();
        }
    }

    /// <summary>
    /// 水平方向の移動処理
    /// </summary>
    private Vector3 HandleHorizontalMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = Vector3.zero;
        if (tpsCamController != null)
        {
            // カメラの水平方向の回転を取得し、それに基づいて移動方向を計算
            Quaternion cameraHorizontalRotation = Quaternion.Euler(0, tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraHorizontalRotation * (Vector3.right * horizontalInput + Vector3.forward * verticalInput);
        }
        else
        {
            moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        }
        moveDirection.Normalize();

        float currentSpeed = moveSpeed; // moveSpeed を直接使用
        bool isConsumingEnergy = false;

        // ブースト処理
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentEnergy > 0)
        {
            currentSpeed *= boostMultiplier; // boostMultiplier を直接使用
            currentEnergy -= energyConsumptionRate * Time.deltaTime; // energyConsumptionRate を直接使用
            isConsumingEnergy = true;
        }

        Vector3 horizontalMove = moveDirection * currentSpeed;

        // エネルギー枯渇時の速度制限
        if (currentEnergy <= 0.01f)
        {
            horizontalMove = horizontalMove.normalized * moveSpeed;
        }

        // チュートリアル用タイマー更新 - 移動している間だけ増加
        if (horizontalMove.magnitude > moveSpeed * 0.1f)
        {
            _wasdMoveTimer += Time.deltaTime;
        }
        else
        {
            _wasdMoveTimer = 0f;
        }

        // エネルギー消費情報を更新
        if (isConsumingEnergy)
        {
            lastEnergyConsumptionTime = Time.time;
        }

        return horizontalMove;
    }

    /// <summary>
    /// 垂直方向の移動処理と重力適用
    /// </summary>
    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = controller.isGrounded;
        bool isConsumingEnergy = false;

        // 接地している場合はY速度をリセット
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -0.5f; // 接地判定を確実にするために微小な下向き速度を設定
        }

        if (canFly)
        {
            if (Input.GetKey(KeyCode.Space) && currentEnergy > 0)
            {
                velocity.y = verticalSpeed; // verticalSpeed を直接使用
                currentEnergy -= energyConsumptionRate * Time.deltaTime; // energyConsumptionRate を直接使用
                isConsumingEnergy = true;
                _jumpTimer += Time.deltaTime; // 上昇タイマー更新
            }
            else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && currentEnergy > 0)
            {
                velocity.y = -verticalSpeed; // verticalSpeed を直接使用
                currentEnergy -= energyConsumptionRate * Time.deltaTime; // energyConsumptionRate を直接使用
                isConsumingEnergy = true;
                _descendTimer += Time.deltaTime; // 降下タイマー更新
            }
            else // スペース/Altが離されたらタイマーをリセット
            {
                if (Input.GetKeyUp(KeyCode.Space)) _jumpTimer = 0f;
                if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt)) _descendTimer = 0f;
            }
        }

        // 重力処理
        if (!isGrounded && !Input.GetKey(KeyCode.Space) && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // エネルギー枯渇時の垂直方向の制御を停止
        if (currentEnergy <= 0.01f && velocity.y > 0)
        {
            velocity.y = 0;
            _jumpTimer = 0f;
            _descendTimer = 0f;
        }

        // エネルギー消費情報を更新
        if (isConsumingEnergy)
        {
            lastEnergyConsumptionTime = Time.time;
        }

        return new Vector3(0, velocity.y, 0);
    }

    /// <summary>
    /// エネルギー回復と枯渇イベントの処理
    /// </summary>
    private void HandleEnergy()
    {
        // エネルギー回復（消費から遅延時間経過後）
        if (Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            currentEnergy += energyRecoveryRate * Time.deltaTime; // energyRecoveryRate を直接使用
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        // エネルギー枯渇イベントの発火
        if (currentEnergy <= 0.1f && !hasTriggeredEnergyDepletedEvent)
        {
            onEnergyDepleted?.Invoke();
            hasTriggeredEnergyDepletedEvent = true;
        }

        // エネルギーが回復し始めたらフラグをリセット
        if (currentEnergy > 0.1f && hasTriggeredEnergyDepletedEvent && Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            hasTriggeredEnergyDepletedEvent = false;
        }

        UpdateEnergyUI();
    }

    // 以下、既存のメソッドをそのまま残す（攻撃処理、ヘルパー、UI更新など）

    /// <summary>
    /// チュートリアル用の入力追跡フラグとタイマーをリセットする。
    /// </summary>
    public void ResetInputTracking()
    {
        _wasdMoveTimer = 0f;
        _jumpTimer = 0f;
        _descendTimer = 0f;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    Transform FindBeamTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnRange, enemyLayer);
        if (hitColliders.Length == 0) { return null; }
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;
        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }

    Transform FindMeleeTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnMeleeRange, enemyLayer);
        if (hitColliders.Length == 0) { return null; }
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;
        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }

    void PerformMeleeAttack()
    {
        if (Time.time < lastMeleeAttackTime + meleeAttackCooldown)
        {
            Debug.Log("Melee attack is on cooldown.");
            return;
        }

        lastMeleeAttackTime = Time.time;
        lastMeleeInputTime = Time.time; // コンボタイマーを更新

        // 攻撃中の固定開始
        isAttacking = true;
        attackTimer = 0.0f;

        // ターゲットを見つける
        currentLockedMeleeTarget = FindMeleeTarget();

        // ターゲットが存在し、かつ優先設定がされている場合、ターゲットの方向を向く
        if (currentLockedMeleeTarget != null && preferLockedMeleeTarget)
        {
            Vector3 lookAtTarget = currentLockedMeleeTarget.position;
            lookAtTarget.y = transform.position.y;
            transform.LookAt(lookAtTarget);
        }

        // 近接攻撃の突進コルーチンを開始
        StartCoroutine(MeleeDashAndAttack());

        // コンボ段階を進める
        currentMeleeCombo = (currentMeleeCombo % maxMeleeCombo) + 1; // 1からmaxMeleeComboまでをループ

        Debug.Log($"Melee Attack! Combo: {currentMeleeCombo}");
        onMeleeAttackPerformed?.Invoke(); // イベント発火
    }

    IEnumerator MeleeDashAndAttack()
    {
        Vector3 startPosition = transform.position;
        Vector3 dashTargetPosition;

        // ターゲットがいる場合はターゲットに向かって突進、いない場合はプレイヤーの正面に突進
        if (currentLockedMeleeTarget != null && preferLockedMeleeTarget)
        {
            // ターゲットとの距離が近すぎる場合は突進距離を短くする
            float distanceToTarget = Vector3.Distance(transform.position, currentLockedMeleeTarget.position);
            // meleeAttackRange を直接使用
            float actualDashDistance = Mathf.Min(meleeDashDistance, distanceToTarget - 0.5f); // ターゲットの手前0.5mで停止
            actualDashDistance = Mathf.Max(0, actualDashDistance); // 負にならないように

            dashTargetPosition = transform.position + transform.forward * actualDashDistance;
        }
        else
        {
            dashTargetPosition = transform.position + transform.forward * meleeDashDistance;
        }

        float dashStartTime = Time.time;
        while (Time.time < dashStartTime + meleeDashDuration)
        {
            float t = (Time.time - dashStartTime) / meleeDashDuration;
            // CharacterController.Move はワールド座標での移動量を期待するので、Lerpで位置を計算し、差分でMoveを呼ぶ
            Vector3 newPosition = Vector3.Lerp(startPosition, dashTargetPosition, t);
            controller.Move(newPosition - transform.position); // 現在位置との差分をMoveに渡す
            yield return null;
        }

        // 突進終了後、攻撃判定
        PerformMeleeDamageCheck();
    }

    void PerformMeleeDamageCheck()
    {
        // プレイヤーの現在の位置から前方にSphereCast
        // meleeAttackRange を直接使用
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, meleeAttackRadius, transform.forward, meleeAttackRange, enemyLayer);

        foreach (RaycastHit hit in hits)
        {
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hit.collider.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(meleeDamage); // meleeDamage を直接使用
                Debug.Log($"{hit.collider.name} に近接攻撃で {meleeDamage} ダメージを与えました。");
            }
        }
    }

    void PerformBeamAttack()
    {
        // beamAttackEnergyCost を直接使用
        if (Time.time < lastBeamAttackTime + beamCooldown)
        {
            Debug.Log("Beam attack is on cooldown.");
            return;
        }
        if (currentEnergy < beamAttackEnergyCost)
        {
            Debug.Log("Not enough energy for Beam Attack!");
            return;
        }

        currentEnergy -= beamAttackEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;

        // 攻撃中の固定開始
        isAttacking = true;
        attackTimer = 0.0f;

        // ターゲットを見つける
        currentLockedBeamTarget = FindBeamTarget();

        Vector3 startPoint = beamSpawnPoint.position;
        Vector3 endPoint;
        RaycastHit hitInfo;
        Transform hitEnemyTransform = null;

        if (currentLockedBeamTarget != null && preferLockedTarget)
        {
            // ロックオンターゲットの方向を向く
            Vector3 lookAtTarget = currentLockedBeamTarget.position;
            lookAtTarget.y = transform.position.y;
            transform.LookAt(lookAtTarget);

            // ロックオンしたターゲットに対してRaycastを飛ばす
            // beamAttackRange を直接使用
            if (Physics.Raycast(startPoint, (currentLockedBeamTarget.position - startPoint).normalized, out hitInfo, beamAttackRange, enemyLayer))
            {
                endPoint = hitInfo.point;
                hitEnemyTransform = hitInfo.collider.transform;
            }
            else
            {
                // ロックオンした敵にRayが届かない場合、最大射程まで伸ばす
                endPoint = startPoint + (currentLockedBeamTarget.position - startPoint).normalized * beamAttackRange;
            }
        }
        else
        {
            // カメラの中心からRayを飛ばし、ヒットした場所をターゲットにする
            Ray ray = tpsCamController.GetCameraRay();
            if (Physics.Raycast(ray, out hitInfo, beamAttackRange, enemyLayer))
            {
                endPoint = hitInfo.point;
                hitEnemyTransform = hitInfo.collider.transform;
            }
            else
            {
                endPoint = ray.origin + ray.direction * beamAttackRange;
            }
        }

        // ビームエフェクトとラインレンダラーの表示
        StartCoroutine(ShowBeamEffectAndLine(startPoint, endPoint, hitEnemyTransform));
        onBeamAttackPerformed?.Invoke(); // イベント発火
    }

    IEnumerator ShowBeamEffectAndLine(Vector3 startPoint, Vector3 endPoint, Transform hitEnemyTransform)
    {
        GameObject beamVisualizer = new GameObject("BeamVisualizer");

        // Line Renderer の設定
        LineRenderer lineRenderer = beamVisualizer.AddComponent<LineRenderer>();
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.blue;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        // ビームエフェクトの生成
        GameObject beamEffectInstance = null;
        if (beamEffectPrefab != null)
        {
            beamEffectInstance = Instantiate(beamEffectPrefab, startPoint, Quaternion.identity);
            beamEffectInstance.transform.LookAt(endPoint);
            beamEffectInstance.transform.SetParent(beamVisualizer.transform);
        }

        // 敵にダメージを与える
        if (hitEnemyTransform != null)
        {
            EnemyHealth enemyHealth = hitEnemyTransform.GetComponent<EnemyHealth>() ?? hitEnemyTransform.GetComponentInChildren<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(beamDamage); // beamDamage を直接使用
                Debug.Log($"{hitEnemyTransform.name} にビーム攻撃で {beamDamage} ダメージを与えました。");
            }
        }

        // 指定時間表示した後、破棄
        yield return new WaitForSeconds(beamDisplayDuration);

        Destroy(beamVisualizer);
    }

    // 攻撃中のプレイヤーの状態を処理
    void HandleAttackState()
    {
        if (isAttacking)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackFixedDuration)
            {
                isAttacking = false;
                attackTimer = 0.0f;
            }
            // 攻撃固定中はvelocity.yをリセット
            velocity.y = 0;
        }
    }

    /*
    // 武器装備に関するすべてのフィールドとメソッドを削除
    public void EquipWeapons(WeaponData primary, WeaponData secondary)
    {
        // 削除
    }
    */
}