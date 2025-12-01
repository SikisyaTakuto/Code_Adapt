using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq; // Using System.Linq for potential future use

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // === Enums and Consts ===
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 }
    private const string SelectedArmorKey = "SelectedArmorIndex";

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

    // === 設定: アーマー, UI, ステータス ===
    [Header("1. Armor & Visuals")]
    public List<ArmorStats> armorConfigurations;
    public Image currentArmorIconImage;
    public Sprite[] armorSprites;
    public GameObject[] armorModels;

    [Header("2. Core Stats & Movement")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;

    // 慣性用の追加設定 🚀
    [Tooltip("水平移動の加速速度 (値が大きいほど速く目標速度に達する)")]
    public float accelerationSpeed = 0.1f;
    [Tooltip("水平移動の減速速度 (値が大きいほど速く停止する)")]
    public float decelerationSpeed = 0.15f;
    [Tooltip("空中での水平移動の加速速度")]
    public float airAccelerationSpeed = 0.3f;

    public float verticalSpeed = 10.0f;
    public float gravity = -9.81f;
    public float fastFallMultiplier = 3.0f;
    public bool canFly = true;

    [Header("3. Energy & Health")]
    public float maxHP = 10000.0f;
    public Slider hPSlider;
    public Text hPText;
    public float maxEnergy = 1000.0f;
    public float energyConsumptionRate = 15.0f;
    public float energyRecoveryRate = 10.0f;
    public float recoveryDelay = 1.0f;
    public Slider energySlider;

    [Header("4. Weapon Settings")]
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public float attackFixedDuration = 0.8f;
    public BeamController beamPrefab;
    public Transform beamFirePoint;
    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    [Header("5. UI & Managers")]
    public Image meleeWeaponIcon;
    public Text meleeWeaponText;
    public Image beamWeaponIcon;
    public Text beamWeaponText;
    public Color emphasizedColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    public SceneBasedGameOverManager gameOverManager;

    // === プライベート/キャッシュ変数 ===
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
    private bool _isDead = false;

    // 慣性用の追加変数 🚀
    private Vector3 _velocity; // 垂直方向の速度 (Gravity, Jump, Fly)
    private Vector3 _currentMoveVelocity; // 現在の水平移動速度 (慣性)
    private Vector3 _currentVelocityRef; // SmoothDamp用の参照速度

    private float _moveSpeed; // 最終的な水平移動速度 (ベース速度 * アーマー補正)

    // Public Getters (簡略化)
    [HideInInspector] public float currentHP { get => _currentHP; private set => _currentHP = value; }
    [HideInInspector] public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;

    // =======================================================
    // Unity Lifecycle
    // =======================================================

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (_controller == null) { Debug.LogError($"{nameof(PlayerController)}: CharacterControllerが見つかりません。"); enabled = false; }
        if (_tpsCamController == null) { Debug.LogWarning($"{nameof(PlayerController)}: TPSCameraControllerが見つかりません。ロックオン機能は無効。"); }
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        currentHP = maxHP;
        LoadAndSwitchArmor();
        UpdateUI();
        if (gameOverManager == null) gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();

        // 慣性初期化 🚀
        _currentMoveVelocity = Vector3.zero;
    }

    void Update()
    {
        if (_isDead) return;

        if (_isAttacking)
        {
            HandleAttackState();
            // 攻撃中は水平移動は行わず、垂直方向の慣性を維持するため、重力を手動で適用
            if (!_controller.isGrounded) _velocity.y += gravity * Time.deltaTime;
            _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);
            return;
        }

        // カメラ制御によるプレイヤーの回転 (ロックオン中はTPSCameraControllerが制御)
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        HandleInput();
        HandleEnergy();

        Vector3 horizontalMove = HandleHorizontalMovement(); // 慣性速度計算
        Vector3 verticalMove = HandleVerticalMovement();

        // 慣性速度を適用
        Vector3 finalMove = horizontalMove + verticalMove;
        _controller.Move(finalMove * Time.deltaTime);
    }

    // =======================================================
    // Input Handling
    // =======================================================

    private void HandleInput()
    {
        HandleAttackInputs();
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();
    }

    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
    }

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.E)) SwitchWeapon();
    }

    private void HandleAttackInputs()
    {
        if (_isAttacking || !Input.GetMouseButtonDown(0)) return;

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

    // =======================================================
    // Movement & Physics
    // =======================================================

    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // GetAxisRawを使用
        float v = Input.GetAxisRaw("Vertical"); // GetAxisRawを使用

        Vector3 inputDirection = new Vector3(h, 0, v).normalized;

        Quaternion cameraRotation = (_tpsCamController != null)
            ? Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0)
            : transform.rotation;

        // 目標の移動方向（ワールド空間）
        Vector3 targetMoveDirection = cameraRotation * inputDirection;

        float targetSpeed = _moveSpeed;
        bool isDashing = Input.GetKey(KeyCode.LeftShift) && currentEnergy > 0.01f;

        if (isDashing)
        {
            targetSpeed *= dashMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }

        // 目標の移動速度ベクトル
        Vector3 targetVelocity = targetMoveDirection * targetSpeed;

        // 慣性を適用 🚀
        float currentAcceleration = _controller.isGrounded ? accelerationSpeed : airAccelerationSpeed;
        float currentDeceleration = _controller.isGrounded ? decelerationSpeed : airAccelerationSpeed;

        // SmoothDampを使用して速度をスムーズに移行させる
        // 加速・減速を調整したい場合は、加速度の値を直接操作する必要がありますが、
        // 今回はシンプルにSmoothDampの時間を調整して慣性を表現します。

        // 目標速度への到達時間 (time) を計算
        float smoothTime;
        if (inputDirection.magnitude > 0.01f)
        {
            // 入力がある場合 (加速/移動)
            smoothTime = currentAcceleration;
        }
        else
        {
            // 入力がない場合 (減速/停止)
            smoothTime = currentDeceleration;
        }

        // SmoothDampで慣性移動を計算
        // _currentVelocityRef は private Vector3 _currentVelocityRef; で宣言されていることを前提
        _currentMoveVelocity = Vector3.SmoothDamp(_currentMoveVelocity, targetVelocity, ref _currentVelocityRef, smoothTime);

        // ダッシュなどでエネルギー切れになった場合、現在の慣性を維持しつつ、速度を減速させる処理を追加することもできますが、
        // 今回はシンプルにtargetSpeedが低下するため、SmoothDampが適切に減速してくれます。

        return _currentMoveVelocity;
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
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) // 降下
            {
                _velocity.y = -verticalSpeed;
                hasVerticalInput = true;
            }
        }

        if (!hasVerticalInput)
        {
            if (!isGrounded)
            {
                // 落下速度の調整
                float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
                _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
            }
        }
        else
        {
            // 上昇/降下でエネルギーを消費
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }

        // エネルギー切れで上昇を止める
        if (currentEnergy <= 0.01f && _velocity.y > 0) _velocity.y = 0;

        return new Vector3(0, _velocity.y, 0);
    }

    // =======================================================
    // Combat & Attack
    // =======================================================

    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        // ロックオンターゲットがいればそちらを向く
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) RotateTowards(GetLockOnTargetPosition(lockOnTarget));

        // ダメージ判定
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hitColliders.Where(c => c.transform != this.transform))
        {
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

        currentEnergy -= beamAttackEnergyCost;
        _lastEnergyConsumptionTime = Time.time;
        UpdateEnergyUI();

        // ターゲット方向の計算
        Vector3 origin = beamFirePoint.position;
        Vector3 fireDirection = beamFirePoint.forward;
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;

        if (lockOnTarget != null)
        {
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            fireDirection = (targetPosition - origin).normalized;
            RotateTowards(targetPosition);
        }

        // Raycastでヒット判定
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

        // ビームVFXの生成と発射
        Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection)).Fire(origin, endPoint, didHit);
    }

    void HandleAttackState()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            // 攻撃終了後の垂直速度リセット
            if (!_controller.isGrounded)
            {
                _velocity.y = 0;
            }
            else
            {
                _velocity.y = -0.1f;
            }

            // 攻撃終了時に慣性移動もリセット（プレイヤーの挙動によっては_currentMoveVelocity = Vector3.zero;も検討）
            // 今回は慣性を残すためにこのままにしておきます。
        }
    }

    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffsetIfNoCollider = false)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null) return targetCollider.bounds.center;

        if (useOffsetIfNoCollider) return target.position + Vector3.up * lockOnTargetHeightOffset;

        return target.position;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        transform.rotation = targetRotation;
    }

    /// <summary>
    /// 衝突したColliderから、該当する敵コンポーネントを探してダメージを与える。
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 💡 敵コンポーネントの列挙とダメージ適用
        // IDamageableインターフェースを実装すれば、この長いリストを短縮できます。
        if (target.TryGetComponent<TutorialEnemyController>(out var tutorialEnemy)) { tutorialEnemy.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var scorpion)) { scorpion.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var suicide)) { suicide.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var drone)) { drone.TakeDamage(damageAmount); isHit = true; }

        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.ClosestPoint(transform.position), Quaternion.identity);
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

        if (currentHP <= 0) Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        gameOverManager?.GoToGameOverScene();
        enabled = false;
    }

    // =======================================================
    // Energy & Armor
    // =======================================================

    private void HandleEnergy()
    {
        // エネルギー回復
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            float recoveryMultiplier = _currentArmorStats?.energyRecoveryMultiplier ?? 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            currentEnergy += recoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();
    }

    private void LoadAndSwitchArmor()
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);
        ArmorMode defaultMode = ArmorMode.Normal;

        if (Enum.IsDefined(typeof(ArmorMode), selectedIndex) && selectedIndex < armorConfigurations.Count)
        {
            SwitchArmor((ArmorMode)selectedIndex, false);
        }
        else
        {
            SwitchArmor(defaultMode, false);
            Debug.LogWarning($"不正なインデックス({selectedIndex})が検出されました。{defaultMode}モードを適用します。");
        }
    }

    private void SwitchArmor(ArmorMode newMode, bool shouldLog = true)
    {
        int index = (int)newMode;
        if (index < 0 || index >= armorConfigurations.Count) return; // 無効なモードは無視
        if (_currentArmorMode == newMode && _currentArmorStats != null) return;

        _currentArmorMode = newMode;
        _currentArmorStats = armorConfigurations[index];
        _moveSpeed = baseMoveSpeed * _currentArmorStats.moveSpeedMultiplier;

        PlayerPrefs.SetInt(SelectedArmorKey, index);
        PlayerPrefs.Save();

        UpdateArmorVisuals(index);
        if (shouldLog) Debug.Log($"アーマーを切り替えました: **{_currentArmorStats.name}** ");
    }

    private void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;
        UpdateWeaponUIEmphasis();
        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");
    }

    // =======================================================
    // UI & Visuals
    // =======================================================

    private void UpdateUI()
    {
        UpdateHPUI();
        UpdateEnergyUI();
        UpdateWeaponUIEmphasis();
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
                if (armorModels[i] != null) armorModels[i].SetActive(i == index);
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
        if (meleeWeaponText != null) meleeWeaponText.color = isMelee ? emphasizedColor : normalColor;
        if (beamWeaponText != null) beamWeaponText.color = isMelee ? normalColor : emphasizedColor;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null) energySlider.value = currentEnergy / maxEnergy;
    }

    void UpdateHPUI()
    {
        if (hPSlider != null) hPSlider.value = currentHP / maxHP;
        if (hPText != null)
        {
            int currentHPInt = Mathf.CeilToInt(currentHP);
            int maxHPInt = Mathf.CeilToInt(maxHP);
            hPText.text = $"{currentHPInt} / {maxHPInt}";
        }
    }

    // =======================================================
    // Editor Gizmos
    // =======================================================

    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程
        if (beamFirePoint != null)
        {
            Vector3 origin = beamFirePoint.position;
            Vector3 fireDirection = beamFirePoint.forward;
            Transform lockOnTarget = _tpsCamController?.LockOnTarget;

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