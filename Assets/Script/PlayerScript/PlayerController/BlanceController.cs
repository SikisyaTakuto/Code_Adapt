// ファイル名: BlanceController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの移動、攻撃、およびInput Systemからの入力を制御します。
/// ステータス管理（HP/エネルギー）は PlayerStatus コンポーネントに委譲します。
/// </summary>
public class BlanceController : MonoBehaviour
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
    public GameObject swordObject; // インスペクターで剣のオブジェクトをアサインしてください
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public float beamMaxDistance = 100f;
    public float lockOnTargetHeightOffset = 1.0f;

    [Header("Timing Settings")]
    [Tooltip("剣の3連撃にかかる時間（この後にビームが出る）")]
    public float swordComboTime = 1.5f;
    [Tooltip("ビームを撃っている時間")]
    public float beamFiringTime = 1.0f;
    [Tooltip("2丁拳銃モードの硬直時間")]
    public float doubleGunDuration = 1.2f; // ← この行を追加

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip swordSwingSound; // 剣を振る音をアサイン

    private bool _isAttacking = false;
    private bool _isStunned = false;
    private float _stunTimer = 0.0f;
    private Quaternion _rotationBeforeAttack;

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
    }

    void Update()
    {
        if (playerStatus == null || playerStatus.IsDead) return;

        // ★修正：攻撃中(硬直中)でも武器切り替え(E)とアーマー切り替え(1,2)だけは先に処理する
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();

        bool isGroundedNow = _playerController.isGrounded;
        HandleStunState(isGroundedNow);

        // 硬直中は移動と攻撃入力をスキップ
        if (_isStunned)
        {
            HandleStunnedVerticalMovement(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // プレイヤーの回転
        if (_tpsCamController != null)
        {
            if (_tpsCamController.LockOnTarget == null)
                _tpsCamController.RotatePlayerToCameraDirection();
            else
                RotateTowards(GetLockOnTargetPosition(_tpsCamController.LockOnTarget));
        }

        ApplyArmorStats();
        HandleAttackInputs(); // 攻撃入力

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

    // 硬直開始メソッドを拡張（時間を指定可能に）
    public void StartAttackStun(float duration)
    {
        _rotationBeforeAttack = transform.rotation;
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = duration; // 指定されたアニメーション時間に合わせる
        _velocity.y = 0f;
    }

    private void HandleStunState(bool isGrounded)
    {
        if (!_isStunned) return;
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0.0f)
        {
            // ★追加：攻撃（硬直）が終わったら元の角度に戻す
            if (_isAttacking)
            {
                transform.rotation = _rotationBeforeAttack;
            }

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
           // StartLandingStun();
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
                _velocity.y = verticalSpeed * _debuffJumpMultiplier; // デバフを掛ける
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

    private void PerformAttack()
    {
        // 1. 現在のモードが Attack2 かどうかを判定
        bool isAttack2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);

        // 2. 子供のオブジェクトからアニメーションスクリプトを探す
        // 引数に (true) を入れることで、非アクティブなアーマーも含めて検索できます
        BalanceAnimation balAnim = GetComponentInChildren<BalanceAnimation>(true);
        BusterAnimation busAnim = GetComponentInChildren<BusterAnimation>(true);

        // 見つかった方のスクリプトの再生メソッドを呼ぶ
        if (balAnim != null && balAnim.gameObject.activeInHierarchy)
        {
            balAnim.PlayAttackAnimation(isAttack2);
        }
        else if (busAnim != null && busAnim.gameObject.activeInHierarchy)
        {
            busAnim.PlayAttackAnimation(isAttack2);
        }

        // 3. ★修正：実際の攻撃処理の呼び出し（定義されているメソッド名に変更）
        switch (_modesAndVisuals.CurrentWeaponMode)
        {
            case PlayerModesAndVisuals.WeaponMode.Attack1:
                // コルーチンなので StartCoroutine で呼ぶ必要があります
                StartCoroutine(HandleComboAttackRoutine());
                break;
            case PlayerModesAndVisuals.WeaponMode.Attack2:
                // こちらは内部でコルーチンを呼んでいるため、そのまま呼び出し
                HandleDoubleGunAttack();
                break;
        }
    }

    // パターン1: 剣の3連撃 -> 剣を消す -> ビームを出す
    private IEnumerator HandleComboAttackRoutine()
    {
        float totalDuration = swordComboTime + beamFiringTime;
        StartAttackStun(totalDuration);

        // --- 剣の攻撃フェーズ ---
        StartCoroutine(PlayMeleeSwingSounds());

        // ★追加：剣を振る際も、一番近い敵がいればそちらを向く
        Vector3 initialTarget = FindBestAutoAimTarget();
        RotateTowards(initialTarget);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform != this.transform) ApplyDamageToEnemy(hitCollider, meleeDamage * 3);
        }

        yield return new WaitForSeconds(swordComboTime);

        // --- ビーム攻撃フェーズ ---
        if (swordObject != null) swordObject.SetActive(false);

        // ★修正：ビームを発射し、実際に狙った方向へプレイヤーを回転させる
        Vector3 beamTarget = FireDualBeams();
        RotateTowards(beamTarget);

        yield return new WaitForSeconds(beamFiringTime);

        if (swordObject != null) swordObject.SetActive(true);
    }

    // ★ 3連撃の音を鳴らすためのサブ・コルーチン
    private IEnumerator PlayMeleeSwingSounds()
    {
        float interval = swordComboTime / 3f; // 3連撃なので時間を3分割
        for (int i = 0; i < 3; i++)
        {
            if (audioSource != null && swordSwingSound != null)
            {
                audioSource.PlayOneShot(swordSwingSound);
            }
            yield return new WaitForSeconds(interval);
        }
    }

    // パターン2: 2丁の銃で打つだけ（ここも剣を隠す処理を追加）
    private void HandleDoubleGunAttack()
    {
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) return;

        StartCoroutine(DoubleGunRoutine());
    }

    private IEnumerator DoubleGunRoutine()
    {
        StartAttackStun(doubleGunDuration);
        if (swordObject != null) swordObject.SetActive(false);

        // ★修正：発射したビームのターゲット方向へ即座に向く
        Vector3 beamTarget = FireDualBeams();
        RotateTowards(beamTarget);

        yield return new WaitForSeconds(doubleGunDuration);

        if (swordObject != null) swordObject.SetActive(true);
    }

    // 索敵ロジックを共通化
    private Vector3 FindBestAutoAimTarget()
    {
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) return GetLockOnTargetPosition(lockOnTarget, true);

        if (Camera.main != null)
        {
            Vector3 searchOrigin = Camera.main.transform.position;
            Collider[] nearbyEnemies = Physics.OverlapSphere(searchOrigin, beamMaxDistance, enemyLayer);
            float closestAngle = 35f;
            Transform bestTarget = null;

            foreach (var col in nearbyEnemies)
            {
                Vector3 directionToEnemy = (col.bounds.center - searchOrigin).normalized;
                float angle = Vector3.Angle(Camera.main.transform.forward, directionToEnemy);
                if (angle < closestAngle)
                {
                    closestAngle = angle;
                    bestTarget = col.transform;
                }
            }

            if (bestTarget != null) return GetLockOnTargetPosition(bestTarget, true);

            // 敵がいない場合はカメラの正面
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, beamMaxDistance, ~0)) return hit.point;
            return ray.origin + ray.direction * beamMaxDistance;
        }
        return transform.position + transform.forward * 10f;
    }

    // 共通処理: ビームを発射し、狙った座標を返す
    private Vector3 FireDualBeams()
    {
        if (beamFirePoints == null || beamFirePoints.Length == 0 || beamPrefab == null)
            return transform.position + transform.forward;

        // ターゲット座標を決定
        Vector3 targetPosition = FindBestAutoAimTarget();

        // 各発射ポイントからビームを生成
        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;
            Vector3 fireDirection = (targetPosition - origin).normalized;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }

        return targetPosition; // 回転に使用するために座標を返す
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
    public void SetDebuff(float moveMult, float jumpMult)
    {
        _debuffMoveMultiplier = moveMult;
        _debuffJumpMultiplier = jumpMult;
    }

    // デバフを解除する
    public void ResetDebuff()
    {
        _debuffMoveMultiplier = 1.0f;
        _debuffJumpMultiplier = 1.0f;
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
}