// ファイル名: BlanceController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// プレイヤーの移動、HP/エネルギー管理、攻撃、およびInput Systemからの入力を制御します。
/// PlayerModesAndVisualsコンポーネントに依存します。
/// </summary>
public class BlanceController : MonoBehaviour
{
    // =======================================================
    // 依存コンポーネント / 関連オブジェクト
    // =======================================================

    [Header("Dependencies")]
    [Tooltip("親オブジェクトにアタッチされたCharacterController")]
    private CharacterController _playerController; // Inspectorでの確認用にSerializeField
    private TPSCameraController _tpsCamController;
    public PlayerModesAndVisuals _modesAndVisuals;
    public SceneBasedGameOverManager gameOverManager;

    [Header("UI & Vfx")]
    public Slider hPSlider;
    public Text hPText;
    public Slider energySlider;
    public BeamController beamPrefab;
    public Transform[] beamFirePoints;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // =======================================================
    // ステータス / 設定
    // =======================================================

    [Header("Movement Settings")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
    public float verticalSpeed = 10.0f;
    public float gravity = -9.81f;
    public bool canFly = true;
    public float fastFallMultiplier = 3.0f; // 標準重力に対する落下速度の乗数

    [Header("Stun & Hardening Settings")]
    public float attackFixedDuration = 0.8f;
    public float landStunDuration = 0.2f;

    [Header("Health & Energy")]
    public float maxHP = 10000.0f;
    public float maxEnergy = 1000.0f;
    public float energyRecoveryRate = 10.0f;
    public float recoveryDelay = 1.0f;
    public float energyConsumptionRate = 15.0f;

    [Header("Attack Settings")]
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;

    // =======================================================
    // プライベート/キャッシュ変数
    // =======================================================

    // ステータス
    private float _currentHP;
    public float CurrentHP => _currentHP; // 外部からの読み取り専用プロパティ
    private float _currentEnergy;
    public float CurrentEnergy => _currentEnergy; // 外部からの読み取り専用プロパティ

    // 制御フラグ/タイマー
    private bool _isAttacking = false;
    private bool _isStunned = false;
    private float _stunTimer = 0.0f;
    private float _lastEnergyConsumptionTime;
    private bool _isDead = false;

    // 移動制御
    private Vector3 _velocity;
    private float _moveSpeed; // 派生した移動速度
    private bool _wasGrounded = false; // 前フレームの接地状態
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

        bool isGroundedNow = _playerController.isGrounded;

        // 1. 硬直状態の処理 (最優先)
        HandleStunState(isGroundedNow);

        if (_isStunned)
        {
            // 硬直中は Y 軸の重力処理のみ行い、その他をスキップ
            HandleStunnedVerticalMovement(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // 2. プレイヤーの向き制御（ロックオン時）
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        // 3. ステータスとエネルギー管理
        ApplyArmorStats();
        HandleEnergy();

        // 4. 入力と移動
        HandleInput();
        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
        _playerController.Move(finalMove * Time.deltaTime);

        // 5. 接地状態の更新
        _wasGrounded = isGroundedNow;
    }

    // =======================================================
    // 初期化 / セットアップ
    // =======================================================

    private void InitializeComponents()
    {
        if (_playerController == null)
        {
            _playerController = GetComponentInParent<CharacterController>();
        }

        if (_playerController == null)
        {
            Debug.LogError($"{nameof(BlanceController)}: CharacterControllerが見つかりません。");
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

    // =======================================================
    // 硬直 (Stun) 制御関数
    // =======================================================

    /// <summary>着地硬直を開始。</summary>
    public void StartLandingStun()
    {
        if (_isStunned) return;
        _isStunned = true;
        _stunTimer = landStunDuration;
        _velocity = Vector3.zero; // 着地時の滑りを防止
    }

    /// <summary>攻撃硬直を開始。</summary>
    public void StartAttackStun()
    {
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = attackFixedDuration;
        _velocity.y = 0f; // 攻撃硬直中はY軸移動を一時停止
    }

    /// <summary>硬直状態を処理し、タイマーが切れたら解除します。</summary>
    private void HandleStunState(bool isGrounded)
    {
        if (!_isStunned) return;

        _stunTimer -= Time.deltaTime;

        if (_stunTimer <= 0.0f)
        {
            _isStunned = false;
            _isAttacking = false;
            if (isGrounded)
            {
                _velocity.y = -0.1f;
            }
        }
    }

    /// <summary>硬直中の Y 軸移動 (重力) のみ処理します。</summary>
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


    // =======================================================
    // Movement Logic
    // =======================================================

    private Vector3 HandleHorizontalMovement()
    {
        if (_isStunned) return Vector3.zero;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (h == 0f && v == 0f) return Vector3.zero;

        Vector3 inputDirection = new Vector3(h, 0, v);
        Vector3 moveDirection;

        // カメラの向きに基づく移動方向の計算
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

    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _playerController.isGrounded;

        // 着地硬直の判定
        if (!_wasGrounded && isGrounded && _velocity.y < -0.1f && !_isStunned)
        {
            StartLandingStun();
            return Vector3.zero; // 硬直開始フレームでは移動しない
        }

        // 接地リセット
        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = -0.1f;
        }

        if (_isStunned) return Vector3.zero; // 硬直中は水平移動同様に垂直移動も停止

        // 飛行制御
        bool isFlyingUp = (Input.GetKey(KeyCode.Space) || _verticalInput > 0.5f);
        bool isFlyingDown = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || _verticalInput < -0.5f);

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

        // 重力適用
        if (!hasVerticalInput && !isGrounded)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }
        else if (hasVerticalInput)
        {
            // エネルギー消費
            ConsumeEnergy(energyConsumptionRate * Time.deltaTime);
        }

        // エネルギー枯渇時の上昇停止
        if (_currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    private void ApplyArmorStats()
    {
        var stats = _modesAndVisuals.CurrentArmorStats;
        _moveSpeed = baseMoveSpeed * (stats != null ? stats.moveSpeedMultiplier : 1.0f);
    }

    // =======================================================
    // Input Handling
    // =======================================================

    private void HandleInput()
    {
        if (_isStunned) return;

        // キーボード/マウス入力の処理
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
    }

    // =======================================================
    // Attack Logic
    // =======================================================

    private void HandleMeleeAttack()
    {
        StartAttackStun();

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null)
        {
            RotateTowards(GetLockOnTargetPosition(lockOnTarget));
        }

        // 近接攻撃の当たり判定
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform != this.transform) // 自身を除外
            {
                ApplyDamageToEnemy(hitCollider, meleeDamage, isBeam: false);
            }
        }
    }

