using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// プレイヤーの移動、エネルギー管理、攻撃、およびアーマー制御を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
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

    private CharacterController _controller;
    // TPSCameraControllerの参照を保持
    private TPSCameraController _tpsCamController;

    //UI & Visuals (変更なし)
    [Header("Armor UI & Visuals")]
    public Image currentArmorIconImage;
    public Sprite[] armorSprites;
    public GameObject[] armorModels;

    [Header("Weapon UI")]
    public Image meleeWeaponIcon;
    public Image beamWeaponIcon;
    public Color emphasizedColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f);

    // ベースとなる能力値 (変更なし)
    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f;
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

    // Armor Settings (変更なし)
    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations = new List<ArmorStats>
    {
        new ArmorStats { name = "Normal", defenseMultiplier = 1.0f, moveSpeedMultiplier = 1.0f, energyRecoveryMultiplier = 1.0f },
        new ArmorStats { name = "Buster Mode", defenseMultiplier = 1.5f, moveSpeedMultiplier = 0.8f, energyRecoveryMultiplier = 0.8f },
        new ArmorStats { name = "Speed Mode", defenseMultiplier = 0.75f, moveSpeedMultiplier = 1.5f, energyRecoveryMultiplier = 1.2f }
    };

    // 内部状態 (変更なし)
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

    // 公開プロパティ (変更なし)
    [HideInInspector] public float currentHP { get => _currentHP; private set => _currentHP = value; }
    [HideInInspector] public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;

    // HP/Energy Gauge
    [Header("Health Settings")]
    public float maxHP = 1000.0f; // 🎯 最大HPを1000に設定
    public Slider hPSlider;
    public Text hPText; // 🎯 新たに追加: HPのテキスト表示用 (UnityEngine.UI.Text)

    [Header("Energy Gauge Settings")]
    public float maxEnergy = 100.0f;
    public float recoveryDelay = 1.0f;
    public Slider energySlider;

    // Attack Settings (変更なし)
    public float attackFixedDuration = 0.8f;

    [Header("Beam VFX")]
    public BeamController beamPrefab;
    public Transform beamFirePoint;
    public float beamMaxDistance = 100f;
    [Tooltip("ロックオン時に敵のColliderがない場合、ビームを狙う高さのオフセット。")]
    public float lockOnTargetHeightOffset = 1.0f; // 🎯 新規追加: ロックオン時のオフセット

    [Header("Melee Attack Settings")]
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // チュートリアル用イベントとプロパティ (変更なし)
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    public event Action onEnergyDepleted;
    public float WASDMoveTimer { get; private set; }
    public float JumpTimer { get; private set; }
    public float DescendTimer { get; private set; }

    // 移動関連の内部変数 (変更なし)
    private Vector3 _velocity;
    private float _moveSpeed;
    public bool canReceiveInput = true;

    // ... (Awake, Start, LoadAndSwitchArmor メソッドは変更なし) ...
    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        currentHP = maxHP;

        LoadAndSwitchArmor();
        UpdateHPUI(); // UI更新の呼び出し
        UpdateEnergyUI();
        UpdateWeaponUIEmphasis();

        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();
            if (gameOverManager == null)
            {
                Debug.LogWarning("SceneBasedGameOverManagerがInspectorで設定されていません。シーンから取得もできませんでした。Die()時にエラーが発生する可能性があります。");
            }
        }

        Debug.Log($"初期武器: {currentWeaponMode} | 初期アーマー: {currentArmorMode}");
    }

    private void InitializeComponents()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            Debug.LogError($"{nameof(PlayerController)}: CharacterControllerが見つかりません。");
            enabled = false;
            return;
        }

        // 🎯 修正: FindObjectOfType<TPSCameraController>() で参照を取得
        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (_tpsCamController == null)
        {
            Debug.LogWarning($"{nameof(PlayerController)}: TPSCameraControllerが見つかりません。ビームのロックオン機能は無効になります。");
            // FindObjectOfTypeは負荷が高いため、理想的にはStart()かInspectorで設定するのが望ましい
        }
    }

    private void LoadAndSwitchArmor()
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);

        if (Enum.IsDefined(typeof(ArmorMode), selectedIndex) && selectedIndex < armorConfigurations.Count)
        {
            ArmorMode initialMode = (ArmorMode)selectedIndex;
            SwitchArmor(initialMode, false);
        }
        else
        {
            SwitchArmor(ArmorMode.Normal, false);
            Debug.LogWarning($"不正なアーマーインデックス({selectedIndex})が検出されました。Normalモードを適用します。");
        }
    }

    void Update()
    {
        HandleTestInput();

        if (_isDead) return;

        if (!canReceiveInput || _isAttacking)
        {
            HandleAttackState();
            WASDMoveTimer = JumpTimer = DescendTimer = 0f;
            _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);
        }
        else
        {
            // ロックオン中はTPSCameraControllerがプレイヤーの回転を制御します
            if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
            {
                _tpsCamController?.RotatePlayerToCameraDirection();
            }

            HandleAttackInputs();
            HandleWeaponSwitchInput();
            HandleArmorSwitchInput();

            HandleEnergy();

            Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
            _controller.Move(finalMove * Time.deltaTime);
        }
    }

    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.LogWarning("Pキーが押されました: HPを0にして死亡処理を実行します。");
            currentHP = 0;
            UpdateHPUI();
            Die();
        }
    }
    // ... (他の移動、アーマー切り替えメソッドは変更なし) ...
    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
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

    private void UpdateArmorVisuals(int index)
    {
        if (currentArmorIconImage != null && armorSprites != null && index < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[index];
            currentArmorIconImage.enabled = true;
        }

        if (armorModels != null && armorModels.Length > 0)
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

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchWeapon();
        }
    }

    private void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;

        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");
        UpdateWeaponUIEmphasis();
    }

    private void UpdateWeaponUIEmphasis()
    {
        if (meleeWeaponIcon == null || beamWeaponIcon == null)
        {
            return;
        }

        bool isMelee = (_currentWeaponMode == WeaponMode.Melee);

        meleeWeaponIcon.color = isMelee ? emphasizedColor : normalColor;
        beamWeaponIcon.color = isMelee ? normalColor : emphasizedColor;
    }

    private Vector3 HandleHorizontalMovement()
    {
        if (_isAttacking) return Vector3.zero;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (h == 0f && v == 0f)
        {
            WASDMoveTimer = 0f;
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
        bool isConsumingEnergy = false;

        bool isBoosting = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && currentEnergy > 0.01f;

        if (isBoosting)
        {
            currentSpeed *= boostMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }

        Vector3 horizontalMove = moveDirection * currentSpeed;

        WASDMoveTimer += Time.deltaTime;

        if (isConsumingEnergy) _lastEnergyConsumptionTime = Time.time;

        return horizontalMove;
    }

    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool hasVerticalInput = false;

        if (canFly && currentEnergy > 0.01f && !_isAttacking)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                _velocity.y = verticalSpeed;
                JumpTimer += Time.deltaTime;
                DescendTimer = 0f;
                hasVerticalInput = true;
            }
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
            if (!isGrounded && !_isAttacking)
            {
                _velocity.y += gravity * Time.deltaTime;
            }
        }
        else
        {
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }

        if (currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
            JumpTimer = DescendTimer = 0f;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    private void HandleAttackInputs()
    {
        if (Input.GetMouseButtonDown(0) && !_isAttacking)
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

    /// <summary>
    /// 衝突したColliderから、該当する敵コンポーネントを探してダメージを与える。
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        // ターゲットのGameObjectを取得
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 1. ScorpionEnemyを試す
        ScorpionEnemy scorpion = target.GetComponent<ScorpionEnemy>();
        if (scorpion != null)
        {
            // TakeDamageを呼び出す
            scorpion.TakeDamage(damageAmount);
            Debug.Log($"ScorpionEnemyにダメージ: {damageAmount}");
            isHit = true;
        }

        // 2. SuicideEnemyを試す
        SuicideEnemy suicide = target.GetComponent<SuicideEnemy>();
        if (suicide != null)
        {
            // TakeDamageを呼び出す
            suicide.TakeDamage(damageAmount);
            Debug.Log($"SuicideEnemyにダメージ: {damageAmount}");
            isHit = true;
        }

        // 3. DroneEnemyを試す
        DroneEnemy drone = target.GetComponent<DroneEnemy>();
        if (drone != null)
        {
            // TakeDamageを呼び出す
            drone.TakeDamage(damageAmount);
            Debug.Log($"DroneEnemyにダメージ: {damageAmount}");
            isHit = true;
        }

        // 共通のヒットエフェクト処理
        if (isHit && hitEffectPrefab != null)
        {
            // ヒットした場所にエフェクトを生成
            Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
        }
    }

    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            // プレイヤー自身を除外
            if (hitCollider.transform == this.transform) continue;

            // 🎯 修正: 個別の敵コンポーネントを判別してダメージを与える
            ApplyDamageToEnemy(hitCollider, meleeDamage);

            // エフェクト生成は ApplyDamageToEnemy 内に移動
        }

        onMeleeAttackPerformed?.Invoke();
    }

    // =======================================================
    // 🎯 修正された HandleBeamAttack メソッド
    // =======================================================
    private void HandleBeamAttack()
    {
        if (currentEnergy < beamAttackEnergyCost)
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        if (beamFirePoint == null || beamPrefab == null)
        {
            Debug.LogError("ビームの発射点(BeamFirePoint)またはビームのプレハブ(BeamPrefab)が設定されていません。");
            return;
        }

        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f;

        currentEnergy -= beamAttackEnergyCost;
        _lastEnergyConsumptionTime = Time.time;
        UpdateEnergyUI();

        Vector3 origin = beamFirePoint.position;
        Vector3 fireDirection;
        Transform lockOnTarget = null;

        // 1. ロックオンターゲットの確認
        if (_tpsCamController != null)
        {
            lockOnTarget = _tpsCamController.LockOnTarget;
        }


        if (lockOnTarget != null)
        {
            // --- ロックオン中: ターゲットを狙う ---
            Vector3 targetPosition;

            // 敵のColliderがあれば、その中心を狙う (より正確な狙い)
            Collider targetCollider = lockOnTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetPosition = targetCollider.bounds.center;
            }
            else
            {
                // Colliderがなければ、デフォルトの高さオフセットを適用
                targetPosition = lockOnTarget.position + Vector3.up * lockOnTargetHeightOffset;
            }

            fireDirection = (targetPosition - origin).normalized;

            // プレイヤーをターゲットの水平方向に向ける
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(fireDirection.x, 0, fireDirection.z));
            transform.rotation = targetRotation;

            Debug.Log($"ビーム発射！ロックオンターゲット: {lockOnTarget.name} に向けて発射。");
        }
        else
        {
            // --- 通常時: 銃口の向いている方向を向く ---
            fireDirection = beamFirePoint.forward;
            Debug.Log("ビーム発射！正面に向けて発射。");
        }
        // ===========================================

        RaycastHit hit;
        Vector3 endPoint;
        bool didHit = false;

        // Raycastで衝突をチェック
        // 今回はビームの視覚化のために全てのレイヤーをチェック (~0)
        if (Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0))
        {
            endPoint = hit.point;
            didHit = true;

            // ダメージ判定を実行 
            ApplyDamageToEnemy(hit.collider, beamDamage);
        }
        else
        {
            endPoint = origin + fireDirection * beamMaxDistance;
        }

        BeamController beamInstance = Instantiate(
            beamPrefab,
            origin,
            // 発射方向に向けてビームの回転を設定
            Quaternion.LookRotation(fireDirection)
        );
        // BeamControllerに始点と終点を渡し、ビジュアルを更新
        beamInstance.Fire(origin, endPoint, didHit);

        onBeamAttackPerformed?.Invoke();
    }

    void HandleAttackState()
    {
        if (!_isAttacking) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            if (!_controller.isGrounded)
            {
                _velocity.y = 0;
            }
            else
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

    // チュートリアル・UI関連のメソッド

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

    /// <summary>HPスライダーとHPテキストを更新する。UI更新は専用メソッドに集約</summary>
    void UpdateHPUI()
    {
        // 1. スライダーの更新
        if (hPSlider != null)
        {
            hPSlider.value = currentHP / maxHP;
        }

        // 2. 🎯 HPテキストの更新
        if (hPText != null)
        {
            // HPの値を整数に丸めて表示（小数点を非表示）
            int currentHPInt = Mathf.CeilToInt(currentHP);
            int maxHPInt = Mathf.CeilToInt(maxHP);

            // 「現在のHP / 最大HP」形式の文字列をセット
            hPText.text = $"{currentHPInt} / {maxHPInt}";
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
        UpdateHPUI(); // HPが変化したらUIを更新

        Debug.Log($"ダメージを受けました。残りHP: {currentHP} (元のダメージ: {damageAmount}, 最終ダメージ: {finalDamage})");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        canReceiveInput = false;

        Debug.Log("プレイヤーは破壊されました。ゲームオーバー処理をマネージャーに委譲します。");

        if (gameOverManager != null)
        {
            gameOverManager.GoToGameOverScene();
        }
        else
        {
            Debug.LogError("SceneBasedGameOverManagerが設定されていません。Inspectorを確認してください。");
        }

        enabled = false;
    }
    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程
        if (beamFirePoint != null)
        {
            Vector3 origin = beamFirePoint.position;
            Vector3 direction = beamFirePoint.forward;

            // 🎯 Gizmos表示の際もロックオン状態を確認して方向を決定
            Vector3 fireDirection = beamFirePoint.forward;
            Transform lockOnTarget = _tpsCamController != null ? _tpsCamController.LockOnTarget : null;

            if (lockOnTarget != null)
            {
                // ロックオンターゲットの中心を狙う
                Collider targetCollider = lockOnTarget.GetComponent<Collider>();
                Vector3 targetPosition = targetCollider != null
                    ? targetCollider.bounds.center
                    : lockOnTarget.position + Vector3.up * lockOnTargetHeightOffset;

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