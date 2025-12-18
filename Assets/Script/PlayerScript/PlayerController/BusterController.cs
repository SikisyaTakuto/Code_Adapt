using UnityEngine;
using UnityEngine.InputSystem;

public class BusterController : MonoBehaviour
{
    // =======================================================
    // 依存コンポーネント / 関連オブジェクト
    // =======================================================

    [Header("Dependencies")]
    private CharacterController _playerController;
    private TPSCameraController _tpsCamController;
    public PlayerModesAndVisuals _modesAndVisuals;

    // ★追加: 共通ステータス管理への参照
    public PlayerStatus playerStatus;

    [Header("Vfx & Layers")]
    public BeamController beamPrefab;
    public Transform[] beamFirePoints;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // =======================================================
    // 移動・攻撃設定 (ステータス以外)
    // =======================================================

    [Header("Movement Settings")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
    public float verticalSpeed = 10.0f;
    public float gravity = -9.81f;
    public bool canFly = true;
    public float fastFallMultiplier = 3.0f;

    [Header("Stun & Hardening Settings")]
    public float attackFixedDuration = 0.8f;
    public float landStunDuration = 0.2f;

    [Header("Attack Settings")]
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;

    // HP, Energy, UI, 死亡フラグに関する変数は PlayerStatus へ移動したため削除

    // =======================================================
    // プライベート/キャッシュ変数
    // =======================================================

    private bool _isAttacking = false;
    private bool _isStunned = false;
    private float _stunTimer = 0.0f;

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
        InitializeComponents();
    }