    private void HandleBeamAttack()
    {
        if (!ConsumeEnergy(beamAttackEnergyCost))
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        if (beamFirePoints == null || beamFirePoints.Length == 0) return;
        if (beamPrefab == null)
        {
            Debug.LogError("ビームプレハブが設定されていません。");
            return;
        }

        StartAttackStun();

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;

        if (isLockedOn)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, useOffsetIfNoCollider: true);
            RotateTowards(targetPosition);
        }

        // ビームの発射処理
        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection = isLockedOn ? (targetPosition - origin).normalized : firePoint.forward;

            RaycastHit hit;
            Vector3 endPoint;
            if (Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0))
            {
                endPoint = hit.point;
                ApplyDamageToEnemy(hit.collider, beamDamage, isBeam: true);
            }
            else
            {
                endPoint = origin + fireDirection * beamMaxDistance;
            }

            // ビジュアルエフェクトの生成と発射
            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit: true); // didHit はビームの見た目処理に依存
        }
    }

    /// <summary>衝突したColliderから、該当する敵コンポーネントを探してダメージを与える。</summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount, bool isBeam)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // ★ [改善推奨]: 敵の型を列挙する代わりに、共通インターフェース(例: IDamageable)を使用してください。
        // 現状のロジックを維持しつつ、冗長性を削減するためTryGetComponentで一つずつ確認。
        if (target.TryGetComponent<SoldierMoveEnemy>(out var soldierMoveEnemy)) { soldierMoveEnemy.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SoliderEnemy>(out var soliderEnemy)) { soliderEnemy.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<TutorialEnemyController>(out var tutorialEnemy)) { tutorialEnemy.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var scorpion)) { scorpion.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var suicide)) { suicide.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var drone)) { drone.TakeDamage(damageAmount); isHit = true; }

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
    // Energy / HP Management
    // =======================================================

    private void HandleEnergy()
    {
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            var stats = _modesAndVisuals.CurrentArmorStats;
            float recoveryMultiplier = stats != null ? stats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            _currentEnergy += recoveryRate * Time.deltaTime;
        }

        _currentEnergy = Mathf.Clamp(_currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();

        // エネルギー枯渇/回復イベントは UI のみを目的とするため、フラグ処理は簡素化
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

        Debug.Log($"<color=red>プレイヤーがダメージを受けました！</color> ダメージ量: {finalDamage}, 残りHP: {_currentHP}");

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
    // Input System Event Handlers (コンテキストによる bool/float 設定のみに簡素化)
    // =======================================================

    // Aボタン (上昇) - Action名: FlyUp
    public void OnFlyUp(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        _verticalInput = context.performed ? 1f : 0f;
    }

    // Bボタン (下降) - Action名: FlyDown
    public void OnFlyDown(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        _verticalInput = context.performed ? -1f : 0f;
    }

    // Right Button/RB (加速/ブースト) - Action名: Boost
    public void OnBoost(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        _isBoosting = context.performed;
    }

    // Right Trigger/RT (攻撃) - Action名: Attack
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        if (context.started && !_isAttacking)
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
    }

    // Yボタン (武装切替) - Action名: WeaponSwitch
    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        if (context.started)
        {
            _modesAndVisuals.SwitchWeapon();
        }
    }

    // DPad (Armor Switch)
    public void OnDPad(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned || !context.started) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.x > 0.5f) { _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Buster); }
        else if (input.y > 0.5f) { _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Normal); }
        else if (input.x < -0.5f) { _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Speed); }
    }

    // Menuボタン (設定画面)
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (_isDead || _isStunned) return;
        if (context.started)
        {
            Debug.Log("メニューボタンが押されました: 設定画面を開く");
        }
    }

    // =======================================================
    // Debug Visuals
    // =======================================================

    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程
        if (beamFirePoints == null || _tpsCamController == null) return;

        Transform lockOnTarget = _tpsCamController.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;

        if (isLockedOn)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
        }

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection = isLockedOn ? (targetPosition - origin).normalized : firePoint.forward;

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