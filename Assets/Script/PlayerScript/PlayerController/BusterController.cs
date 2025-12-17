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

    // ★硬直設定
    [Header("Hardening (Stun) Settings")]
    [Tooltip("攻撃による硬直時間。この間、移動や他の攻撃はできません。")]
    public float attackFixedDuration = 0.8f;
    [Tooltip("着地時の硬直時間。この間、移動や攻撃はできません。")]
    public float landStunDuration = 0.2f;

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
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;

    [Header("VFX & Layers")]
    public BeamController beamPrefab;

    [Tooltip("ミニガン用発射点 (2箇所)")]
    public Transform[] minigunFirePoints;

    [Tooltip("レールガン用発射点 (2箇所)")]
    public Transform[] railgunFirePoints;

    [Tooltip("ミニガン連射間隔")]
    public float minigunFireRate = 0.1f;

    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // === プライベート/キャッシュ変数 ===
    private float _currentHP;
    private float _currentEnergy;

    // ★硬直/攻撃制御フラグ
    private bool _isAttacking = false; // 攻撃アニメーションによる硬直 (内部状態)
    private bool _isStunned = false; // 着地または攻撃による全操作の硬直
    private float _stunTimer = 0.0f; // 硬直タイマー

    private float _lastEnergyConsumptionTime;
    private bool _hasTriggeredEnergyDepletedEvent = false;
    private bool _isDead = false;

    private float _lastMinigunFireTime = -1f;

    private Vector3 _velocity;
    private float _moveSpeed; // 派生した移動速度
    private bool _wasGrounded = false; // 前フレームの接地状態（着地硬直判定に必須）

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

        // 1. 今フレームの接地状態をキャッシュ
        bool isGroundedNow = _playerController.isGrounded;

        // 2. ★硬直状態の処理 (最優先: タイマー減少と解除)
        HandleStunState();

        // 3. ★硬直中の移動・入力をキャンセル
        if (_isStunned)
        {
            // 硬直中の重力処理
            if (!isGroundedNow)
            {
                // 硬直中の空中では重力のみ適用
                float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
                _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
            }
            else
            {
                // 接地している場合はY速度をリセット
                _velocity.y = -0.1f;
            }

            // 硬直中はY軸移動のみ実行 (X, Z移動はVector3.zeroを返すことで停止)
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);

            // ★重要: 次フレームのために接地状態を更新して終了
            _wasGrounded = isGroundedNow;
            return;
        }

        // 4. プレイヤーの向き制御
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        // 5. ステータスとエネルギー管理
        ApplyArmorStats();
        HandleEnergy();

        // 6. 入力と移動
        HandleInput();

        // Minigunの連射処理 (硬直/攻撃中でない場合のみ)
        if (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Beam &&
            Input.GetMouseButton(0) && !_isAttacking)
        {
            HandleBeamAttack(true); // true = Minigunモード
        }

        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();

        // 親のCharacterControllerを動かす
        _playerController.Move(finalMove * Time.deltaTime);

        // 7. 接地状態の更新 (次フレームのために保存)
        _wasGrounded = isGroundedNow;
    }

    // =======================================================
    // 硬直 (Stun) 制御関数
    // =======================================================

    /// <summary>
    /// 着地硬直を開始し、タイマーを設定します。
    /// </summary>
    public void StartLandingStun()
    {
        // 既に硬直中の場合は上書きしない
        if (_isStunned) return;

        Debug.Log("着地硬直開始");
        _isStunned = true;
        _stunTimer = landStunDuration;
        // 着地時の速度をリセット（不自然なスライドを防ぐ）
        _velocity = Vector3.zero;
    }

    /// <summary>
    /// 攻撃による硬直を開始し、タイマーを設定します。
    /// </summary>
    public void StartAttackStun()
    {
        // 既に硬直中の場合は上書きしない
        if (_isStunned) return;

        Debug.Log("攻撃硬直開始");
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = attackFixedDuration;

        // 攻撃硬直中はY軸移動を一時停止
        _velocity.y = 0f;
    }

    /// <summary>
    /// 硬直状態を処理し、タイマーが切れたら硬直を解除します。
    /// </summary>
    private void HandleStunState()
    {
        if (!_isStunned) return;

        _stunTimer -= Time.deltaTime;

        if (_stunTimer <= 0.0f)
        {
            // 硬直解除
            _isStunned = false;
            _isAttacking = false; // 攻撃由来の硬直もここで解除

            // 接地状態であれば、Y軸速度をリセット
            if (_playerController.isGrounded)
            {
                _velocity.y = -0.1f;
            }
            Debug.Log("硬直解除");
        }
    }


    // =======================================================
    // Movement Logic
    // =======================================================

    private Vector3 HandleHorizontalMovement()
    {
        // 硬直中は移動を停止
        if (_isStunned) return Vector3.zero;

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
        bool isGrounded = _playerController.isGrounded;

        // 着地硬直の判定
        // 前フレーム非接地 && 今フレーム接地 && 落下速度が一定以上 && 硬直中でない
        if (!_wasGrounded && isGrounded && _velocity.y < -0.1f && !_isStunned)
        {
            StartLandingStun();
            // 硬直が始まったフレームでは移動ベクトルをゼロとする
            return Vector3.zero;
        }

        // 接地しているが、落下速度が残っている場合 
        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = -0.1f;
        }

        // 硬直中は Y 軸移動を停止する。
        if (_isStunned)
        {
            return Vector3.zero;
        }

        // ... (以降、上昇/下降の入力処理) ...
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
    // Input Handling (Old Input System/Keyboard Fallbacks)
    // =======================================================

    private void HandleInput()
    {
        // 硬直中は攻撃/武器切り替え入力を無視
        if (_isStunned) return;

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
        // 硬直中は攻撃入力を無視
        if (_isAttacking || _isStunned) return;

        // マウス左クリックの処理
        if (Input.GetMouseButtonDown(0))
        {
            switch (_modesAndVisuals.CurrentWeaponMode)
            {
                case PlayerModesAndVisuals.WeaponMode.Melee:
                    HandleMeleeAttack();
                    break;
                case PlayerModesAndVisuals.WeaponMode.Beam:
                    // Railgun（単発）モードだと仮定
                    HandleBeamAttack(false);
                    break;
            }
        }
    }

    // =======================================================
    // Attack Logic (Melee / Multiple Beam)
    // =======================================================

    private void HandleMeleeAttack()
    {
        // 攻撃開始時に硬直を開始
        StartAttackStun();

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
        // Railgunは硬直中は発射しない
        if (_isStunned && !isMinigunMode) return;

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

        float cost = beamAttackEnergyCost * (isMinigunMode ? 0.5f : 1.0f);

        if (!ConsumeEnergy(cost * firePoints.Length))
        {
            return;
        }

        // Railgunは攻撃アニメーションを伴う（単発固定時間）
        if (!isMinigunMode)
        {
            // Railgunは攻撃開始時に硬直を開始
            StartAttackStun();
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
            // ~0 は全てのレイヤーに対してRaycastを実行することを意味する (LayerMaskに依存しない)
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

            if (didHit)
            {
                endPoint = hit.point;
                // ダメージ適用
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


    /// <summary>
    /// 衝突したColliderから、該当する敵コンポーネントを探してダメージを与える。
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 共通インターフェースを使わず、具体的な敵クラスをチェックする

        // 1. SoldierMoveEnemy がターゲットか確認
        if (target.TryGetComponent<SoldierMoveEnemy>(out var soldierMoveEnemy))
        {
            soldierMoveEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        // 2. SoliderEnemy がターゲットか確認
        else if (target.TryGetComponent<SoliderEnemy>(out var soliderEnemy))
        {
            soliderEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        // 3. TutorialEnemyController がターゲットか確認
        else if (target.TryGetComponent<TutorialEnemyController>(out var tutorialEnemy))
        {
            tutorialEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        // 4. ScorpionEnemy がターゲットか確認
        else if (target.TryGetComponent<ScorpionEnemy>(out var scorpion))
        {
            scorpion.TakeDamage(damageAmount);
            isHit = true;
        }
        // 5. SuicideEnemy がターゲットか確認
        else if (target.TryGetComponent<SuicideEnemy>(out var suicide))
        {
            suicide.TakeDamage(damageAmount);
            isHit = true;
        }
        // 6. DroneEnemy がターゲットか確認
        else if (target.TryGetComponent<DroneEnemy>(out var drone))
        {
            drone.TakeDamage(damageAmount);
            isHit = true;
        }

        if (isHit && hitEffectPrefab != null)
        {
            // 命中位置が不明な近接攻撃/ビーム攻撃用に当たり判定の中心を使用
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
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
        if (_isDead || _isStunned) return; // 硬直中入力無視
        _verticalInput = context.performed ? 1f : 0f;
    }

    // Bボタン (下降) - Action名: FlyDown
    public void OnFlyDown(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return; // 硬直中入力無視
        _verticalInput = context.performed ? -1f : 0f;
    }

    // Right Button/RB (加速/ブースト) - Action名: Boost
    public void OnBoost(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return; // 硬直中入力無視
        _isBoosting = context.performed;
    }

    // Right Trigger/RT (攻撃) - Action名: Attack
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return; // 硬直中入力無視

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
        if (_isDead || _isStunned) return; // 硬直中入力無視
        if (context.started)
        {
            _modesAndVisuals.SwitchWeapon();
        }
    }

    // DPad (Armor Switch)
    public void OnDPad(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return; // 硬直中入力無視
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
        if (_isDead || _isStunned) return; // 硬直中入力無視
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