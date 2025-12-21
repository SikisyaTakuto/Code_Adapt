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
            if (isFlyingUp) { _velocity.y = verticalSpeed; hasVerticalInput = true; }
            // else if (isFlyingDown) { _velocity.y = -verticalSpeed; hasVerticalInput = true; }
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
        // 1. 攻撃開始と硬直の設定
        float totalDuration = swordComboTime + beamFiringTime;
        StartAttackStun(totalDuration);

        // 向きの調整
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) RotateTowards(GetLockOnTargetPosition(lockOnTarget));

        // ★ 2. 剣の3連撃音を再生（コルーチン内でタイミングを制御）
        StartCoroutine(PlayMeleeSwingSounds());

        // 剣の攻撃判定（判定の発生タイミングは音と合わせるのが理想ですが、一旦ここで一括判定）
        Debug.Log("Sword Combo Start");
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform != this.transform) ApplyDamageToEnemy(hitCollider, meleeDamage * 3);
        }

        // 3. 剣を振っている時間だけ待機
        yield return new WaitForSeconds(swordComboTime);

        // 4. 剣を非表示にしてビームを撃つ
        if (swordObject != null) swordObject.SetActive(false);

        Debug.Log("Beam Attack Start");
        FireDualBeams();

        // 5. ビームを撃ち終わるまで待機
        yield return new WaitForSeconds(beamFiringTime);

        // 6. 剣を再表示する
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

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        if (lockOnTarget != null) RotateTowards(GetLockOnTargetPosition(lockOnTarget));

        FireDualBeams();

        yield return new WaitForSeconds(doubleGunDuration);

        if (swordObject != null) swordObject.SetActive(true);
    }

    // 共通処理: 2丁の銃からビームを発射
    private void FireDualBeams()
    {
        if (beamFirePoints == null || beamFirePoints.Length == 0 || beamPrefab == null) return;

        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPosition = Vector3.zero;
        bool isLockedOn = lockOnTarget != null;
        if (isLockedOn) targetPosition = GetLockOnTargetPosition(lockOnTarget, true);

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
}