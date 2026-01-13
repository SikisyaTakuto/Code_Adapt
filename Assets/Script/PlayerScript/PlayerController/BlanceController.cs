// ファイル名: BlanceController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【プレイヤー総合制御クラス】
/// プレイヤーの移動、空中飛行、攻撃（近接・遠距離）、および入力Systemとの連携を管理します。
/// 実際のステータス（HP/EN）は PlayerStatus クラス、見た目の切り替えは PlayerModesAndVisuals クラスが担当します。
/// </summary>
public class BlanceController : MonoBehaviour
{
    // ======================================================================================
    // 1. 外部コンポーネント参照 (Dependencies)
    // ======================================================================================
    [Header("Core Components")]
    [Tooltip("移動を制御する標準コンポーネント")]
    [SerializeField] private CharacterController _playerController;

    [Tooltip("カメラ制御スクリプト（ロックオン情報の取得に使用）")]
    [SerializeField] private TPSCameraController _tpsCamController;

    [Tooltip("武器やアーマーの見た目・ステータス倍率を管理")]
    public PlayerModesAndVisuals _modesAndVisuals;

    [Tooltip("HPやエネルギーの残量を管理")]
    public PlayerStatus playerStatus;

    [Header("Attack VFX & Points")]
    [Tooltip("遠距離攻撃用ビームのプレハブ")]
    public BeamController beamPrefab;
    [Tooltip("ビームが発生する位置（左右の銃口など）")]
    public Transform[] beamFirePoints;
    [Tooltip("敵に命中した際のエフェクト")]
    public GameObject hitEffectPrefab;
    [Tooltip("攻撃対象となるレイヤー（Enemy等）")]
    public LayerMask enemyLayer;

    // ======================================================================================
    // 2. 移動・飛行パラメータ (Movement Parameters)
    // ======================================================================================
    [Header("Basic Movement")]
    public float baseMoveSpeed = 15.0f;    // 地上移動の基本速度
    public float dashMultiplier = 2.5f;   // ダッシュ（ブースト）時の速度倍率
    public float verticalSpeed = 10.0f;   // 上昇（飛行）速度
    public float gravity = -9.81f;        // 重力定数
    public float fastFallMultiplier = 3.0f; // 落下中、さらに速く落ちるための倍率
    public bool canFly = true;            // 飛行許可フラグ

    // ======================================================================================
    // 3. 攻撃・硬直パラメータ (Attack & Stun Parameters)
    // ======================================================================================
    [Header("Melee Settings")]
    public GameObject swordObject;        // 近接攻撃中に表示される剣のモデル
    public float meleeAttackRange = 2.0f; // 剣の攻撃判定が届く距離
    public float meleeDamage = 50.0f;     // 剣の1ヒットあたりのダメージ

    [Header("Range Settings")]
    public float beamDamage = 50.0f;      // ビーム1発のダメージ
    public float beamAttackEnergyCost = 30.0f; // ビーム使用時の消費EN
    public float beamMaxDistance = 100f;  // ビームの最大射程
    public float lockOnTargetHeightOffset = 1.0f; // 非コライダー対象を狙う際の高さ補正

    [Header("Animation Timings")]
    public float swordComboTime = 1.5f;     // 剣を振っている間の時間
    public float beamFiringTime = 1.0f;    // ビームを照射している時間
    public float doubleGunDuration = 1.2f; // 2丁拳銃攻撃時の移動不能時間
    public float landStunDuration = 0.2f;  // 着地時に発生する短い硬直時間

    [Header("Audio Resources")]
    public AudioSource audioSource;
    public AudioClip swordSwingSound;

    // ======================================================================================
    // 4. 内部状態管理 (Internal State)
    // ======================================================================================
    private bool _isAttacking = false;     // 現在攻撃モーション中か
    private bool _isStunned = false;       // 現在硬直中（操作不能）か
    private float _stunTimer = 0.0f;       // 硬直終了までの残り時間
    private float _moveSpeed;              // 現在のアーマー性能を適用した移動速度
    private float _verticalInput = 0f;     // 上下移動の入力値（コントローラー対応用）
    private float _debuffMoveMultiplier = 1.0f; // デバフによる移動速度補正
    private float _debuffJumpMultiplier = 1.0f; // デバフによる上昇速度補正

    private Vector3 _velocity;             // 重力・上昇などの垂直方向の速度
    private bool _wasGrounded = false;     // 前フレームで接地していたか（着地判定用）
    private Quaternion _rotationBeforeAttack; // 攻撃開始前のプレイヤーの向き

