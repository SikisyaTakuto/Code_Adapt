using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// プレイヤーの移動、エネルギー管理、攻撃、およびアーマー制御を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // =======================================================
    // Enum and Constants
    // =======================================================
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 }
    private const string SelectedArmorKey = "SelectedArmorIndex";

    // =======================================================
    // Serialized Classes
    // =======================================================
    [System.Serializable]
    public class ArmorStats
    {
        public string name;
        [Tooltip("ダメージ軽減率 (例: 1.0 = 変更なし, 0.5 = ダメージ半減)")]
        public float defenseMultiplier = 1.0f;
        [Tooltip("移動速度補正 (例: 1.5 = 1.5倍速)")]
        public float moveSpeedMultiplier = 1.0f;
        [Tooltip("エネルギー回復補正")]
        public float energyRecoveryMultiplier = 1.0f;
    }

    // =======================================================
    // Public Fields (Inspector Settings)
    // =======================================================

    [Header("Game Over Settings")]
    public SceneBasedGameOverManager gameOverManager;

    [Header("Armor UI & Visuals")]
    public Image currentArmorIconImage;
    public Sprite[] armorSprites;
    public GameObject[] armorModels;

    [Header("Weapon UI")]
    public Image meleeWeaponIcon;
    public Text meleeWeaponText;
    public Image beamWeaponIcon;
    public Text beamWeaponText;
    public Color emphasizedColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
    public float verticalSpeed = 10.0f;
    public float energyConsumptionRate = 15.0f;
    public float energyRecoveryRate = 10.0f;
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public bool canFly = true;
    public float gravity = -9.81f;
    [Tooltip("標準重力に対する落下速度の乗数 (例: 2.0 = 2倍速く落下)")]
    public float fastFallMultiplier = 3.0f;

    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations = new List<ArmorStats>
    {
        new ArmorStats { name = "Normal", defenseMultiplier = 1.0f, moveSpeedMultiplier = 1.0f, energyRecoveryMultiplier = 1.0f },
        new ArmorStats { name = "Buster Mode", defenseMultiplier = 1.5f, moveSpeedMultiplier = 0.8f, energyRecoveryMultiplier = 0.8f },
        new ArmorStats { name = "Speed Mode", defenseMultiplier = 0.75f, moveSpeedMultiplier = 1.5f, energyRecoveryMultiplier = 1.2f }
    };

    [Header("Health Settings")]
    public float maxHP = 10000.0f;
    public Slider hPSlider;
    public Text hPText;

    [Header("Energy Gauge Settings")]
    public float maxEnergy = 1000.0f;
    public float recoveryDelay = 1.0f;
    public Slider energySlider;

    [Header("Attack Settings")]
    public float attackFixedDuration = 0.8f;

    [Header("Beam VFX")]
    public BeamController beamPrefab;
    public Transform beamFirePoint;
    public float beamMaxDistance = 100f;
    [Tooltip("ロックオン時に敵のColliderがない場合、ビームを狙う高さのオフセット。")]
    public float lockOnTargetHeightOffset = 1.0f;

    [Header("Melee Attack Settings")]
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // =======================================================
    // Private/Internal Fields
    // =======================================================
    private CharacterController _controller;
    private TPSCameraController _tpsCamController;
    private Animator _animator; // ★ NEW: アニメーターの参照

    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;
    private float _currentHP;
    private float _currentEnergy;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;
    private float _lastEnergyConsumptionTime;
    private bool _isDead = false;

    private Vector3 _velocity;
    private float _moveSpeed;
    private bool _isDashing = false; // ★ 既存: ダッシュ状態を管理するフラグ

    [HideInInspector] public float currentHP { get => _currentHP; private set => _currentHP = value; }
    [HideInInspector] public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;

    // =======================================================
    // Unity Lifecycle Methods
    // =======================================================

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        currentHP = maxHP;

        LoadAndSwitchArmor();
        UpdateUI();

        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();
            if (gameOverManager == null)
            {
                Debug.LogWarning($"{nameof(SceneBasedGameOverManager)}が見つかりません。");
            }
        }

        Debug.Log($"初期武器: {currentWeaponMode} | 初期アーマー: {currentArmorMode}");
    }

    void Update()
    {
        HandleTestInput();

        if (_isDead) return;

        // 攻撃状態 の処理
        if (_isAttacking)
        {
            HandleAttackState();

            // 攻撃中に垂直方向の慣性を維持するため、重力を手動で適用
            if (!_controller.isGrounded)
            {
                _velocity.y += gravity * Time.deltaTime;
            }
            _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);

            return;
        }

        // ロックオン中はTPSCameraControllerがプレイヤーの回転を制御
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        HandleInput();
        HandleEnergy();

        Vector3 finalMove = Vector3.zero;

        finalMove += HandleVerticalMovement();
        finalMove += HandleHorizontalMovement();

        _controller.Move(finalMove * Time.deltaTime);
    }

    // =======================================================
    // Initialization and Core Logic
    // =======================================================

    private void InitializeComponents()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            Debug.LogError($"{nameof(PlayerController)}: CharacterControllerが見つかりません。");
            enabled = false;
            return;
        }

        // ★ NEW: Animatorコンポーネントを取得
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        if (_animator == null)
        {
            Debug.LogWarning($"{nameof(PlayerController)}: Animatorコンポーネントが見つかりません。アニメーションは再生されません。");
        }
        // ------------------------------------

        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (_tpsCamController == null)
        {
            Debug.LogWarning($"{nameof(PlayerController)}: TPSCameraControllerが見つかりません。ビームのロックオン機能は無効になります。");
        }
    }

    private void LoadAndSwitchArmor()
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);

        if (Enum.IsDefined(typeof(ArmorMode), selectedIndex) && selectedIndex < armorConfigurations.Count)
        {
            SwitchArmor((ArmorMode)selectedIndex, false);
        }
        else
        {
            SwitchArmor(ArmorMode.Normal, false);
            Debug.LogWarning($"不正なアーマーインデックス({selectedIndex})が検出されました。Normalモードを適用します。");
        }
    }

    private void UpdateUI()
    {
        UpdateHPUI();
        UpdateEnergyUI();
        UpdateWeaponUIEmphasis();
    }

    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            currentHP = 0;
            UpdateHPUI();
            Die();
        }
    }

    // =======================================================
    // Input Handlers
    // =======================================================

    private void HandleInput()
    {
        HandleAttackInputs();
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();

        HandleDashInput();
    }

    // 既存: ダッシュ入力の処理
    private void HandleDashInput()
    {
        bool isSpeedMode = _currentArmorMode == ArmorMode.Speed;

        if (isSpeedMode)
        {
            // ダッシュ開始
            if (Input.GetKeyDown(KeyCode.LeftShift) && currentEnergy > 0.01f)
            {
                _isDashing = true;
                Debug.Log("ダッシュ開始 (Speed Mode)");
            }
            // ダッシュ終了: キーを離す、またはエネルギーがなくなる
            else if (Input.GetKeyUp(KeyCode.LeftShift) || (Input.GetKey(KeyCode.LeftShift) && currentEnergy <= 0.01f))
            {
                _isDashing = false;
                if (currentEnergy <= 0.01f) Debug.Log("ダッシュ終了: エネルギー不足");
            }
        }
        else
        {
            // Speed Mode でない場合は強制的にダッシュをオフにする
            _isDashing = false;
        }
    }
    // --------------------------------------------------------

    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
    }

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchWeapon();
        }
    }

    private void HandleAttackInputs()
    {
        if (_isAttacking) return;

        if (Input.GetMouseButtonDown(0))
        {
            switch (_currentWeaponMode)
            {
                case WeaponMode.Melee:
                    HandleMeleeAttack();
                    break;
                case WeaponMode.Beam:
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
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 停止中の処理（ダッシュ中でも入力を受け付けていない場合は静止）
        if (h == 0f && v == 0f && !_isDashing)
        {
            if (_animator != null) _animator.SetFloat("Speed", 0f);
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
            moveDirection = transform.right * h + transform.forward * v;
        }

        // 入力方向がゼロでない場合のみ正規化（停止状態での方向維持を防ぐ）
        if (inputDirection.magnitude > 0.01f)
        {
            moveDirection.Normalize();
        }

        float currentSpeed = _moveSpeed;
        bool isConsumingEnergy = false;

        // ダッシュ判定を _isDashing フラグに基づいて行う
        if (_isDashing && inputDirection.magnitude > 0.01f) // 入力があるときのみダッシュ速度を適用
        {
            currentSpeed *= dashMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        else if (_isDashing && inputDirection.magnitude <= 0.01f)
        {
            // ダッシュキーは押されているが、移動入力がない場合
            _isDashing = false;
        }

        Vector3 actualMove = moveDirection * currentSpeed;

        // ★ NEW: アニメーションの速度パラメーターを更新
        // CharacterControllerが実際に移動する水平速度を計算（垂直速度は無視）
        float currentHorizontalSpeed = new Vector3(actualMove.x, 0, actualMove.z).magnitude;

        if (_animator != null)
        {
            // _moveSpeed（基本速度）に対する正規化された速度を渡すと、アニメーション遷移がスムーズ
            // DashMultiplierを含めてSpeedパラメーターに渡す
            _animator.SetFloat("Speed", currentHorizontalSpeed);
        }
        // ------------------------------------------

        if (isConsumingEnergy) _lastEnergyConsumptionTime = Time.time;

        return actualMove;
    }

    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool hasVerticalInput = false;

        if (canFly && currentEnergy > 0.01f)
        {
            if (Input.GetKey(KeyCode.Space)) // 上昇
            {
                _velocity.y = verticalSpeed;
                hasVerticalInput = true;
            }
            // Altキーは降下
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
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
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }

        // エネルギー切れで上昇を止める
        if (currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    // =======================================================
    // Attack Logic
    // =======================================================

    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        // ★ NEW: アニメーションのトリガーを引く
        if (_animator != null)
        {
            _animator.SetTrigger("MeleeAttack");
        }
        // ------------------------------------------

        Transform lockOnTarget = _tpsCamController != null ? _tpsCamController.LockOnTarget : null;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        if (lockOnTarget != null)
        {
            // ターゲットの方向を向く
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget);
            RotateTowards(targetPosition);
        }

        // ダメージ判定
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;

            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    private void HandleBeamAttack()
    {
        if (currentEnergy < beamAttackEnergyCost)
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        if (beamFirePoint == null || beamPrefab == null)
        {
            Debug.LogError("ビームの発射点またはプレハブが設定されていません。");
            return;
        }

        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f; // ビーム発射時は垂直移動を停止

        // ★ NEW: アニメーションのトリガーを引く
        if (_animator != null)
        {
            _animator.SetTrigger("BeamAttack");
        }
        // ------------------------------------------

        currentEnergy -= beamAttackEnergyCost;
        _lastEnergyConsumptionTime = Time.time;
        UpdateEnergyUI();

        Vector3 origin = beamFirePoint.position;
        Vector3 fireDirection;
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;

        if (lockOnTarget != null)
        {
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            fireDirection = (targetPosition - origin).normalized;
            RotateTowards(targetPosition);
        }
        else
        {
            fireDirection = beamFirePoint.forward;
        }

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

        // BeamControllerへの依存をそのままに
        BeamController beamInstance = Instantiate(
            beamPrefab,
            origin,
            Quaternion.LookRotation(fireDirection)
        );
        beamInstance.Fire(origin, endPoint, didHit);
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

            if (_currentWeaponMode == WeaponMode.Beam && !_controller.isGrounded)
            {
                // ビーム攻撃終了後、空中であれば垂直速度をリセットして落下開始
                _velocity.y = 0;
            }
            else if (_controller.isGrounded)
            {
                _velocity.y = -0.1f;
            }
        }
    }

    // =======================================================
    // Energy and Damage
    // =======================================================

    private void HandleEnergy()
    {
        // ダッシュ中または垂直移動中はエネルギー消費中と判断
        bool isConsumingEnergy = _isDashing || (!_controller.isGrounded && (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)));
        if (isConsumingEnergy)
        {
            _lastEnergyConsumptionTime = Time.time;
        }

        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            float recoveryMultiplier = _currentArmorStats != null ? _currentArmorStats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            currentEnergy += recoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();
    }

    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        float finalDamage = damageAmount;

        if (_currentArmorStats != null)
        {
            finalDamage *= _currentArmorStats.defenseMultiplier;
        }

        currentHP -= finalDamage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPUI();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        // ★ NEW: 死亡時にアニメーションパラメーターをリセット（または死亡アニメーションを再生）
        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f); // 移動を停止
            // 死亡アニメーションがあれば _animator.SetTrigger("Die"); など
        }

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

    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 💡 敵コンポーネントへの依存
        // チュートリアル版のコード構造を維持するため、そのまま残します。
        if (target.TryGetComponent<TutorialEnemyController>(out var tutorialEnemy))
        {
            tutorialEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<ScorpionEnemy>(out var scorpion))
        {
            scorpion.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<SuicideEnemy>(out var suicide))
        {
            suicide.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<DroneEnemy>(out var drone))
        {
            drone.TakeDamage(damageAmount);
            isHit = true;
        }


        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
        }
    }

    // =======================================================
    // Armor and Weapon Switching
    // =======================================================

    private void SwitchArmor(ArmorMode newMode, bool shouldLog = true)
    {
        int index = (int)newMode;
        if (index < 0 || index >= armorConfigurations.Count)
        {
            Debug.LogError($"アーマーモード {newMode} の設定が見つかりません。");
            return;
        }

        if (_currentArmorMode == newMode && _currentArmorStats != null)
        {
            if (shouldLog) Debug.Log($"アーマーは既に **{newMode}** です。");
            return;
        }

        _currentArmorMode = newMode;
        _currentArmorStats = armorConfigurations[index];

        _moveSpeed = baseMoveSpeed * _currentArmorStats.moveSpeedMultiplier;

        // アーマー切り替え時にダッシュ状態をリセット
        _isDashing = false;

        PlayerPrefs.SetInt(SelectedArmorKey, index);
        PlayerPrefs.Save();

        UpdateArmorVisuals(index);

        if (shouldLog)
        {
            Debug.Log($"アーマーを切り替えました: **{_currentArmorStats.name}** " +
                        $" (速度補正: x{_currentArmorStats.moveSpeedMultiplier}, 防御補正: x{_currentArmorStats.defenseMultiplier}, 回復補正: x{_currentArmorStats.energyRecoveryMultiplier})");
        }
    }

    private void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;
        UpdateWeaponUIEmphasis();
        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");
    }

    // =======================================================
    // UI Update Methods
    // =======================================================

    private void UpdateArmorVisuals(int index)
    {
        if (currentArmorIconImage != null && armorSprites != null && index < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[index];
            currentArmorIconImage.enabled = true;
        }

        if (armorModels != null)
        {
            for (int i = 0; i < armorModels.Length; i++)
            {
                if (armorModels[i] != null)
                {
                    armorModels[i].SetActive(i == index);
                }
            }
        }
    }

    private void UpdateWeaponUIEmphasis()
    {
        bool isMelee = (_currentWeaponMode == WeaponMode.Melee);

        // アイコンの色を更新
        if (meleeWeaponIcon != null) meleeWeaponIcon.color = isMelee ? emphasizedColor : normalColor;
        if (beamWeaponIcon != null) beamWeaponIcon.color = isMelee ? normalColor : emphasizedColor;

        // テキストを更新
        if (meleeWeaponText != null)
        {
            meleeWeaponText.text = "Melee";
            meleeWeaponText.color = isMelee ? emphasizedColor : normalColor;
        }
        if (beamWeaponText != null)
        {
            beamWeaponText.text = "Beam";
            beamWeaponText.color = isMelee ? normalColor : emphasizedColor;
        }
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    void UpdateHPUI()
    {
        if (hPSlider != null)
        {
            hPSlider.value = currentHP / maxHP;
        }

        if (hPText != null)
        {
            int currentHPInt = Mathf.CeilToInt(currentHP);
            int maxHPInt = Mathf.CeilToInt(maxHP);

            hPText.text = $"{currentHPInt} / {maxHPInt}";
        }
    }

    // =======================================================
    // Public Utility Methods
    // =======================================================

    public void SwitchWeaponMode(WeaponMode newMode)
    {
        if (_currentWeaponMode == newMode) return;

        _currentWeaponMode = newMode;
        UpdateWeaponUIEmphasis();
        Debug.Log($"[Manager] 武器を強制切り替えしました: **{_currentWeaponMode}**");
    }

    // =======================================================
    // Debug Visualizations
    // =======================================================

    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程
        if (beamFirePoint != null)
        {
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