    void Update()
    {
        // ★PlayerStatusの死亡判定を参照
        if (playerStatus == null || playerStatus.IsDead) return;

        bool isGroundedNow = _playerController.isGrounded;

        // 1. 硬直状態の処理
        HandleStunState(isGroundedNow);

        if (_isStunned)
        {
            HandleStunnedVerticalMovement(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // 2. プレイヤーの向き制御
        if (_tpsCamController == null || _tpsCamController.LockOnTarget == null)
        {
            _tpsCamController?.RotatePlayerToCameraDirection();
        }

        // 3. 移動計算
        ApplyArmorStats();
        // HandleEnergy() は PlayerStatus 側で自動実行されるため削除

        HandleInput();
        Vector3 finalMove = HandleVerticalMovement(isGroundedNow) + HandleHorizontalMovement();
        _playerController.Move(finalMove * Time.deltaTime);

        _wasGrounded = isGroundedNow;
    }

    private void InitializeComponents()
    {
        _playerController = GetComponentInParent<CharacterController>();
        _tpsCamController = FindObjectOfType<TPSCameraController>();

        // ★親オブジェクトからPlayerStatusを探す
        if (playerStatus == null)
        {
            playerStatus = GetComponentInParent<PlayerStatus>();
        }
    }

    // =======================================================
    // 硬直 (Stun) 制御
    // =======================================================

    public void StartLandingStun()
    {
        if (_isStunned) return;
        _isStunned = true;
        _stunTimer = landStunDuration;
        _velocity = Vector3.zero;
    }

    public void StartAttackStun()
    {
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = attackFixedDuration;
        _velocity.y = 0f;
    }

    private void HandleStunState(bool isGrounded)
    {
        if (!_isStunned) return;
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0.0f)
        {
            _isStunned = false;
            _isAttacking = false;
            if (isGrounded) _velocity.y = -0.1f;
        }
    }

    private void HandleStunnedVerticalMovement(bool isGroundedNow)
    {
        if (!isGroundedNow)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }
        else _velocity.y = -0.1f;
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

        Vector3 moveDirection;
        if (_tpsCamController != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraRotation * new Vector3(h, 0, v);
        }
        else moveDirection = (transform.right * h + transform.forward * v);

        moveDirection.Normalize();

        float currentSpeed = _moveSpeed;

        // ★エネルギーチェックをPlayerStatusに委譲
        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || _isBoosting) && playerStatus.currentEnergy > 0.1f;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        }

        return moveDirection * currentSpeed;
    }

    private Vector3 HandleVerticalMovement(bool isGrounded)
    {
        if (!_wasGrounded && isGrounded && _velocity.y < -0.1f && !_isStunned)
        {
            StartLandingStun();
            return Vector3.zero;
        }

        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;
        if (_isStunned) return Vector3.zero;

        bool isFlyingUp = (Input.GetKey(KeyCode.Space) || _verticalInput > 0.5f);
        // bool isFlyingDown = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || _verticalInput < -0.5f);
        bool hasVerticalInput = false;

        // ★飛行エネルギーチェックを委譲
        if (canFly && playerStatus.currentEnergy > 0.1f)
        {
            if (isFlyingUp) { _velocity.y = verticalSpeed; hasVerticalInput = true; }
            //else if (isFlyingDown) { _velocity.y = -verticalSpeed; hasVerticalInput = true; }
        }

        if (hasVerticalInput) playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        else if (!isGrounded)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }

        if (playerStatus.currentEnergy <= 0.1f && _velocity.y > 0) _velocity.y = 0;

        return new Vector3(0, _velocity.y, 0);
    }

    private void ApplyArmorStats()
    {
        var stats = _modesAndVisuals.CurrentArmorStats;
        _moveSpeed = baseMoveSpeed * (stats != null ? stats.moveSpeedMultiplier : 1.0f);
    }

    // =======================================================
    // Input & Attack Logic
    // =======================================================

    private void HandleInput()
    {
        if (_isStunned) return;
        HandleAttackInputs();
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();
    }

    private void HandleAttackInputs()
    {
        if (_isAttacking || _isStunned) return;
        if (Input.GetMouseButtonDown(0)) PerformAttack();
    }

    private void PerformAttack()
    {
        switch (_modesAndVisuals.CurrentWeaponMode)
        {
            case PlayerModesAndVisuals.WeaponMode.Melee: HandleMeleeAttack(); break;
            case PlayerModesAndVisuals.WeaponMode.Beam: HandleBeamAttack(); break;
        }
    }

    private void HandleMeleeAttack()
    {
        StartAttackStun();
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) RotateTowards(GetLockOnTargetPosition(lockOnTarget));

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform != this.transform) ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    private void HandleBeamAttack()
    {
        // ★エネルギー消費を委譲
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) return;

        if (beamFirePoints == null || beamFirePoints.Length == 0 || beamPrefab == null) return;

        StartAttackStun();
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;

        if (isLockedOn)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            RotateTowards(targetPosition);
        }

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;
            Vector3 fireDirection = isLockedOn ? (targetPosition - origin).normalized : firePoint.forward;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 既存の敵判定ロジック
        if (target.TryGetComponent<SoldierMoveEnemy>(out var s1)) { s1.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SoliderEnemy>(out var s2)) { s2.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<TutorialEnemyController>(out var s3)) { s3.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var s4)) { s4.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var s5)) { s5.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var s6)) { s6.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<VoxController>(out var vox))
        {
            vox.TakeDamage(damageAmount);
            isHit = true;
        }
        // ★追加：ボスのパーツ（アームなど）へのヒット
        else if (target.TryGetComponent<VoxPart>(out var part))
        {
            part.TakeDamage(damageAmount);
            isHit = true;
        }

        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // ★ダメージ計算をPlayerStatusに委譲
        var stats = _modesAndVisuals.CurrentArmorStats;
        float defense = (stats != null) ? stats.defenseMultiplier : 1.0f;
        playerStatus.TakeDamage(damageAmount, defense);
    }

    // =======================================================
    // Utilities & Input System Events
    // =======================================================

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
    }

    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffsetIfNoCollider = false)
    {
        if (target.TryGetComponent<Collider>(out var col)) return col.bounds.center;
        return useOffsetIfNoCollider ? target.position + Vector3.up * lockOnTargetHeightOffset : target.position;
    }

    private void HandleWeaponSwitchInput() { if (Input.GetKeyDown(KeyCode.E)) _modesAndVisuals.SwitchWeapon(); }
    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) _modesAndVisuals.SwitchArmor(PlayerModesAndVisuals.ArmorMode.Speed);
    }

    public void OnFlyUp(InputAction.CallbackContext ctx) { if (!playerStatus.IsDead && !_isStunned) _verticalInput = ctx.performed ? 1f : 0f; }
    public void OnFlyDown(InputAction.CallbackContext ctx) { if (!playerStatus.IsDead && !_isStunned) _verticalInput = ctx.performed ? -1f : 0f; }
    public void OnBoost(InputAction.CallbackContext ctx) { if (!playerStatus.IsDead && !_isStunned) _isBoosting = ctx.performed; }
    public void OnAttack(InputAction.CallbackContext ctx) { if (!playerStatus.IsDead && !_isStunned && ctx.started && !_isAttacking) PerformAttack(); }
    public void OnWeaponSwitch(InputAction.CallbackContext ctx) { if (!playerStatus.IsDead && !_isStunned && ctx.started) _modesAndVisuals.SwitchWeapon(); }
    public void OnDPad(InputAction.CallbackContext context) { /* 既存のDPad処理 */ }

}