using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// プレイヤーの移動、エネルギー管理、攻撃、およびアーマー制御を制御します。
/// チュートリアルステージ用に、各入力の許可/不許可を外部から制御できます。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TutorialPlayerController : MonoBehaviour
{
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 }
    private const string SelectedArmorKey = "SelectedArmorIndex";

    [Header("Game Over Settings")]
    public SceneBasedGameOverManager gameOverManager;

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

    // ⭐ 降下速度高速化のための乗数
    [Tooltip("標準重力に対する落下速度の乗数 (例: 2.0 = 2倍速く落下)")]
    public float fastFallMultiplier = 3.0f;

    [Header("Melee Lock-On Dash Settings")]
    // 突進に関する設定は削除されました。

    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations = new List<ArmorStats>
    {
        new ArmorStats { name = "Normal", defenseMultiplier = 1.0f, moveSpeedMultiplier = 1.0f, energyRecoveryMultiplier = 1.0f },
        new ArmorStats { name = "Buster Mode", defenseMultiplier = 1.5f, moveSpeedMultiplier = 0.8f, energyRecoveryMultiplier = 0.8f },
        new ArmorStats { name = "Speed Mode", defenseMultiplier = 0.75f, moveSpeedMultiplier = 1.5f, energyRecoveryMultiplier = 1.2f }
    };

    [Header("Tutorial Input Control")]
    [Tooltip("全ての入力を上書きして停止させます (優先度高)")]
    public bool isInputLocked = false;
    [Tooltip("水平方向 (WASD) の移動を許可します")]
    public bool allowHorizontalMove = true;
    [Tooltip("垂直方向 (Space/Alt) の移動を許可します")]
    public bool allowVerticalMove = true;
    [Tooltip("ダッシュ (Left Shift) を許可します")]
    public bool allowDash = false;
    [Tooltip("武器切り替え (Eキー) を許可します")]
    public bool allowWeaponSwitch = true;
    [Tooltip("アーマー切り替え (1, 2, 3キー) を許可します")]
    public bool allowArmorSwitch = true;
    [Tooltip("攻撃 (左クリック) を許可します")]
    public bool allowAttack = true;

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

    private CharacterController _controller;
    private TPSCameraController _tpsCamController;

    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;
    private float _currentHP;
    private float _currentEnergy;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;
    private float _lastEnergyConsumptionTime;
    private bool _hasTriggeredEnergyDepletedEvent = false;
    private bool _isDead = false;
    // private bool _isMeleeDashing = false; // 削除

    private Vector3 _velocity;
    private float _moveSpeed;

    [HideInInspector] public float currentHP { get => _currentHP; private set => _currentHP = value; }
    [HideInInspector] public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;

    // チュートリアル用イベントとプロパティ
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    public event Action onEnergyDepleted;
    public float WASDMoveTimer { get; private set; }
    public float JumpTimer { get; private set; }
    public float DescendTimer { get; private set; }
    private float DashTimer { get; set; }

    public bool HasMovedHorizontally => WASDMoveTimer > 0f;
    public bool HasJumped => JumpTimer > 0f;
    public bool HasDescended => DescendTimer > 0f;
    public bool HasDashed => DashTimer > 0f;


    // =======================================================
    // Unity Lifecycle Methods
    // =======================================================

    [Obsolete]
    void Awake()
    {
        InitializeComponents();
    }

    [Obsolete]
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

        // 攻撃状態 または 入力ロック中 の処理
        if (isInputLocked || _isAttacking)
        {
            HandleAttackState();

            // if (!_isMeleeDashing) // 削除
            {
                // ロック/攻撃中に垂直方向の慣性を維持するため、重力を手動で適用
                if (!_controller.isGrounded)
                {
                    _velocity.y += gravity * Time.deltaTime;
                }
                _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);

                // ロック/攻撃中は移動関連のタイマーをリセット
                ResetMovementTimers();
            }
            // 突進中はMeleeDashTowardsTargetコルーチンが移動を制御 (削除)
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

        if (allowVerticalMove) finalMove += HandleVerticalMovement();
        if (allowHorizontalMove) finalMove += HandleHorizontalMovement();

        _controller.Move(finalMove * Time.deltaTime);
    }

    [Obsolete]
    private void InitializeComponents()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            Debug.LogError($"{nameof(TutorialPlayerController)}: CharacterControllerが見つかりません。");
            enabled = false;
            return;
        }

        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (_tpsCamController == null)
        {
            Debug.LogWarning($"{nameof(TutorialPlayerController)}: TPSCameraControllerが見つかりません。ビームのロックオン機能は無効になります。");
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

    private void ResetMovementTimers()
    {
        WASDMoveTimer = JumpTimer = DescendTimer = DashTimer = 0f;
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

    private void HandleInput()
    {
        HandleAttackInputs();
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();
    }

    private void HandleArmorSwitchInput()
    {
        if (!allowArmorSwitch) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
    }

    private void HandleWeaponSwitchInput()
    {
        if (!allowWeaponSwitch) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchWeapon();
        }
    }

    private void HandleAttackInputs()
    {
        if (!allowAttack || _isAttacking) return;

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

    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (h == 0f && v == 0f)
        {
            WASDMoveTimer = 0f;
            DashTimer = 0f;
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

        moveDirection.Normalize();

        float currentSpeed = _moveSpeed;
        bool isConsumingEnergy = false;

        bool isDashing = allowDash && Input.GetKey(KeyCode.LeftShift) && currentEnergy > 0.01f;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
            DashTimer += Time.deltaTime;
        }
        else
        {
            DashTimer = 0f;
        }

        WASDMoveTimer += Time.deltaTime;

        if (isConsumingEnergy) _lastEnergyConsumptionTime = Time.time;

        return moveDirection * currentSpeed;
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
                JumpTimer += Time.deltaTime;
                DescendTimer = 0f;
                hasVerticalInput = true;
            }
            // Altキーは降下
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                _velocity.y = -verticalSpeed;
                DescendTimer += Time.deltaTime;
                JumpTimer = 0f;
                hasVerticalInput = true;
            }
        }

        if (!hasVerticalInput)
        {
            if (!isGrounded)
            {
                // ⭐ リファクタリング: 降下速度を速くするため、重力に fastFallMultiplier を適用
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
            JumpTimer = 0f;
        }

        return new Vector3(0, _velocity.y, 0);
    }


    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        Transform lockOnTarget = _tpsCamController != null ? _tpsCamController.LockOnTarget : null;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        // 突進の判定と実行 (削除)
        if (lockOnTarget != null)
        {
            // ターゲットの方向を向く (突進はしない)
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget);
            RotateTowards(targetPosition);
        }

        // ダメージ判定
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;

            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }

        onMeleeAttackPerformed?.Invoke();
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

        onBeamAttackPerformed?.Invoke();
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

    /*
    // MeleeDashTowardsTarget コルーチンは削除されました。
    private IEnumerator MeleeDashTowardsTarget(Vector3 targetPosition)
    {
        // ... (削除)
    }
    */

    void HandleAttackState()
    {
        if (!_isAttacking) return;

        // 突進中の時間計測はMeleeDashTowardsTargetコルーチンに任せる (削除)
        // if (_isMeleeDashing) return; 

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

    private void HandleEnergy()
    {
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            float recoveryMultiplier = _currentArmorStats != null ? _currentArmorStats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            currentEnergy += recoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();

        // エネルギー枯渇イベントの管理
        if (currentEnergy <= 0.1f && !_hasTriggeredEnergyDepletedEvent)
        {
            onEnergyDepleted?.Invoke();
            _hasTriggeredEnergyDepletedEvent = true;
        }
        else if (currentEnergy > 0.1f && _hasTriggeredEnergyDepletedEvent && Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            _hasTriggeredEnergyDepletedEvent = false;
        }
    }

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

    /// <summary>
    /// 衝突したColliderから、該当する敵コンポーネントを探してダメージを与える。
    /// 💡 改善点: 敵クラスに IDamageable インターフェースを実装させ、GetComponent<IDamageable>() で処理を一本化することを推奨します。
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 💡 敵コンポーネントへの依存
        // 以下の冗長な処理は、IDamageableインターフェースを導入することで改善できます。
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
        isInputLocked = true;

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


    public void ResetInputTracking()
    {
        ResetMovementTimers();
    }

    public void SwitchWeaponMode(WeaponMode newMode)
    {
        if (_currentWeaponMode == newMode) return;

        _currentWeaponMode = newMode;
        UpdateWeaponUIEmphasis();
        Debug.Log($"[Manager] 武器を強制切り替えしました: **{_currentWeaponMode}**");
    }

    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. 近接突進の範囲 (ワイヤーフレーム) は削除されました。

        // 3. ビーム攻撃の射程
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