    // ======================================================================================
    // 5. ライフサイクル (Life Cycle)
    // ======================================================================================

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        // ゲーム開始時は剣を隠しておく
        if (swordObject != null) swordObject.SetActive(false);
    }

    void Update()
    {
        // 死亡時、またはステータス未設定時は処理しない
        if (playerStatus == null || playerStatus.IsDead) return;

        // 【最優先処理】武器・アーマーの切り替え（硬直中や攻撃中でも先行入力可能）
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();

        bool isGroundedNow = _playerController.isGrounded;
        UpdateStunTimer(isGroundedNow);

        // 硬直中の挙動：移動入力を受け付けず、重力のみ適用して終了
        if (_isStunned)
        {
            ApplyGravityOnly(isGroundedNow);
            _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
            _wasGrounded = isGroundedNow;
            return;
        }

        // プレイヤーの向きをカメラまたはロックオン対象に合わせる
        HandleRotation();

        // アーマーごとの機動力（速度倍率）を反映
        ApplyArmorStats();

        // 攻撃入力の受付
        HandleAttackInputs();

        // 垂直移動（飛行・重力）と水平移動（前後左右）の計算・実行
        Vector3 finalMove = HandleVerticalMovement(isGroundedNow) + HandleHorizontalMovement();
        _playerController.Move(finalMove * Time.deltaTime);

        _wasGrounded = isGroundedNow;
    }

    /// <summary>
    /// コンポーネントが未設定の場合、自動的に親やシーンから取得します
    /// </summary>
    private void InitializeComponents()
    {
        if (_playerController == null) _playerController = GetComponentInParent<CharacterController>();
        if (_tpsCamController == null) _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (playerStatus == null) playerStatus = GetComponentInParent<PlayerStatus>();
    }

    // ======================================================================================
    // 6. 移動ロジック (Movement Logic)
    // ======================================================================================

    /// <summary>
    /// プレイヤーの回転を制御します。攻撃中は向きを固定します。
    /// </summary>
    private void HandleRotation()
    {
        if (_tpsCamController == null || _isAttacking) return;

        if (_tpsCamController.LockOnTarget == null)
            // ロックオンしていない場合はカメラの正面を向く
            _tpsCamController.RotatePlayerToCameraDirection();
        else
            // ロックオン中はターゲットの方向を常に見る
            RotateTowards(GetLockOnTargetPosition(_tpsCamController.LockOnTarget));
    }

    /// <summary>
    /// キーボード入力を元に、前後左右の移動ベクトルを計算します。
    /// </summary>
    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h == 0f && v == 0f) return Vector3.zero;

        // カメラの向き（Y軸のみ）を考慮して移動方向を決定
        Vector3 moveDirection;
        if (_tpsCamController != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraRotation * new Vector3(h, 0, v);
        }
        else
        {
            moveDirection = (transform.right * h + transform.forward * v);
        }
        moveDirection.Normalize();

        // 速度計算（基本速度 × デバフ倍率）
        float currentSpeed = _moveSpeed * _debuffMoveMultiplier;

        // ダッシュ処理（左Shift押し ＋ ENが残っている場合）
        bool isDashing = Input.GetKey(KeyCode.LeftShift) && playerStatus.currentEnergy > 0.1f;
        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        }

        return moveDirection * currentSpeed;
    }

    /// <summary>
    /// 飛行上昇、重力落下、着地判定などの垂直移動を計算します。
    /// </summary>
    private Vector3 HandleVerticalMovement(bool isGrounded)
    {
        // 地面に付いている場合は、わずかに下向きの力をかけて接地を安定させる
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool isFlyingUp = Input.GetKey(KeyCode.Space) || _verticalInput > 0.5f;
        bool hasVerticalInput = false;

        // 飛行処理（スペースキー ＋ EN残量あり）
        if (canFly && playerStatus.currentEnergy > 0.1f && isFlyingUp)
        {
            _velocity.y = verticalSpeed * _debuffJumpMultiplier;
            hasVerticalInput = true;
        }

        if (hasVerticalInput)
        {
            playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        }
        else if (!isGrounded)
        {
            // 空中にいて上昇中でない場合は重力を適用
            float fallMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallMultiplier;
        }

        // ENが切れたら強制的に上昇を止める
        if (playerStatus.currentEnergy <= 0.1f && _velocity.y > 0) _velocity.y = 0;

        return new Vector3(0, _velocity.y, 0);
    }

    /// <summary>
    /// 硬直中など、重力以外の移動入力を無視したい場合に使用します
    /// </summary>
    private void ApplyGravityOnly(bool isGroundedNow)
    {
        if (!isGroundedNow)
        {
            float fallMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallMultiplier;
        }
        else
        {
            _velocity.y = -0.1f;
        }
    }

    /// <summary>
    /// 現在装備中のアーマーから移動速度倍率を取得し反映します
    /// </summary>
    private void ApplyArmorStats()
    {
        var stats = _modesAndVisuals.CurrentArmorStats;
        _moveSpeed = baseMoveSpeed * (stats != null ? stats.moveSpeedMultiplier : 1.0f);
    }

    // ======================================================================================
    // 7. 攻撃ロジック (Combat Logic)
    // ======================================================================================

    private void HandleAttackInputs()
    {
        if (_isAttacking || _isStunned) return;
        if (Input.GetMouseButtonDown(0)) PerformAttack();
    }

    /// <summary>
    /// 攻撃の種類を判定し、アニメーションの再生と攻撃処理の開始を行います。
    /// </summary>
    private void PerformAttack()
    {
        bool isAttack2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);

        // アクティブなアーマーのアニメーションスクリプトを自動取得
        BalanceAnimation balAnim = GetComponentInChildren<BalanceAnimation>(true);
        BusterAnimation busAnim = GetComponentInChildren<BusterAnimation>(true);

        if (balAnim != null && balAnim.gameObject.activeInHierarchy) balAnim.PlayAttackAnimation(isAttack2);
        else if (busAnim != null && busAnim.gameObject.activeInHierarchy) busAnim.PlayAttackAnimation(isAttack2);

        // 武装モードに応じて処理を分岐
        switch (_modesAndVisuals.CurrentWeaponMode)
        {
            case PlayerModesAndVisuals.WeaponMode.Attack1:
                StartCoroutine(HandleComboAttackRoutine());
                break;
            case PlayerModesAndVisuals.WeaponMode.Attack2:
                HandleDoubleGunAttack();
                break;
        }
    }

    /// <summary>
    /// 【攻撃パターン1】剣での2連撃。
    /// 回数を3回から2回に変更し、判定時間を調整しました。
    /// </summary>
    private IEnumerator HandleComboAttackRoutine()
    {
        _isAttacking = true;
        _rotationBeforeAttack = transform.rotation;

        Vector3 targetPos = FindBestAutoAimTarget();
        RotateTowards(targetPos);
        Quaternion attackRotation = transform.rotation;

        // 硬直時間は設定された合計時間を使用
        StartAttackStun(swordComboTime + beamFiringTime);

        if (swordObject != null) swordObject.SetActive(true);

        // --- 修正ポイント：2連撃に合わせて分割 ---
        int comboCount = 2;
        float interval = swordComboTime / (float)comboCount;

        for (int i = 0; i < comboCount; i++) // 2連撃
        {
            if (audioSource != null && swordSwingSound != null) audioSource.PlayOneShot(swordSwingSound);

            // 今回のスイングで叩いた敵のリストをリセット
            HashSet<GameObject> alreadyHitEnemies = new HashSet<GameObject>();

            float elapsedInSwing = 0;
            while (elapsedInSwing < interval)
            {
                transform.rotation = attackRotation;

                // 剣の当たり判定
                Collider[] hits = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);
                foreach (var col in hits)
                {
                    if (col.transform != this.transform && !alreadyHitEnemies.Contains(col.gameObject))
                    {
                        ApplyDamageToEnemy(col, meleeDamage);
                        alreadyHitEnemies.Add(col.gameObject);
                    }
                }

                elapsedInSwing += Time.deltaTime;
                yield return null;
            }
        }

        if (swordObject != null) swordObject.SetActive(false);
        transform.rotation = _rotationBeforeAttack;
    }

    /// <summary>
    /// 【攻撃パターン2】2丁拳銃ビーム。
    /// </summary>
    private void HandleDoubleGunAttack()
    {
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) return;
        StartCoroutine(DoubleGunRoutine());
    }

    private IEnumerator DoubleGunRoutine()
    {
        _rotationBeforeAttack = transform.rotation;
        StartAttackStun(doubleGunDuration);

        if (swordObject != null) swordObject.SetActive(false);

        // ビーム発射（同時にターゲットの方向を向く）
        Vector3 target = FireDualBeams();
        RotateTowards(target);

        yield return new WaitForSeconds(doubleGunDuration);

        transform.rotation = _rotationBeforeAttack;
    }

    /// <summary>
    /// 左右の発射口からビームを生成し、ダメージ判定を行います。
    /// </summary>
    private Vector3 FireDualBeams()
    {
        if (beamFirePoints == null || beamPrefab == null) return transform.position + transform.forward;

        Vector3 targetPosition = FindBestAutoAimTarget();

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;
            Vector3 dir = (targetPosition - origin).normalized;

            // ヒットスキャン方式で当たり判定を行う
            bool didHit = Physics.Raycast(origin, dir, out RaycastHit hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + dir * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            // ビームのビジュアルを生成
            BeamController beam = Instantiate(beamPrefab, origin, Quaternion.LookRotation(dir));
            beam.Fire(origin, endPoint, didHit);
        }
        return targetPosition;
    }

    // ======================================================================================
    // 8. ダメージ・デバフ・ユーティリティ (Utilities)
    // ======================================================================================

    /// <summary>
    /// 命中したオブジェクトのコンポーネントをチェックし、適切なダメージメソッドを呼び出します。
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damage)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 敵の種類（各コンポーネント）に応じてダメージを適用
        if (target.TryGetComponent<SoldierMoveEnemy>(out var s1)) { s1.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<SoliderEnemy>(out var s2)) { s2.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<TutorialEnemyController>(out var s3)) { s3.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var s4)) { s4.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var s5)) { s5.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var s6)) { s6.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<VoxBodyPart>(out var body)) { body.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<VoxPart>(out var part)) { part.TakeDamage(damage); isHit = true; }

        // ヒットした場合、中心に火花などのエフェクトを出す
        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
        }
    }

    /// <summary>
    /// プレイヤーがダメージを受けた際の入り口。
    /// 現在のアーマーの防御力を考慮して計算します。
    /// </summary>
    public void TakeDamage(float damage)
    {
        var stats = _modesAndVisuals.CurrentArmorStats;
        float defense = (stats != null) ? stats.defenseMultiplier : 1.0f;
        playerStatus.TakeDamage(damage, defense);
    }

    /// <summary>
    /// 硬直タイマーの更新。0になると操作が可能になります。
    /// </summary>
    private void UpdateStunTimer(bool isGrounded)
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

    /// <summary>
    /// 指定時間、移動・攻撃を封じます。
    /// </summary>
    public void StartAttackStun(float duration)
    {
        _isStunned = true;
        _stunTimer = duration;
        _velocity.y = 0f; // 攻撃開始時に垂直速度をリセット（空中で止まる演出用）
    }

    /// <summary>
    /// ロックオン対象、または画面中央（カメラの先）から最も適切な狙い撃ち座標を計算します。
    /// </summary>
    private Vector3 FindBestAutoAimTarget()
    {
        // 1. ロックオン対象がいればそこを狙う
        if (_tpsCamController?.LockOnTarget != null)
            return GetLockOnTargetPosition(_tpsCamController.LockOnTarget, true);

        // 2. ロックオンなしならカメラのレティクル（画面中央）の先を狙う
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, beamMaxDistance, ~0)) return hit.point;
            return ray.origin + ray.direction * beamMaxDistance;
        }

        return transform.position + transform.forward * 10f;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffset = false)
    {
        if (target.TryGetComponent<Collider>(out var col)) return col.bounds.center;
        return useOffset ? target.position + Vector3.up * lockOnTargetHeightOffset : target.position;
    }

    // ======================================================================================
    // 9. 入力イベント (System Inputs)
    // ======================================================================================

    private void HandleWeaponSwitchInput() { if (Input.GetKeyDown(KeyCode.E)) _modesAndVisuals.SwitchWeapon(); }

    private void HandleArmorSwitchInput()
    {
        // 1キーまたは2キーでアーマー（装備スロット）を切り替え
        if (Input.GetKeyDown(KeyCode.Alpha1)) _modesAndVisuals.ChangeArmorBySlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) _modesAndVisuals.ChangeArmorBySlot(1);
    }

    /// <summary>
    /// 剣の3連撃に合わせて時間差で音を鳴らすためのサブ処理。
    /// </summary>
    private IEnumerator PlayMeleeSwingSounds()
    {
        float interval = swordComboTime / 3f;
        for (int i = 0; i < 3; i++)
        {
            if (audioSource != null && swordSwingSound != null) audioSource.PlayOneShot(swordSwingSound);
            yield return new WaitForSeconds(interval);
        }
    }

    // 外部（敵の攻撃など）から機動力を下げたい場合に使用
    public void SetDebuff(float moveMult, float jumpMult) { _debuffMoveMultiplier = moveMult; _debuffJumpMultiplier = jumpMult; }
    public void ResetDebuff() { _debuffMoveMultiplier = 1.0f; _debuffJumpMultiplier = 1.0f; }
}