// ファイル名: BlanceController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic; // List<T>を使用する場合に必要ですが、今回はArrayのままにします。

/// <summary>
/// プレイヤーの移動、HP/エネルギー管理、攻撃、およびInput Systemからの入力を制御します。
/// PlayerModesAndVisualsコンポーネントに依存します。
/// </summary>
// ★注意: 親にCharacterControllerをアタッチする場合は、子にアタッチされているCharacterControllerを削除し、
// この[RequireComponent(typeof(CharacterController))]も削除することが推奨されます。
// 今回は元のコードの構造を残し、機能だけを親依存に変更します。
public class BlanceController : MonoBehaviour
{
    [Header("Dependencies")]
    // private CharacterController _controller; // 元の子のCharacterController（今回は親を使う）
    private CharacterController _playerController; // ★修正：親オブジェクトのCharacterControllerを格納
    private TPSCameraController _tpsCamController;
    public PlayerModesAndVisuals _modesAndVisuals; // モード/ビジュアルコンポーネント

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

    [Header("Attack Settings")]
    public float attackFixedDuration = 0.8f;
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;

    [Header("VFX & Layers")]
    public BeamController beamPrefab;
    // ★修正: 単一のFirePointから複数のFirePointsへ変更
    // Unityエディタで2つ以上のTransformを割り当ててください。
    public Transform[] beamFirePoints;
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
            // ★修正：親のCharacterControllerのisGroundedを使用
            if (!_playerController.isGrounded)
            {
                _velocity.y += gravity * Time.deltaTime;
            }
            // ★修正：親のCharacterControllerを動かす
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

        HandleInput(); // 古いInput向け処理
        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();

        // ★修正：親のCharacterControllerを動かす
        _playerController.Move(finalMove * Time.deltaTime);
    }

    // =======================================================
    // Initialization / Setup
    // =======================================================

    private void InitializeComponents()
    {
        // ★修正：親オブジェクトのCharacterControllerを取得
        _playerController = GetComponentInParent<CharacterController>();

        if (_playerController == null)
        {
            Debug.LogError($"{nameof(BlanceController)}: 必要なCharacterControllerが見つかりません。親オブジェクト (Player) にアタッチしてください。");
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
    // Energy / HP Management (変更なし)
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
    // Input Handling (Old Input System/Keyboard Fallbacks) (変更なし)
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
                    HandleBeamAttack();
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

        // カメラ基準の移動方向を決定
        if (_tpsCamController != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraRotation * inputDirection;
        }
        else
        {
            // transformは子オブジェクト（Blance）のTransformです。
            moveDirection = transform.right * h + transform.forward * v;
        }

        moveDirection.Normalize();

        float currentSpeed = _moveSpeed;

        // ダッシュ処理 (キーボード LeftShift / Input System _isBoosting)
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
        // ★修正：親のCharacterControllerのisGroundedを使用
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
    // Attack Logic (ビーム発射処理を複数FirePoint対応に修正)
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

        // transform.positionは子オブジェクト（Blance）の位置を基準にします。
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;
            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    private void HandleBeamAttack()
    {
        // エネルギーチェックは一度で良い
        if (!ConsumeEnergy(beamAttackEnergyCost))
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        if (beamFirePoints == null || beamFirePoints.Length == 0 || beamPrefab == null)
        {
            Debug.LogError("ビームの発射点またはプレハブが設定されていません。");
            return;
        }

        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f;

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;

        if (isLockedOn)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            RotateTowards(targetPosition);
        }

        // ★修正: すべてのFirePointからビームを発射
        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection;

            if (isLockedOn)
            {
                fireDirection = (targetPosition - origin).normalized;
            }
            else
            {
                // ロックオンしていない場合は、FirePointの向きをそのまま使用
                fireDirection = firePoint.forward;
            }

            RaycastHit hit;
            Vector3 endPoint;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

            if (didHit)
            {
                endPoint = hit.point;
                // ダメージはFirePointの数で分割すべきか、そのままか、ゲームデザインによります。
                // ここではシンプルに、当たった場合はビーム一本分のダメージを与えることにします。
                ApplyDamageToEnemy(hit.collider, beamDamage);
            }
            else
            {
                endPoint = origin + fireDirection * beamMaxDistance;
            }

            BeamController beamInstance = Instantiate(
                beamPrefab,
                origin,
                Quaternion.LookRotation(fireDirection)
            );
            // BeamController.Fire()は実装されているものと仮定
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 敵コンポーネントへの依存
        // （略）
        // 以下の行をコメントアウトまたは適切な敵コンポーネントのロジックに置き換える必要があります
        // if (target.TryGetComponent<IDamageable>(out var damageable))
        // {
        //     damageable.TakeDamage(damageAmount);
        //     isHit = true;
        // }

        // 暫定的にレイヤーで判定できると仮定してisHitをtrueにします
        if (((1 << target.layer) & enemyLayer) != 0)
        {
            // 敵レイヤーに属していればヒットと見なします (本来は敵コンポーネントでの判定が必要)
            isHit = true;
        }


        if (isHit && hitEffectPrefab != null)
        {
            // Raycastのhit.pointを使う代わりに、一旦ターゲットの位置でエフェクトを生成
            // 近接攻撃との兼ね合いを考え、今回はそのままColliderの位置を使用
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

    void HandleAttackState()
    {
        if (!_isAttacking) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            // ★修正：親のCharacterControllerのisGroundedを使用
            if (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Beam && !_playerController.isGrounded)
            {
                _velocity.y = 0;
            }
            // ★修正：親のCharacterControllerのisGroundedを使用
            else if (_playerController.isGrounded)
            {
                _velocity.y = -0.1f;
            }
        }
    }

    // =======================================================
    // UI Updates (変更なし)
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
    // Input System Event Handlers (変更なし)
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

    // Right Button/RB (加速/ブースト) - Action名: RightShoulder (推奨) または Boost
    public void OnBoost(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        _isBoosting = context.performed;
    }

    // Right Trigger/RT (攻撃) - Action名: RightTrigger または Attack
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (_isDead) return;
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

    // Yボタン (武装切替) - Action名: WeaponSwitch (推奨) または YButton
    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (context.started)
        {
            _modesAndVisuals.SwitchWeapon();
        }
    }

    // DPad (Armor Switch - 例として DPad Right, Up, Left を使用)
    public void OnDPad(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (context.started)
        {
            Vector2 input = context.ReadValue<Vector2>();

            // DPadの右 (Buster Mode)
            if (input.x > 0.5f)
            {
                _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Buster);
            }
            // DPadの上 (Normal Mode)
            else if (input.y > 0.5f)
            {
                _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Normal);
            }
            // DPadの左 (Speed Mode)
            else if (input.x < -0.5f)
            {
                _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Speed);
            }
        }
    }

    // Menuボタン (設定画面) - Action名: Menu (推奨) または StartButton
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (_isDead) return;
        if (context.started)
        {
            Debug.Log("メニューボタンが押されました: 設定画面を開く");
        }
    }


    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        // transform.positionは子オブジェクト（Blance）の位置を基準にします。
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程 (複数FirePoint対応に修正)
        if (beamFirePoints != null && beamFirePoints.Length > 0 && beamFirePoints[0] != null) // 少なくとも1つ目のFirePointが存在するか確認
        {
            Transform lockOnTarget = _tpsCamController != null ? _tpsCamController.LockOnTarget : null;
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
                Vector3 fireDirection;

                if (isLockedOn)
                {
                    fireDirection = (targetPosition - origin).normalized;
                }
                else
                {
                    fireDirection = firePoint.forward;
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
}