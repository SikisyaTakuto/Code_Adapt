using System.Collections;
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
    public Transform[] beamFirePoints;    // 上の2個
    public Transform[] gatlingFirePoints; // 下の2個
    public GameObject gatlingBulletPrefab; // ガトリング用の弾丸またはエフェクト
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

    [Header("Attack Patterns")]
    public float backstepForce = 20f; // Attack2で後ろに下がる強さ
    public float gatlingFireRate = 0.05f; // ガトリングの連射速度
    public int gatlingBurstCount = 10;     // ★追加：1回の攻撃で発射する弾数
    public float gatlingEnergyCostTotal = 10f; // ★追加：ガトリング全体の消費エネルギー
    private float nextGatlingTime;

    private BusterAnimation _busterAnim; // キャッシュ用

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

    private float _debuffMoveMultiplier = 1.0f;
    private float _debuffJumpMultiplier = 1.0f;

    void Awake()
    {
        InitializeComponents();
        // アニメーションコンポーネントを取得
        _busterAnim = GetComponentInChildren<BusterAnimation>();
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

            // 空中であれば、攻撃時に設定した _velocity (後ろへのベクトル) をそのまま使う
            // 徐々に減速させる(摩擦)
            float friction = 5f;
            _velocity.x = Mathf.Lerp(_velocity.x, 0, Time.deltaTime * friction);
            _velocity.z = Mathf.Lerp(_velocity.z, 0, Time.deltaTime * friction);

            _playerController.Move(_velocity * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // 2. プレイヤーの向き制御
        if (_tpsCamController != null)
        {
            if (_tpsCamController.LockOnTarget == null)
            {
                // 非ロックオン時：カメラが向いている水平方向に体を固定
                _tpsCamController.RotatePlayerToCameraDirection();
            }
            else
            {
                // ロックオン時：常にターゲットを向く
                RotateTowards(GetLockOnTargetPosition(_tpsCamController.LockOnTarget));
            }
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

        // 足元を地面に密着させる微調整
        _velocity = new Vector3(0, -0.1f, 0);
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

        float currentSpeed = _moveSpeed * _debuffMoveMultiplier;

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
            //StartLandingStun();
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
            if (isFlyingUp)
            {
                _velocity.y = verticalSpeed * _debuffJumpMultiplier; // デバフ適用
                hasVerticalInput = true;
            }
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

    // =======================================================
    // 索敵・エイム支援ロジック (追加)
    // =======================================================

    private Vector3 GetAutoAimTargetPosition()
    {
        // 1. ロックオン中ならその座標
        if (_tpsCamController != null && _tpsCamController.LockOnTarget != null)
        {
            return GetLockOnTargetPosition(_tpsCamController.LockOnTarget, true);
        }

        // 2. 非ロックオン時：カメラ中央付近の敵を探す
        if (Camera.main != null)
        {
            Vector3 origin = Camera.main.transform.position;
            Vector3 forward = Camera.main.transform.forward;
            Collider[] hits = Physics.OverlapSphere(transform.position, beamMaxDistance, enemyLayer);

            float minAngle = 30f; // 30度以内なら吸い付く
            Transform best = null;

            foreach (var col in hits)
            {
                Vector3 dir = (col.bounds.center - origin).normalized;
                float angle = Vector3.Angle(forward, dir);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    best = col.transform;
                }
            }

            if (best != null) return GetLockOnTargetPosition(best, true);

            // 敵がいない場合はカメラの100m先
            return origin + forward * beamMaxDistance;
        }

        return transform.position + transform.forward * beamMaxDistance;
    }

    private void PerformAttack()
    {
        bool isMode2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);

        if (_busterAnim != null) _busterAnim.PlayAttackAnimation(isMode2);

        // ★攻撃開始時に最適なターゲットを取得し、その方向を向く
        Vector3 targetPos = GetAutoAimTargetPosition();
        RotateTowards(targetPos);

        switch (_modesAndVisuals.CurrentWeaponMode)
        {
            case PlayerModesAndVisuals.WeaponMode.Attack1:
                HandleAttack1_LongRangeBeam(targetPos); // 引数追加
                break;
            case PlayerModesAndVisuals.WeaponMode.Attack2:
                HandleAttack2_FullBurstBackstep();
                break;
        }
    }

    // --- Attack1: 中遠距離ビーム ---
    private void HandleAttack1_LongRangeBeam(Vector3 targetPos)
    {
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) return;

        StartAttackStun();
        // FireAllGunsを介さず直接特定のターゲットへ発射
        FireSpecificGuns(true, false, targetPos, true);
    }

    // --- Attack2: 近中距離 全弾発射 & バックステップ (全部) ---
    private void HandleAttack2_FullBurstBackstep()
    {
        StartCoroutine(FullBurstRoutine());
    }

    // --- Attack2: 近中距離 全弾発射 & バックステップ ---
    private IEnumerator FullBurstRoutine()
    {
        // --- 攻撃開始: 硬直フラグを立てる ---
        _isAttacking = true;
        _isStunned = true;

        // ★追加：攻撃開始の瞬間に最適なターゲットを索敵し、向き直る
        Vector3 initialTargetPos = GetAutoAimTargetPosition();
        RotateTowards(initialTargetPos);

        // 1. バックステップ (向いた方向の真後ろへ)
        Vector3 backDir = -transform.forward;
        _velocity = backDir * backstepForce + Vector3.up * 2f;

        // 攻撃アニメーションに合わせてスタンの残り時間を長めに設定
        _stunTimer = 5.0f;

        // 2. 【第一波】ビーム発射
        if (playerStatus.ConsumeEnergy(beamAttackEnergyCost))
        {
            // 索敵したターゲット、または正面に向けて発射
            FireSpecificGuns(true, false, initialTargetPos, true);
        }

        // ビームとガトリングの間のディレイ
        yield return new WaitForSeconds(1.0f);

        // 3. 【第二波】ガトリング連射
        if (playerStatus.ConsumeEnergy(gatlingEnergyCostTotal))
        {
            for (int i = 0; i < gatlingBurstCount; i++)
            {
                // 連射中も硬直を維持
                _isStunned = true;
                _stunTimer = 1.0f;

                // ★追加：連射中もターゲットを追尾し続ける
                Vector3 currentTargetPos = GetAutoAimTargetPosition();
                RotateTowards(currentTargetPos);

                // 発射 (isLockedOnフラグをtrueにすることで、FireProjectile内で方向計算が行われる)
                FireSpecificGuns(false, true, currentTargetPos, true);

                yield return new WaitForSeconds(gatlingFireRate);
            }
        }

        // --- 攻撃終了: 硬直を短くして操作権を戻す準備 ---
        _stunTimer = 0.6f;
    }

    // 特定の武器種だけを撃つヘルパー
    private void FireSpecificGuns(bool useBeam, bool useGatling, Vector3 targetPosition, bool isLockedOn)
    {
        Vector3 playerForward = transform.forward;

        if (useBeam && beamFirePoints != null)
        {
            foreach (var fp in beamFirePoints)
            {
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, true);
            }
        }

        if (useGatling && gatlingFirePoints != null)
        {
            foreach (var fp in gatlingFirePoints)
            {
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, false);
            }
        }
    }

    // 全ての発射ポイントから撃つ共通処理
    private void FireAllGuns(bool useBeam, bool useGatling)
    {
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;

        if (isLockedOn)
        {
            targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
            RotateTowards(targetPosition);
        }
        Vector3 playerForward = transform.forward;

        // 1. ビーム発射 (上の2個)
        if (useBeam && beamFirePoints != null)
        {
            foreach (var fp in beamFirePoints)
            {
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, true);
            }
        }

        // 2. ガトリング発射 (下の2個)
        if (useGatling && gatlingFirePoints != null)
        {
            if (!playerStatus.ConsumeEnergy(10f)) return; // 追加のエネルギー消費

            foreach (var fp in gatlingFirePoints)
            {
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, false);
            }
        }
    }

    // 発射ロジックを共通化
    private void FireProjectile(Transform firePoint, Vector3 targetPos, bool isLockedOn, Vector3 forward, bool isBeam)
    {
        Vector3 origin = firePoint.position;
        Vector3 fireDirection = isLockedOn ? (targetPos - origin).normalized : forward;

        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);
        Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

        if (didHit) ApplyDamageToEnemy(hit.collider, isBeam ? beamDamage : 10.0f); // ダメージ差

        if (isBeam)
        {
            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
        else
        {
            if (gatlingBulletPrefab != null)
            {
                // 弾を生成
                GameObject bulletObj = Instantiate(gatlingBulletPrefab, origin, Quaternion.LookRotation(fireDirection));
                GatlingBullet bullet = bulletObj.GetComponent<GatlingBullet>();

                if (bullet != null)
                {
                    // 弾を発射（ターゲットがいればその方向、いなければ正面）
                    bullet.Launch(fireDirection);
                }
            }
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
        // ★追加：本体のパーツ（胴体など）を撃った場合
        else if (target.TryGetComponent<VoxBodyPart>(out var bodyPart))
        {
            bodyPart.TakeDamage(damageAmount);
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
        // 1キーで最初に選んだアーマーを表示
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _modesAndVisuals.ChangeArmorBySlot(0);
        }
        // 2キーで2番目に選んだアーマーを表示
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _modesAndVisuals.ChangeArmorBySlot(1);
        }
    }

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