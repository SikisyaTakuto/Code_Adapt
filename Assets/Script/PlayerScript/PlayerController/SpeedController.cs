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
    private float _stunTimer = 0.0f;
    private Quaternion _rotationBeforeAttack;
    private Vector3 _velocity;
    private float _moveSpeed;
    private bool _wasGrounded = false;
    private bool _isBoosting = false;
    private float _verticalInput = 0f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip meleeSwingSound;    // 3連撃の音
    public AudioClip scratchAttackSound; // ひっかきの音

    private float _debuffMoveMultiplier = 1.0f;
    private float _debuffJumpMultiplier = 1.0f;
    void Awake() { InitializeComponents(); }

    void Update()
    {
        if (playerStatus == null || playerStatus.IsDead) return;
        bool isGroundedNow = _playerController.isGrounded;

        HandleStunState(isGroundedNow);

        // ★修正: 攻撃中(硬直中)は移動・回転を一切受け付けない
        if (_isStunned || _isAttacking)
        {
            HandleStunnedVerticalMovement(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // ロックオン等の向き制御
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

    // --- 硬直制御 ---

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
    // 索敵・エイム支援ロジック (追加)
    // =======================================================

    private Vector3 GetAutoAimTargetPosition()
    {
        // 1. ロックオン中ならそのターゲットを返す（プレイヤーの操作を優先）
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) return GetLockOnTargetPosition(lockOnTarget, true);

        // 2. 非ロックオン時：常にカメラの正面を狙う（自動で敵を探さない）
        if (Camera.main != null)
        {
            // カメラの中心からレイを飛ばす
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            // 何かに当たればその地点、何もなければ最大射程（beamMaxDistance）の地点を返す
            if (Physics.Raycast(ray, out RaycastHit hit, beamMaxDistance, ~0))
            {
                return hit.point;
            }
            return ray.origin + ray.direction * beamMaxDistance;
        }

        // カメラがない場合のフォールバック（自身の正面）
        return transform.position + transform.forward * beamMaxDistance;
    }

    // =======================================================
    // 攻撃処理 (修正版)
    // =======================================================

    private void PerformAttack()
    {
        bool isAttack2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);

        // オートエイムで振り向く
        Vector3 targetPos = GetAutoAimTargetPosition();
        RotateTowards(targetPos);

        var speedAnim = GetComponentInChildren<SpeedAnimation>(true);
        if (speedAnim != null && speedAnim.gameObject.activeInHierarchy)
            speedAnim.PlayAttackAnimation(isAttack2);

        if (isAttack2) StartCoroutine(HandleSpeedCrouchBeamRoutine());
        else StartCoroutine(HandleSpeedMeleeRoutine());
    }

    // Attack1: 3連撃 + ひっかき + 下がる (Animator制御・回転復帰版)
    private IEnumerator HandleSpeedMeleeRoutine()
    {
        // 1. Animatorの取得と「攻撃前」の回転を保存
        Animator anim = GetComponentInChildren<Animator>();
        _isAttacking = true;
        _rotationBeforeAttack = transform.rotation;

        // アニメーションによって体が勝手に回転するのを防ぐため Root Motion を一時オフ
        if (anim != null) anim.applyRootMotion = false;

        float totalDuration = 1.5f;
        StartAttackStun();
        _stunTimer = totalDuration;

        // 1～3連撃のループ
        for (int i = 0; i < 3; i++)
        {
            // 各コンボの瞬間にターゲットを向き直し、その向きをこのステップの基準にする
            Vector3 currentTarget = GetAutoAimTargetPosition();
            RotateTowards(currentTarget);
            Quaternion currentStepRotation = transform.rotation;

            PlaySound(meleeSwingSound);
            ApplyMeleeSphereDamage(meleeDamage);

            // 振っている最中(0.25秒間)、アニメーションに回転を上書きされないよう強制固定
            float stepTimer = 0;
            while (stepTimer < 0.25f)
            {
                transform.rotation = currentStepRotation;
                stepTimer += Time.deltaTime;
                yield return null;
            }
        }

        // --- フィニッシュ：ひっかき攻撃 ---
        Vector3 finalTarget = GetAutoAimTargetPosition();
        RotateTowards(finalTarget);
        Quaternion scratchRotation = transform.rotation;

        yield return new WaitForSeconds(0.1f);

        PlaySound(scratchAttackSound);
        ApplyMeleeSphereDamage(meleeDamage * 1.5f);

        // 後方に下がる（向きを固定したまま移動）
        float backstepTime = 0.25f;
        float timer = 0f;
        while (timer < backstepTime)
        {
            // ひっかきの向きを維持したままバックステップ
            transform.rotation = scratchRotation;
            _playerController.Move(-transform.forward * 12f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. 攻撃終了：保存しておいた「カメラ方向」の回転に戻す
        transform.rotation = _rotationBeforeAttack;
        if (anim != null) anim.applyRootMotion = true;
        _isAttacking = false;
    }

    // Attack2: しゃがみビーム (回転保存・復帰版)
    private IEnumerator HandleSpeedCrouchBeamRoutine()
    {
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) yield break;

        Animator anim = GetComponentInChildren<Animator>();
        _isAttacking = true;
        _rotationBeforeAttack = transform.rotation;
        if (anim != null) anim.applyRootMotion = false;

        float crouchDuration = 1.0f;
        StartAttackStun();
        _stunTimer = crouchDuration;

        // ターゲットを向いて回転を固定
        Vector3 targetPos = GetAutoAimTargetPosition();
        RotateTowards(targetPos);
        Quaternion attackRotation = transform.rotation;

        // 発射前タメ（回転固定）
        float prepTimer = 0;
        while (prepTimer < 0.3f)
        {
            transform.rotation = attackRotation;
            prepTimer += Time.deltaTime;
            yield return null;
        }

        ExecuteBeamLogic(GetAutoAimTargetPosition());

        // 発射後硬直（回転固定）
        float remainTimer = 0;
        while (remainTimer < (crouchDuration - 0.3f))
        {
            transform.rotation = attackRotation;
            remainTimer += Time.deltaTime;
            yield return null;
        }

        // 終了：復帰
        transform.rotation = _rotationBeforeAttack;
        if (anim != null) anim.applyRootMotion = true;
        _isAttacking = false;
    }

    private void ExecuteBeamLogic(Vector3 targetPosition)
    {
        if (beamFirePoints == null || beamPrefab == null) return;

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;

            // すでにRotateTowardsで体は向いているが、ビームの弾道自体もターゲットへ向ける
            Vector3 fireDirection = (targetPosition - origin).normalized;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    // 効果音再生用ヘルパー
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ビーム発射の実体
    private void ExecuteBeamLogic()
    {
        if (beamFirePoints == null || beamPrefab == null) return;

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;

        if (isLockedOn)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            RotateTowards(targetPosition);
        }

        Vector3 playerForward = transform.forward;

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;
            Vector3 fireDirection = isLockedOn ? (targetPosition - origin).normalized : playerForward;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    private void ApplyMeleeSphereDamage(float damage)
    {
        // 判定の中心点を調整（高さ+1m、前方+1.5m）
        Vector3 detectionCenter = transform.position + (Vector3.up * 1.0f) + (transform.forward * 1.5f);

        // 判定の半径（meleeAttackRangeをインスペクターで3～4に広げることを推奨）
        Collider[] hitColliders = Physics.OverlapSphere(detectionCenter, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            // プレイヤー自身を除外
            if (hitCollider.transform.root == transform.root) continue;

            ApplyDamageToEnemy(hitCollider, damage);
        }
    }

    // --- その他 (Movement, Input, Utilities) ---

    private Vector3 HandleHorizontalMovement()
    {
        if (_isStunned) return Vector3.zero;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h == 0f && v == 0f) return Vector3.zero;

        Vector3 moveDir = (_tpsCamController != null)
            ? Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0) * new Vector3(h, 0, v)
            : (transform.right * h + transform.forward * v);

        float currentSpeed = (Input.GetKey(KeyCode.LeftShift) && playerStatus.currentEnergy > 0.1f)
            ? _moveSpeed * dashMultiplier : _moveSpeed;

        currentSpeed *= _debuffMoveMultiplier;

        if (currentSpeed > _moveSpeed) playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);

        return moveDir.normalized * currentSpeed;
    }

    private Vector3 HandleVerticalMovement(bool isGrounded)
    {
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;
        if (_isStunned) return Vector3.zero;

        bool isFlyingUp = Input.GetKey(KeyCode.Space);
        if (canFly && isFlyingUp && playerStatus.currentEnergy > 0.1f)
        {
            _velocity.y = verticalSpeed * _debuffJumpMultiplier;
            playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        }
        else if (!isGrounded)
        {
            _velocity.y += gravity * Time.deltaTime * ((_velocity.y < 0) ? fastFallMultiplier : 1.0f);
        }
        return new Vector3(0, _velocity.y, 0);
    }

    private void HandleInput()
    {
        if (_isStunned) return;
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
        // 当たったオブジェクトそのもの、あるいはその親からコンポーネントを探す
        GameObject t = hitCollider.gameObject;
        bool isHit = false;

        // 既存の敵リスト（親オブジェクトにスクリプトがある場合を考慮）
        if (t.GetComponentInParent<SoldierMoveEnemy>()) { t.GetComponentInParent<SoldierMoveEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<SoliderEnemy>()) { t.GetComponentInParent<SoliderEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<TutorialEnemyController>()) { t.GetComponentInParent<TutorialEnemyController>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<ScorpionEnemy>()) { t.GetComponentInParent<ScorpionEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<SuicideEnemy>()) { t.GetComponentInParent<SuicideEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<DroneEnemy>()) { t.GetComponentInParent<DroneEnemy>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<VoxBodyPart>()) { t.GetComponentInParent<VoxBodyPart>().TakeDamage(damageAmount); isHit = true; }
        else if (t.GetComponentInParent<VoxPart>()) { t.GetComponentInParent<VoxPart>().TakeDamage(damageAmount); isHit = true; }

        if (isHit && hitEffectPrefab != null)
        {
            // ヒットエフェクトを生成（当たったコライダーの中心位置に）
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 previewCenter = transform.position + (Vector3.up * 1.0f) + (transform.forward * 1.5f);
        Gizmos.DrawWireSphere(previewCenter, meleeAttackRange);
    }
    public void TakeDamage(float amount) { float def = (_modesAndVisuals.CurrentArmorStats != null) ? _modesAndVisuals.CurrentArmorStats.defenseMultiplier : 1.0f; playerStatus.TakeDamage(amount, def); }

    public void SetDebuff(float moveMult, float jumpMult)
    {
        _debuffMoveMultiplier = moveMult;
        _debuffJumpMultiplier = jumpMult;
    }

    public void ResetDebuff()
    {
        _debuffMoveMultiplier = 1.0f;
        _debuffJumpMultiplier = 1.0f;
    }
}