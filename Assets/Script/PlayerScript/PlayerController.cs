using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq; // LINQは念のため残します

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ============================= Enums & Consts ==============================
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 }
    private const string SelectedArmorKey = "SelectedArmorIndex";
    private const float GroundSnapVelocity = -0.1f;

    [System.Serializable]
    public class ArmorStats
    {
        public string name;
        [Tooltip("ダメージ軽減率")] public float defenseMultiplier = 1.0f;
        [Tooltip("移動速度補正")] public float moveSpeedMultiplier = 1.0f;
        [Tooltip("エネルギー回復補正")] public float energyRecoveryMultiplier = 1.0f;
    }

    // ========================== Public Fields (Settings) ==========================
    [Header("1. Armor & Visuals")]
    public List<ArmorStats> armorConfigurations;
    public Image currentArmorIconImage;
    public Sprite[] armorSprites;
    public GameObject[] armorModels;

    [Header("2. Core Stats & Movement")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
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

    // =========================== Private / Cached Variables ===========================
    private CharacterController _controller;
    private TPSCameraController _tpsCamController;
    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;
    private float _lastEnergyConsumptionTime;
    private bool _isDead = false;

    private Vector3 _velocity; // 垂直方向の速度
    private float _moveSpeed; // 最終的な水平移動速度

    // Public Getters (短縮)
    private float _currentHP;
    private float _currentEnergy;
    [HideInInspector] public float currentHP { get => _currentHP; private set => _currentHP = value; }
    [HideInInspector] public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;

    // =============================== Unity Lifecycle ===============================

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (_controller == null) { Debug.LogError("CharacterControllerが見つかりません。"); enabled = false; }
    }

    void Start()
    {
        _currentEnergy = maxEnergy;
        _currentHP = maxHP;
        LoadAndSwitchArmor();
        UpdateUI();
        if (gameOverManager == null) gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();
    }

    void Update()
    {
        if (_isDead) return;

        if (_isAttacking)
        {
            HandleAttackState();
            if (!_controller.isGrounded) _velocity.y += gravity * Time.deltaTime;
            _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);
            return;
        }

        HandleRotation();
        HandleInput();
        HandleEnergy();

        Vector3 horizontalMove = HandleHorizontalMovement();
        Vector3 verticalMove = HandleVerticalMovement();

        _controller.Move((horizontalMove + verticalMove) * Time.deltaTime);
    }

    // =============================== Input & Movement ===============================

    /// <summary>カメラ方向へのプレイヤー回転を処理</summary>
    private void HandleRotation()
    {
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
            _tpsCamController?.RotatePlayerToCameraDirection();
    }

    private void HandleInput()
    {
        HandleAttackInputs();
        if (Input.GetKeyDown(KeyCode.E)) SwitchWeapon();
        HandleArmorSwitchInput();
    }

    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
    }

    /// <summary>慣性なしの即時的な水平移動を計算</summary>
    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(h, 0, v).normalized;

        Quaternion camRotation = (_tpsCamController != null)
            ? Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0) : transform.rotation;
        Vector3 targetMoveDirection = camRotation * inputDirection;

        float targetSpeed = _moveSpeed;
        bool isDashing = Input.GetKey(KeyCode.LeftShift) && _currentEnergy > 0.01f;

        if (isDashing)
        {
            targetSpeed *= dashMultiplier;
            _currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }

        return targetMoveDirection * targetSpeed;
    }

    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _velocity.y < 0) _velocity.y = GroundSnapVelocity;

        bool hasVerticalInput = false;

        if (canFly && _currentEnergy > 0.01f)
        {
            if (Input.GetKey(KeyCode.Space)) { _velocity.y = verticalSpeed; hasVerticalInput = true; }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) { _velocity.y = -verticalSpeed; hasVerticalInput = true; }
        }

        if (hasVerticalInput)
        {
            _currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }
        else if (!isGrounded)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }

        if (_currentEnergy <= 0.01f && _velocity.y > 0) _velocity.y = 0;

        return new Vector3(0, _velocity.y, 0);
    }

    // ================================== Combat ==================================

    private void HandleAttackInputs()
    {
        if (_isAttacking || !Input.GetMouseButtonDown(0)) return;
        if (_currentWeaponMode == WeaponMode.Melee) HandleMeleeAttack();
        else HandleBeamAttack();
    }

    private void HandleAttackState()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;
            _velocity.y = _controller.isGrounded ? GroundSnapVelocity : 0f;
        }
    }

    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) RotateTowards(GetLockOnTargetPosition(lockOnTarget));

        // ダメージ判定
        Collider[] hits = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hits.Where(c => c.transform != this.transform))
        {
            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    private void HandleBeamAttack()
    {
        if (_currentEnergy < beamAttackEnergyCost) { Debug.LogWarning("エネルギー不足"); return; }
        if (beamFirePoint == null || beamPrefab == null) { Debug.LogError("ビーム設定不足"); return; }

        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f;

        _currentEnergy -= beamAttackEnergyCost;
        _lastEnergyConsumptionTime = Time.time;
        UpdateEnergyUI();

        Vector3 origin = beamFirePoint.position;
        Vector3 fireDirection = beamFirePoint.forward;
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;

        if (lockOnTarget != null)
        {
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            fireDirection = (targetPosition - origin).normalized;
            RotateTowards(targetPosition);
        }

        RaycastHit hit;
        Vector3 endPoint;
        bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

        if (didHit) { endPoint = hit.point; ApplyDamageToEnemy(hit.collider, beamDamage); }
        else endPoint = origin + fireDirection * beamMaxDistance;

        Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection)).Fire(origin, endPoint, didHit);
    }

    /// <summary>ターゲットの中心座標を取得</summary>
    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffset = false)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null) return targetCollider.bounds.center;
        return useOffset ? target.position + Vector3.up * lockOnTargetHeightOffset : target.position;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = targetRotation;
    }

    /// <summary>敵にダメージを与える</summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // IDamageableインターフェースがあれば、以下の冗長なチェックを排除できます。
        if (target.TryGetComponent<TutorialEnemyController>(out var c1)) { c1.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var c2)) { c2.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var c3)) { c3.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var c4)) { c4.TakeDamage(damageAmount); isHit = true; }

        if (isHit && hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitCollider.ClosestPoint(transform.position), Quaternion.identity);
    }

    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        float finalDamage = damageAmount * (_currentArmorStats?.defenseMultiplier ?? 1.0f);
        _currentHP = Mathf.Clamp(_currentHP - finalDamage, 0, maxHP);
        UpdateHPUI();

        if (_currentHP <= 0) Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        gameOverManager?.GoToGameOverScene();
        enabled = false;
    }

    // =========================== Energy & Armor Management ===========================

    private void HandleEnergy()
    {
        // エネルギー回復
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            float recoveryMultiplier = _currentArmorStats?.energyRecoveryMultiplier ?? 1.0f;
            _currentEnergy += energyRecoveryRate * recoveryMultiplier * Time.deltaTime;
        }

        _currentEnergy = Mathf.Clamp(_currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();
    }

    private void LoadAndSwitchArmor()
    {
        int index = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);
        if (Enum.IsDefined(typeof(ArmorMode), index) && index < armorConfigurations.Count)
        {
            SwitchArmor((ArmorMode)index, false);
        }
        else
        {
            SwitchArmor(ArmorMode.Normal, false);
            Debug.LogWarning($"不正なインデックス({index})。Normalモードを適用。");
        }
    }

    private void SwitchArmor(ArmorMode newMode, bool shouldLog = true)
    {
        int index = (int)newMode;
        if (index < 0 || index >= armorConfigurations.Count) return;
        if (_currentArmorMode == newMode && _currentArmorStats != null) return;

        _currentArmorMode = newMode;
        _currentArmorStats = armorConfigurations[index];
        _moveSpeed = baseMoveSpeed * _currentArmorStats.moveSpeedMultiplier;

        PlayerPrefs.SetInt(SelectedArmorKey, index);
        PlayerPrefs.Save();

        UpdateArmorVisuals(index);
        if (shouldLog) Debug.Log($"アーマー切替: **{_currentArmorStats.name}** ");
    }

    private void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;
        UpdateWeaponUIEmphasis();
        Debug.Log($"武器切替: **{_currentWeaponMode}**");
    }

    // =================================== UI & Visuals ===================================

    private void UpdateUI()
    {
        UpdateHPUI();
        UpdateEnergyUI();
        UpdateWeaponUIEmphasis();
        // UpdateArmorVisualsはSwitchArmorで実行済み
    }

    private void UpdateArmorVisuals(int index)
    {
        if (currentArmorIconImage != null && armorSprites != null && index < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[index];
            currentArmorIconImage.enabled = true;
        }

        for (int i = 0; i < armorModels?.Length; i++)
        {
            if (armorModels[i] != null) armorModels[i].SetActive(i == index);
        }
    }

    private void UpdateWeaponUIEmphasis()
    {
        bool isMelee = (_currentWeaponMode == WeaponMode.Melee);

        // アイコンの色を更新 (短縮)
        if (meleeWeaponIcon != null) meleeWeaponIcon.color = isMelee ? emphasizedColor : normalColor;
        if (beamWeaponIcon != null) beamWeaponIcon.color = isMelee ? normalColor : emphasizedColor;

        // テキストを更新
        if (meleeWeaponText != null) meleeWeaponText.color = isMelee ? emphasizedColor : normalColor;
        if (beamWeaponText != null) beamWeaponText.color = isMelee ? normalColor : emphasizedColor;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null) energySlider.value = _currentEnergy / maxEnergy;
    }

    void UpdateHPUI()
    {
        if (hPSlider != null) hPSlider.value = _currentHP / maxHP;

        if (hPText != null)
        {
            hPText.text = $"{Mathf.CeilToInt(_currentHP)} / {Mathf.CeilToInt(maxHP)}";
        }
    }

    // ================================ Editor Gizmos ===============================

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