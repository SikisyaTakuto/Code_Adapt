using System.Collections;
using UnityEngine;

public class SpeedController : MonoBehaviour
{
    [Header("Dependencies")]
    private CharacterController _playerController;
    private TPSCameraController _tpsCamController;
    public PlayerModesAndVisuals _modesAndVisuals;
    public PlayerStatus playerStatus;

    [Header("Vfx & Layers")]
    public BeamController beamPrefab;
    public Transform[] beamFirePoints;
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

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

    private bool _isAttacking = false;
    private bool _isStunned = false;
    private bool _isRestoringRotation = false;
    private float _stunTimer = 0.0f;
    private Quaternion _rotationBeforeAttack;
    private Vector3 _velocity;
    private float _moveSpeed;
    private bool _wasGrounded = false;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip meleeSwingSound;
    public AudioClip scratchAttackSound;

    private float _debuffMoveMultiplier = 1.0f;
    private float _debuffJumpMultiplier = 1.0f;

    void Awake() { InitializeComponents(); }

    void Update()
    {
        if (playerStatus == null || playerStatus.IsDead) return;
        bool isGroundedNow = _playerController.isGrounded;

        HandleStunState(isGroundedNow);

        if (_isStunned || _isAttacking || _isRestoringRotation)
        {
            HandleStunnedVerticalMovement(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        if (_tpsCamController != null)
        {
            if (_tpsCamController.LockOnTarget == null) _tpsCamController.RotatePlayerToCameraDirection();
            else RotateTowards(GetLockOnTargetPosition(_tpsCamController.LockOnTarget));
        }

        ApplyArmorStats();
        HandleInput();
        Vector3 finalMove = HandleVerticalMovement(isGroundedNow) + HandleHorizontalMovement();
        _playerController.Move(finalMove * Time.deltaTime);
        _wasGrounded = isGroundedNow;
    }

    private void InitializeComponents()
    {
        _playerController = GetComponentInParent<CharacterController>();
        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (playerStatus == null) playerStatus = GetComponentInParent<PlayerStatus>();
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

    private IEnumerator RestoreRotationRoutine(Quaternion targetRot)
    {
        _isRestoringRotation = true;
        float elapsed = 0f;
        float duration = 0.2f;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRot;
        _isRestoringRotation = false;
    }

    private void PerformAttack()
    {
        bool isAttack2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);

        // 攻撃前の回転を保存（終了後にここに戻る）
        _rotationBeforeAttack = transform.rotation;

        var speedAnim = GetComponentInChildren<SpeedAnimation>(true);
        if (speedAnim != null && speedAnim.gameObject.activeInHierarchy)
            speedAnim.PlayAttackAnimation(isAttack2);

        if (isAttack2) StartCoroutine(HandleSpeedCrouchBeamRoutine());
        else StartCoroutine(HandleSpeedMeleeRoutine());
    }

    private IEnumerator HandleSpeedMeleeRoutine()
    {
        Animator anim = GetComponentInChildren<Animator>();
        _isAttacking = true;
        if (anim != null) anim.applyRootMotion = false;

        StartAttackStun();
        _stunTimer = 1.5f;

        // ★オートエイムを削除し、現在の「正面」を維持して攻撃
        Quaternion attackRotation = transform.rotation;

        for (int i = 0; i < 3; i++)
        {
            PlaySound(meleeSwingSound);
            ApplyMeleeSphereDamage(meleeDamage);

            float stepTimer = 0;
            while (stepTimer < 0.25f)
            {
                transform.rotation = attackRotation; // 向きを固定
                stepTimer += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.1f);
        PlaySound(scratchAttackSound);
        ApplyMeleeSphereDamage(meleeDamage * 1.5f);

        float backstepTime = 0.25f;
        float timer = 0f;
        while (timer < backstepTime)
        {
            transform.rotation = attackRotation;
            _playerController.Move(-transform.forward * 12f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.applyRootMotion = true;
        yield return StartCoroutine(RestoreRotationRoutine(_rotationBeforeAttack));
        _isAttacking = false;
        _isStunned = false;
    }

    private IEnumerator HandleSpeedCrouchBeamRoutine()
    {
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) yield break;

        Animator anim = GetComponentInChildren<Animator>();
        _isAttacking = true;
        if (anim != null) anim.applyRootMotion = false;

        StartAttackStun();
        _stunTimer = 1.0f;

        Quaternion attackRotation = transform.rotation;

        float prepTimer = 0;
        while (prepTimer < 0.3f)
        {
            transform.rotation = attackRotation;
            prepTimer += Time.deltaTime;
            yield return null;
        }

        // ビーム発射：引数を削除し、正面に撃つように修正
        ExecuteBeamLogic();

        float remainTimer = 0;
        while (remainTimer < 0.7f)
        {
            transform.rotation = attackRotation;
            remainTimer += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.applyRootMotion = true;
        yield return StartCoroutine(RestoreRotationRoutine(_rotationBeforeAttack));
        _isAttacking = false;
        _isStunned = false;
    }

    // 引数なし版のExecuteBeamLogic（正面またはロックオン先に撃つ）
    private void ExecuteBeamLogic()
    {
        if (beamFirePoints == null || beamPrefab == null) return;

        // ロックオン中ならその方向、そうでなければ正面
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPos = lockOnTarget != null
            ? GetLockOnTargetPosition(lockOnTarget, true)
            : transform.position + transform.forward * beamMaxDistance;

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;
            Vector3 fireDirection = (targetPos - origin).normalized;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    // --- 以降、補助関数 ---
    private void PlaySound(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
    private void ApplyMeleeSphereDamage(float damage)
    {
        Vector3 detectionCenter = transform.position + (Vector3.up * 1.0f) + (transform.forward * 1.5f);
        Collider[] hitColliders = Physics.OverlapSphere(detectionCenter, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform.root == transform.root) continue;
            ApplyDamageToEnemy(hitCollider, damage);
        }
    }

    private Vector3 HandleHorizontalMovement()
    {
        if (_isStunned || _isRestoringRotation) return Vector3.zero;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h == 0f && v == 0f) return Vector3.zero;
        Vector3 moveDir = (_tpsCamController != null)
            ? Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0) * new Vector3(h, 0, v)
            : (transform.right * h + transform.forward * v);
        float currentSpeed = (Input.GetKey(KeyCode.LeftShift) && playerStatus.currentEnergy > 0.1f) ? _moveSpeed * dashMultiplier : _moveSpeed;
        currentSpeed *= _debuffMoveMultiplier;
        if (currentSpeed > _moveSpeed) playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        return moveDir.normalized * currentSpeed;
    }

    private Vector3 HandleVerticalMovement(bool isGrounded)
    {
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;
        if (_isStunned || _isRestoringRotation) return Vector3.zero;
        bool isFlyingUp = Input.GetKey(KeyCode.Space);
        if (canFly && isFlyingUp && playerStatus.currentEnergy > 0.1f)
        {
            _velocity.y = verticalSpeed * _debuffJumpMultiplier;
            playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        }
        else if (!isGrounded)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }
        return new Vector3(0, _velocity.y, 0);
    }

    private void HandleInput()
    {
        if (_isStunned || _isRestoringRotation) return;
        if (Input.GetMouseButtonDown(0)) PerformAttack();
        if (Input.GetKeyDown(KeyCode.E)) _modesAndVisuals.SwitchWeapon();
        if (Input.GetKeyDown(KeyCode.Alpha1)) _modesAndVisuals.ChangeArmorBySlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) _modesAndVisuals.ChangeArmorBySlot(1);
    }

    private void ApplyArmorStats() { var stats = _modesAndVisuals.CurrentArmorStats; _moveSpeed = baseMoveSpeed * (stats != null ? stats.moveSpeedMultiplier : 1.0f); }
    private void RotateTowards(Vector3 target) { Vector3 dir = (target - transform.position).normalized; transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)); }
    private Vector3 GetLockOnTargetPosition(Transform t, bool offset = false) { if (t.TryGetComponent<Collider>(out var c)) return c.bounds.center; return offset ? t.position + Vector3.up * lockOnTargetHeightOffset : t.position; }

    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject t = hitCollider.gameObject;
        bool isHit = false;
        if (t.GetComponentInParent<SoldierMoveEnemy>()) { t.GetComponentInParent<SoldierMoveEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<SoliderEnemy>()) { t.GetComponentInParent<SoliderEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<TutorialEnemyController>()) { t.GetComponentInParent<TutorialEnemyController>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<ScorpionEnemy>()) { t.GetComponentInParent<ScorpionEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<SuicideEnemy>()) { t.GetComponentInParent<SuicideEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<DroneEnemy>()) { t.GetComponentInParent<DroneEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<VoxBodyPart>()) { t.GetComponentInParent<VoxBodyPart>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<VoxPart>()) { t.GetComponentInParent<VoxPart>().TakeDamage(damageAmount); isHit = true; }
        if (isHit && hitEffectPrefab != null) Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 previewCenter = transform.position + (Vector3.up * 1.0f) + (transform.forward * 1.5f);
        Gizmos.DrawWireSphere(previewCenter, meleeAttackRange);
    }

    public void TakeDamage(float amount) { float def = (_modesAndVisuals.CurrentArmorStats != null) ? _modesAndVisuals.CurrentArmorStats.defenseMultiplier : 1.0f; playerStatus.TakeDamage(amount, def); }
    public void SetDebuff(float moveMult, float jumpMult) { _debuffMoveMultiplier = moveMult; _debuffJumpMultiplier = jumpMult; }
    public void ResetDebuff() { _debuffMoveMultiplier = 1.0f; _debuffJumpMultiplier = 1.0f; }
}