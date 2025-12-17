using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BusterController : MonoBehaviour
{
    // =======================================================
    // Dependencies
    // =======================================================

    [Header("Dependencies")]
    private CharacterController _playerController; // 親オブジェクトのCharacterControllerを格納
    private TPSCameraController _tpsCamController;
    public PlayerModesAndVisuals _modesAndVisuals; // モード/ビジュアルコンポーネント

    // =======================================================
    // Game/Stats Settings
    // =======================================================

    [Header("Game Over Settings")]
    public SceneBasedGameOverManager gameOverManager;

    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
    public float verticalSpeed = 10.0f;
    public float energyConsumptionRate = 15.0f;
    public float gravity = -9.81f;
    public bool canFly = true;

    [Tooltip("標準重力に対する落下速度の乗数")]
    public float fastFallMultiplier = 3.0f;

    [Header("Health Settings")]
    public float maxHP = 10000.0f;
    public Slider hPSlider;
    public Text hPText;

    [Header("Energy Gauge Settings")]
    public float maxEnergy = 1000.0f;
    public float energyRecoveryRate = 10.0f;
    public float recoveryDelay = 1.0f;
    public Slider energySlider;

    // =======================================================
    // Attack Settings (Multiple Beam Fire Points)
    // =======================================================

    [Header("Attack Settings")]
    public float attackFixedDuration = 0.8f;
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;

    [Header("VFX & Layers")]
    public BeamController beamPrefab;

    // ★修正: 複数の発射点を格納するための配列
    [Tooltip("ミニガン用発射点 (2箇所)")]
    public Transform[] minigunFirePoints;

    [Tooltip("レールガン用発射点 (2箇所)")]
    public Transform[] railgunFirePoints;

    // ★追加: ミニガン連射間隔
    [Tooltip("ミニガン連射間隔")]
    public float minigunFireRate = 0.1f;

    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // === プライベート/キャッシュ変数 ===
    private float _currentHP;
    private float _currentEnergy;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private float _lastEnergyConsumptionTime;
    private bool _hasTriggeredEnergyDepletedEvent = false;
    private bool _isDead = false;

    private float _lastMinigunFireTime = -1f; // ミニガン連射タイマー

    private Vector3 _velocity;
    private float _moveSpeed; // 派生した移動速度

    // === コントローラー入力のための追加変数 ===
    private bool _isBoosting = false;
    private float _verticalInput = 0f;

    // =======================================================
    // Unity Lifecycle
    // =======================================================

    void Awake()
    {
        InitializeComponents();
        _currentEnergy = maxEnergy;
        _currentHP = maxHP;
    }

    void Start()
    {
        InitializeGameOverManager();
        UpdateAllUI();
    }

    void Update()
    {
        if (_isDead) return;

        // 攻撃状態中の処理 (移動のロック)
        if (_isAttacking)
        {
            HandleAttackState();
            // 親のCharacterControllerのisGroundedを使用し、重力を適用
            if (!_playerController.isGrounded)
            {
                _velocity.y += gravity * Time.deltaTime;
            }
            // 親のCharacterControllerを動かす
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            return;
        }

        // プレイヤーの向き制御
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        ApplyArmorStats();
        HandleEnergy();

        HandleInput(); // 古いInput向け処理 (主に攻撃と武器切り替えを処理)

        // ★修正: Minigunの連射はUpdateで処理する
        if (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Beam &&
            Input.GetMouseButton(0) && !_isAttacking)
        {
            // 仮定: 現在のビームモードがMinigunであるかどうかのフラグが必要
            // ここではMinigunモードであると仮定して連射ロジックを呼び出す
            // (実際には_modesAndVisualsにフラグが必要です)
            HandleBeamAttack(true); // true = Minigunモード
        }

        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();

        // 親のCharacterControllerを動かす
        _playerController.Move(finalMove * Time.deltaTime);
    }

    // =======================================================
    // Initialization / Setup
    // =======================================================

    private void InitializeComponents()
    {
        // 親オブジェクトのCharacterControllerを取得
        _playerController = GetComponentInParent<CharacterController>();

        if (_playerController == null)
        {
            Debug.LogError($"{nameof(BusterController)}: 必要なCharacterControllerが見つかりません。親オブジェクト (Player) にアタッチしてください。");
            enabled = false;
            return;
        }

        _tpsCamController = FindObjectOfType<TPSCameraController>();
    }

    private void InitializeGameOverManager()
    {
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();
            if (gameOverManager == null)
            {
                Debug.LogWarning($"{nameof(SceneBasedGameOverManager)}が見つかりません。");
            }
        }
    }

    private void ApplyArmorStats()
    {
        // アーマーの移動速度補正を適用
        var stats = _modesAndVisuals.CurrentArmorStats;
        if (stats != null)
        {
            _moveSpeed = baseMoveSpeed * stats.moveSpeedMultiplier;
        }
        else
        {
            _moveSpeed = baseMoveSpeed;
        }
    }

    // =======================================================
    // Energy / HP Management
    // =======================================================

    private void HandleEnergy()
    {
        // エネルギー回復
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            var stats = _modesAndVisuals.CurrentArmorStats;
            float recoveryMultiplier = stats != null ? stats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            _currentEnergy += recoveryRate * Time.deltaTime;
        }

        _currentEnergy = Mathf.Clamp(_currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();

        // エネルギー枯渇イベントの管理 (省略)
        if (_currentEnergy <= 0.1f && !_hasTriggeredEnergyDepletedEvent)
        {
            _hasTriggeredEnergyDepletedEvent = true;
        }
        else if (_currentEnergy > 0.1f && _hasTriggeredEnergyDepletedEvent && Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            _hasTriggeredEnergyDepletedEvent = false;
        }
    }

    public bool ConsumeEnergy(float amount)
    {
        if (_currentEnergy >= amount)
        {
            _currentEnergy -= amount;
            _lastEnergyConsumptionTime = Time.time;
            UpdateEnergyUI();
            return true;
        }
        return false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        float finalDamage = damageAmount;
        var stats = _modesAndVisuals.CurrentArmorStats;

        if (stats != null)
        {
            // 防御補正を適用
            finalDamage *= stats.defenseMultiplier;
        }

        _currentHP -= finalDamage;
        _currentHP = Mathf.Clamp(_currentHP, 0, maxHP);
        UpdateHPUI();

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        if (gameOverManager != null)
        {
            gameOverManager.GoToGameOverScene();
        }
        else
        {
            Debug.LogError("SceneBasedGameOverManagerが設定されていません。");
        }

        enabled = false;
    }

    // =======================================================
    // Input Handling (Old Input System/Keyboard Fallbacks)
    // =======================================================

    private void HandleInput()
    {
        HandleAttackInputs();
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();
    }

    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Speed);
    }

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            _modesAndVisuals.SwitchWeapon();
        }
    }

    private void HandleAttackInputs()
    {
        if (_isAttacking) return;

        // マウス左クリックの処理
        if (Input.GetMouseButtonDown(0))
        {
            switch (_modesAndVisuals.CurrentWeaponMode)
            {
                case PlayerModesAndVisuals.WeaponMode.Melee:
                    HandleMeleeAttack();
                    break;
                case PlayerModesAndVisuals.WeaponMode.Beam:
                    // レールガン（単発）モードだと仮定し、HandleBeamAttack(false) を呼び出す
                    // MinigunはUpdate()のGetMouseButton(0)で処理されます。
                    HandleBeamAttack(false);
                    break;
            }
        }
    }

    // =======================================================
    // Movement Logic
    // =======================================================

    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxis("Horizontal"); // 左スティックX
        float v = Input.GetAxis("Vertical");// 左スティックY

        if (h == 0f && v == 0f)
        {
            return Vector3.zero;
        }

        Vector3 inputDirection = new Vector3(h, 0, v);
        Vector3 moveDirection;

        if (_tpsCamController != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraRotation * inputDirection;
        }
        else
        {
            moveDirection = transform.right * h + transform.forward * v;
        }

        moveDirection.Normalize();

        float currentSpeed = _moveSpeed;

        // ダッシュ処理
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || _isBoosting) && _currentEnergy > 0.01f;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            ConsumeEnergy(energyConsumptionRate * Time.deltaTime);
        }

        return moveDirection * currentSpeed;
    }

    private Vector3 HandleVerticalMovement()
    {
        // 親のCharacterControllerのisGroundedを使用
        bool isGrounded = _playerController.isGrounded;
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool hasVerticalInput = false;

        // キーボード入力: Space (上昇), Alt (下降)
        bool isFlyingUpKey = Input.GetKey(KeyCode.Space);
        bool isFlyingDownKey = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // Input System入力: _verticalInput
        bool isFlyingUpController = _verticalInput > 0.5f;
        bool isFlyingDownController = _verticalInput < -0.5f;


        if (canFly && _currentEnergy > 0.01f)
        {
            if (isFlyingUpKey || isFlyingUpController) // 上昇
            {
                _velocity.y = verticalSpeed;
                hasVerticalInput = true;
            }
            else if (isFlyingDownKey || isFlyingDownController) // 降下
            {
                _velocity.y = -verticalSpeed;
                hasVerticalInput = true;
            }
        }

        if (!hasVerticalInput)
        {
            if (!isGrounded)
            {
                // 降下速度を速くするため、重力に fastFallMultiplier を適用
                float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
                _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
            }
        }
        else
        {
            // 上昇または降下でエネルギーを消費
            ConsumeEnergy(energyConsumptionRate * Time.deltaTime);
        }

        // エネルギー切れで上昇を止める
        if (_currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    // =======================================================
    // Attack Logic (Melee / Multiple Beam)
    // =======================================================

    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        Transform lockOnTarget = _tpsCamController != null ? _tpsCamController.LockOnTarget : null;

        if (lockOnTarget != null)
        {
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget);
            RotateTowards(targetPosition);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;
            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    /// <summary>ミニガンまたはレールガンとしてビーム攻撃を処理します。</summary>
    /// <param name="isMinigunMode">trueの場合ミニガン（連射）、falseの場合レールガン（単発）として処理。</param>
    private void HandleBeamAttack(bool isMinigunMode)
    {
        if (_isAttacking && !isMinigunMode) return; // Railgunは攻撃アニメーション中は発射しない

        // Minigun連射制御
        if (isMinigunMode)
        {
            if (Time.time < _lastMinigunFireTime + minigunFireRate)
            {
                return; // 冷却中
            }
        }

        // Minigun/Railgunでの処理分岐
        Transform[] firePoints = isMinigunMode ? minigunFirePoints : railgunFirePoints;

        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogError($"{(isMinigunMode ? "Minigun" : "Railgun")} の発射点が設定されていません。");
            return;
        }

        float cost = beamAttackEnergyCost * (isMinigunMode ? 0.5f : 1.0f); // Minigunはコストを抑える

        if (!ConsumeEnergy(cost * firePoints.Length)) // 全ての発射点からの合計コストを消費
        {
            // Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        // Railgunは攻撃アニメーションを伴う（単発固定時間）
        if (!isMinigunMode)
        {
            _isAttacking = true;
            _attackTimer = 0f;
            _velocity.y = 0f;
        }

        // 複数の発射点からビームを発射
        foreach (var firePoint in firePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection;
            Transform lockOnTarget = _tpsCamController?.LockOnTarget;

            // ターゲットの決定
            if (lockOnTarget != null)
            {
                Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                fireDirection = (targetPosition - origin).normalized;
                RotateTowards(targetPosition);
            }
            else
            {
                fireDirection = firePoint.forward;
            }

            RaycastHit hit;
            Vector3 endPoint;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

            if (didHit)
            {
                endPoint = hit.point;
                // ダメージ適用 (Minigunはダメージを低くする)
                ApplyDamageToEnemy(hit.collider, beamDamage * (isMinigunMode ? 0.5f : 1.0f));
            }
            else
            {
                endPoint = origin + fireDirection * beamMaxDistance;
            }

            // ビームの視覚効果を生成
            BeamController beamInstance = Instantiate(
                beamPrefab,
                origin,
                Quaternion.LookRotation(fireDirection)
            );
            // BeamControllerのFire関数を呼び出す
            beamInstance.Fire(origin, endPoint, didHit);
        }

        if (isMinigunMode)
        {
            _lastMinigunFireTime = Time.time;
        }
    }


    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 敵コンポーネントへの依存（略）
        // if (target.TryGetComponent<EnemyHealth>(out var health))
        // {
        //     health.TakeDamage(damageAmount);
        //     isHit = true;
        // }
        // 仮に常にヒットしたとします
        isHit = true;

        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
        }
    }

    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffsetIfNoCollider = false)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }
        else if (useOffsetIfNoCollider)
        {
            return target.position + Vector3.up * lockOnTargetHeightOffset;
        }
        return target.position;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        transform.rotation = targetRotation;
    }

    void HandleAttackState()
    {
        if (!_isAttacking) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            // 攻撃終了後の挙動リセット
            if (!_playerController.isGrounded)
            {
                _velocity.y = 0; // 空中攻撃後の勢いをリセット
            }
            else
            {
                _velocity.y = -0.1f; // 地面に着地状態を強制
            }
        }
    }

    // =======================================================
    // UI Updates
    // =======================================================
    private void UpdateAllUI()
    {
        UpdateHPUI();
        UpdateEnergyUI();
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = _currentEnergy / maxEnergy;
        }
    }

    void UpdateHPUI()
    {
        if (hPSlider != null)
        {
            hPSlider.value = _currentHP / maxHP;
        }

        if (hPText != null)
        {
            int currentHPInt = Mathf.CeilToInt(_currentHP);
            int maxHPInt = Mathf.CeilToInt(maxHP);

            hPText.text = $"{currentHPInt} / {maxHPInt}";
        }
    }


    // =======================================================
    // Input System Event Handlers
    // =======================================================

    // Aボタン (上昇) - Action名: FlyUp
    public void OnFlyUp(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        _verticalInput = context.performed ? 1f : 0f;
    }

    // Bボタン (下降) - Action名: FlyDown
    public void OnFlyDown(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        _verticalInput = context.performed ? -1f : 0f;
    }

    // Right Button/RB (加速/ブースト) - Action名: Boost
    public void OnBoost(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        _isBoosting = context.performed;
    }

    // Right Trigger/RT (攻撃) - Action名: Attack
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (_isDead) return;

        // Input Systemの場合、MinigunはPerformed（長押し）で連射させるのが理想ですが、
        // Minigunの連射ロジックはUpdate/GetMouseButton(0)に任せます。
        // ここでは、単発のMeleeまたはRailgunの "開始" のみを処理します。
        if (context.started && !_isAttacking)
        {
            switch (_modesAndVisuals.CurrentWeaponMode)
            {
                case PlayerModesAndVisuals.WeaponMode.Melee:
                    HandleMeleeAttack();
                    break;
                case PlayerModesAndVisuals.WeaponMode.Beam:
                    // Railgunモードの発射
                    HandleBeamAttack(false);
                    break;
            }
        }
    }

    // Yボタン (武装切替) - Action名: WeaponSwitch
    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (context.started)
        {
            _modesAndVisuals.SwitchWeapon();
        }
    }

    // DPad (Armor Switch)
    public void OnDPad(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (context.started)
        {
            Vector2 input = context.ReadValue<Vector2>();

            if (input.x > 0.5f)
            {
                _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Buster);
            }
            else if (input.y > 0.5f)
            {
                _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Normal);
            }
            else if (input.x < -0.5f)
            {
                _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Speed);
            }
        }
    }

    // Menuボタン (設定画面) - Action名: Menu
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (context.started)
        {
            Debug.Log("メニューボタンが押されました: 設定画面を開く");
        }
    }


    // =======================================================
    // Gizmos (Debug Visuals)
    // =======================================================

    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程 (MinigunとRailgunの発射点をすべて描画)
        DrawBeamGizmos(minigunFirePoints, Color.cyan);
        DrawBeamGizmos(railgunFirePoints, Color.magenta);
    }

    private void DrawBeamGizmos(Transform[] firePoints, Color baseColor)
    {
        if (firePoints == null) return;

        foreach (var beamFirePoint in firePoints)
        {
            if (beamFirePoint == null) continue;

            Vector3 origin = beamFirePoint.position;
            Vector3 fireDirection = beamFirePoint.forward;

            Transform lockOnTarget = _tpsCamController != null ? _tpsCamController.LockOnTarget : null;

            if (lockOnTarget != null)
            {
                Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                fireDirection = (targetPosition - origin).normalized;
            }

            RaycastHit hit;
            Vector3 endPoint;

            if (Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0))
            {
                Gizmos.color = Color.red; // ヒット時は赤
                endPoint = hit.point;
                Gizmos.DrawSphere(endPoint, 0.1f);
            }
            else
            {
                Gizmos.color = baseColor; // ベースの色
                endPoint = origin + fireDirection * beamMaxDistance;
            }
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin + fireDirection * beamMaxDistance, 0.05f);
        }
    }
}