using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpeedController : MonoBehaviour
{
    // =======================================================
    // Dependencies
    // =======================================================
    [Header("Dependencies")]
    private CharacterController _playerController;
    private TPSCameraController _tpsCamController;
    public PlayerModesAndVisuals _modesAndVisuals;

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

    [Header("Attack Settings")]
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;

    [Header("VFX & Layers")]
    public BeamController beamPrefab;
    [Tooltip("ビームを発射するポイント (複数設定可能)")]
    public Transform[] beamFirePoints;
    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // === プライベート/キャッシュ変数 ===
    private float _currentHP;
    private float _currentEnergy;
    private bool _isAttacking = false;
    private bool _isStunned = false;
    private float _stunTimer = 0.0f;
    private float _lastEnergyConsumptionTime;
    private bool _hasTriggeredEnergyDepletedEvent = false;
    private bool _isDead = false;
    private Vector3 _velocity;
    private float _moveSpeed;
    private bool _wasGrounded = false;
    private bool _isBoosting = false;
    private float _verticalInput = 0f;

    // =======================================================
    // Unity Lifecycle
    // =======================================================
    void Awake()
    {
        _playerController = GetComponentInParent<CharacterController>();
        _tpsCamController = FindObjectOfType<TPSCameraController>();

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

        bool isGroundedNow = _playerController.isGrounded;

        HandleStunState(isGroundedNow);

        if (_isStunned)
        {
            HandleStunnedVerticalMovement(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // プレイヤーの向き制御
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        // ステータスとエネルギー管理
        ApplyArmorStats();
        HandleEnergy();

        // 入力と移動
        HandleInput();
        Vector3 finalMove = HandleVerticalMovement(isGroundedNow) + HandleHorizontalMovement();

        _playerController.Move(finalMove * Time.deltaTime);

        _wasGrounded = isGroundedNow;
    }


    // =======================================================
    // Stun / Movement Logic
    // =======================================================

    private void HandleStunnedVerticalMovement(bool isGroundedNow)
    {
        if (!isGroundedNow)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }
        else
        {
            _velocity.y = -0.1f;
        }
    }

    /// <summary>着地硬直を開始</summary>
    public void StartLandingStun()
    {
        if (_isStunned) return;
        Debug.Log("着地硬直開始");
        _isStunned = true;
        _stunTimer = landStunDuration;
        _velocity = Vector3.zero;
    }

    /// <summary>攻撃硬直を開始</summary>
    public void StartAttackStun()
    {
        if (_isStunned) return;
        Debug.Log("攻撃硬直開始");
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = attackFixedDuration;
        _velocity.y = 0f;
    }

    private void HandleStunState(bool isGroundedNow)
    {
        if (!_isStunned)
        {
            // 着地硬直判定
            if (!_wasGrounded && isGroundedNow && _velocity.y < -0.1f)
            {
                StartLandingStun();
            }
            return;
        }

        // タイマー処理
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0.0f)
        {
            _isStunned = false;
            _isAttacking = false;
            if (isGroundedNow)
            {
                _velocity.y = -0.1f;
            }
            Debug.Log("硬直解除");
        }
    }

    private Vector3 HandleHorizontalMovement()
    {
        if (_isStunned) return Vector3.zero;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (h == 0f && v == 0f) return Vector3.zero;

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
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || _isBoosting) && _currentEnergy > 0.01f;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            ConsumeEnergy(energyConsumptionRate * Time.deltaTime);
        }

        return moveDirection * currentSpeed;
    }

    private Vector3 HandleVerticalMovement(bool isGrounded)
    {
        if (_isStunned) return Vector3.zero; // 硬直中は移動停止

        // 接地している場合はY速度をリセット
        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = -0.1f;
        }

        bool isFlyingUp = Input.GetKey(KeyCode.Space) || _verticalInput > 0.5f;
        bool isFlyingDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || _verticalInput < -0.5f;
        bool hasVerticalInput = false;

        if (canFly && _currentEnergy > 0.01f)
        {
            if (isFlyingUp)
            {
                _velocity.y = verticalSpeed;
                hasVerticalInput = true;
            }
            else if (isFlyingDown)
            {
                _velocity.y = -verticalSpeed;
                hasVerticalInput = true;
            }
        }

        if (hasVerticalInput)
        {
            ConsumeEnergy(energyConsumptionRate * Time.deltaTime);
        }
        else if (!isGrounded)
        {
            // 重力適用 (落下中は fastFallMultiplier を適用)
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }

        // エネルギー切れで上昇を止める
        if (_currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    // =======================================================
    // Input Handling
    // =======================================================
    private void HandleInput()
    {
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
        if (_isAttacking || _isStunned) return;

        if (Input.GetMouseButtonDown(0))
        {
            PerformAttack();
        }
    }

    // =======================================================
    // Attack Logic
    // =======================================================
    private void PerformAttack()
    {
        switch (_modesAndVisuals.CurrentWeaponMode)
        {
            case PlayerModesAndVisuals.WeaponMode.Melee:
                HandleMeleeAttack();
                break;
            case PlayerModesAndVisuals.WeaponMode.Beam:
                HandleBeamAttack();
                break;
        }
    }

    private void HandleMeleeAttack()
    {
        StartAttackStun();

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;

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

    private void HandleBeamAttack()
    {
        if (beamFirePoints == null || beamFirePoints.Length == 0)
        {
            Debug.LogError("ビームの発射点 (`beamFirePoints`) が設定されていません。");
            return;
        }

        float totalCost = beamAttackEnergyCost * beamFirePoints.Length;
        if (!ConsumeEnergy(totalCost)) return;

        if (beamPrefab == null)
        {
            Debug.LogError("ビームのプレハブが設定されていません。");
            return;
        }

        StartAttackStun();

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;

        if (lockOnTarget != null)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            RotateTowards(targetPosition);
        }

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection = (lockOnTarget != null)
                ? (targetPosition - origin).normalized
                : firePoint.forward;

            RaycastHit hit;
            Vector3 endPoint;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

            if (didHit)
            {
                endPoint = hit.point;
                ApplyDamageToEnemy(hit.collider, beamDamage);
            }
            else
            {
                endPoint = origin + fireDirection * beamMaxDistance;
            }

            // ビームの生成
            BeamController beamInstance = Instantiate(
                beamPrefab,
                origin,
                Quaternion.LookRotation(fireDirection)
            );
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    // =======================================================
    // Damage/Targeting
    // =======================================================
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // ★敵コンポーネントがTakeDamageを持つことを前提とする
        if (target.TryGetComponent<SoldierMoveEnemy>(out var c1)) { c1.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SoliderEnemy>(out var c2)) { c2.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<TutorialEnemyController>(out var c3)) { c3.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var c4)) { c4.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var c5)) { c5.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var c6)) { c6.TakeDamage(damageAmount); isHit = true; }

        if (isHit)
        {
            Debug.Log($"Enemy hit! Target: {target.name}, Damage: {damageAmount}");
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
            }
        }
    }

    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffsetIfNoCollider = false)
    {
        if (target.TryGetComponent<Collider>(out var targetCollider))
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
    // Initialization / Stats / Energy / HP
    // =======================================================
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
        var stats = _modesAndVisuals.CurrentArmorStats;
        _moveSpeed = (stats != null) ? baseMoveSpeed * stats.moveSpeedMultiplier : baseMoveSpeed;
    }

    private void HandleEnergy()
    {
        // エネルギー回復
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            var stats = _modesAndVisuals.CurrentArmorStats;
            float recoveryMultiplier = (stats != null) ? stats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            _currentEnergy += recoveryRate * Time.deltaTime;
        }

        _currentEnergy = Mathf.Clamp(_currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();

        // エネルギー枯渇イベントの管理
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
            finalDamage *= stats.defenseMultiplier;
        }

        _currentHP -= finalDamage;
        _currentHP = Mathf.Clamp(_currentHP, 0, maxHP);
        UpdateHPUI();

        if (_currentHP <= 0) Die();
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
    // Input System Event Handlers (Controller)
    // =======================================================
    public void OnFlyUp(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        _verticalInput = context.performed ? 1f : 0f;
    }

    public void OnFlyDown(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        _verticalInput = context.performed ? -1f : 0f;
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        _isBoosting = context.performed;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        if (context.started && !_isAttacking) PerformAttack();
    }

    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        if (context.started) _modesAndVisuals.SwitchWeapon();
    }

    public void OnDPad(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned || !context.started) return;
        Vector2 input = context.ReadValue<Vector2>();

        if (input.x > 0.5f) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Buster);
        else if (input.y > 0.5f) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Normal);
        else if (input.x < -0.5f) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Speed);
    }

    public void OnMenu(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        if (context.started) Debug.Log("メニューボタンが押されました: 設定画面を開く");
    }

    // =======================================================
    // Gizmos (Debug Visuals)
    // =======================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        if (beamFirePoints == null) return;

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection = firePoint.forward;

            if (lockOnTarget != null)
            {
                Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                fireDirection = (targetPosition - origin).normalized;
            }

            RaycastHit hit;
            Vector3 endPoint;

            if (Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0))
            {
                Gizmos.color = Color.red;
                endPoint = hit.point;
                Gizmos.DrawSphere(endPoint, 0.1f);
            }
            else
            {
                Gizmos.color = Color.cyan;
                endPoint = origin + fireDirection * beamMaxDistance;
            }
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin + fireDirection * beamMaxDistance, 0.05f);
        }
    }
}