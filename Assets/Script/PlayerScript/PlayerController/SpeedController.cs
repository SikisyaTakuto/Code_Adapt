using System.Collections;
using UnityEngine;

/// <summary>
/// スピードモード時のプレイヤー制御を管理するクラス。
/// 移動、ジャンプ（飛行）、近接攻撃、ビーム攻撃のロジックを担当します。
/// </summary>
public class SpeedController : MonoBehaviour
{
    #region 1. 依存コンポーネント & フィールド
    // =======================================================

    [Header("コア・コンポーネント")]
    [SerializeField] private CharacterController _playerController; // 移動制御用
    [SerializeField] private TPSCameraController _tpsCamController; // カメラ・ロックオン制御用
    public PlayerModesAndVisuals _modesAndVisuals;              // モード切替・外見管理
    public PlayerStatus playerStatus;                           // HP・エナジーなどのステータス

    [Header("エフェクト & 戦闘用アセット")]
    public BeamController beamPrefab;      // 発射するビームのプレハブ
    public Transform[] beamFirePoints;     // ビームの発射起点
    public GameObject hitEffectPrefab;     // 着弾時のエフェクト
    public LayerMask enemyLayer;           // 攻撃対象となるレイヤー

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _meleeSwingSound;   // 近接攻撃の振り
    [SerializeField] private AudioClip _scratchAttackSound; // 近接攻撃のトドメ

    // --- 内部状態管理用変数 ---
    private bool _isAttacking = false;       // 攻撃アクション中か
    private bool _isStunned = false;         // 硬直（スタン）状態か
    private bool _isRestoringRotation = false; // 攻撃後の向き復帰中か
    private float _stunTimer = 0.0f;         // 硬直の残り時間
    private Quaternion _rotationBeforeAttack; // 攻撃開始前のプレイヤーの向き
    private Vector3 _velocity;               // 現在の移動速度（重力用）
    private float _moveSpeed;                // 計算後の移動速度
    private bool _wasGrounded = false;       // 前フレームで接地していたか

    private float _debuffMoveMultiplier = 1.0f; // 移動速度デバフ倍率
    private float _debuffJumpMultiplier = 1.0f; // ジャンプ力デバフ倍率
    #endregion

    #region 2. 設定パラメータ
    // =======================================================

    [Header("移動パラメータ")]
    public float baseMoveSpeed = 15.0f;   // 基本移動速度
    public float dashMultiplier = 2.5f;   // ダッシュ（Shift）時の倍率
    public float verticalSpeed = 10.0f;   // 上昇（飛行）速度
    public float gravity = -9.81f;        // 重力加速度
    public float fastFallMultiplier = 3.0f; // 落下時の加速倍率
    public bool canFly = true;            // 飛行可能かどうか

    [Header("戦闘パラメータ")]
    public float meleeAttackRange = 2.0f;  // 近接攻撃の判定半径
    public float meleeDamage = 50.0f;      // 近接攻撃の基本ダメージ
    public float beamDamage = 50.0f;       // ビーム1本のダメージ
    public float beamAttackEnergyCost = 30.0f; // ビーム使用時のエナジー消費量
    public float beamMaxDistance = 100f;   // ビームの最大射程
    public float lockOnTargetHeightOffset = 1.0f; // ロックオン対象の中心オフセット値

    [Header("タイマー設定")]
    public float attackFixedDuration = 0.8f; // 攻撃時の基本硬直時間
    #endregion

    #region 3. ライフサイクル (Unityイベント)
    // =======================================================

    void Awake() => InitializeComponents();

    void Update()
    {
        // 死亡時、またはステータスがない場合は処理しない
        if (playerStatus == null || playerStatus.IsDead) return;

        bool isGroundedNow = _playerController.isGrounded;

        // 【状態チェック】硬直中・攻撃中などは自由な移動を制限する
        if (UpdateStunAndStates(isGroundedNow))
        {
            ApplyGravityOnly(isGroundedNow); // 重力の影響だけは受ける
            _wasGrounded = isGroundedNow;
            return;
        }

        // プレイヤーの向きをカメラまたはロックオン対象に合わせる
        HandleRotation();

        // 移動・入力を処理
        ApplyArmorStats(); // アーマーによる速度補正の適用
        HandleInput();     // 攻撃・武器切替入力

        // 移動ベクトルの計算と実行
        Vector3 finalMove = HandleVerticalMovement(isGroundedNow) + HandleHorizontalMovement();
        _playerController.Move(finalMove * Time.deltaTime);

        _wasGrounded = isGroundedNow;
    }

    /// <summary>
    /// コンポーネントの自動取得
    /// </summary>
    private void InitializeComponents()
    {
        if (_playerController == null) _playerController = GetComponentInParent<CharacterController>();
        if (_tpsCamController == null) _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (playerStatus == null) playerStatus = GetComponentInParent<PlayerStatus>();
    }
    #endregion

    #region 4. 移動・回転ロジック
    // =======================================================

    /// <summary>
    /// カメラの向きやロックオン対象に基づいてプレイヤーを回転させる
    /// </summary>
    private void HandleRotation()
    {
        if (_tpsCamController == null || _isAttacking || _isRestoringRotation) return;

        // ロックオン中のみ、強制的に敵の方向を向かせる
        if (_tpsCamController.LockOnTarget != null)
        {
            RotateTowards(GetLockOnTargetPosition(_tpsCamController.LockOnTarget));
        }
        // 非ロックオン時の「カメラ正面を向く」処理は、
        // HandleHorizontalMovement 内での自由旋回に任せるためここでは行わない
    }

    /// <summary>
    /// 水平方向（前後左右）の移動計算
    /// </summary>
    /// <summary>
    /// 水平方向（前後左右）の移動計算とモデルの回転処理
    /// </summary>
    private Vector3 HandleHorizontalMovement()
    {
        // GetAxisRawを使用してレスポンスを向上
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h == 0f && v == 0f) return Vector3.zero;

        // 1. カメラの向き（Y軸のみ）を取得して基準軸を作る
        Vector3 cameraForward = _tpsCamController.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = _tpsCamController.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        // 2. カメラ基準の移動方向を計算
        Vector3 moveDir = (cameraForward * v + cameraRight * h).normalized;

        // 3. 【追加】入力がある場合、モデルをその進行方向に向ける
        // 攻撃中や硬直中でない時のみ実行
        if (moveDir != Vector3.zero && !_isAttacking && !_isStunned && !_isRestoringRotation)
        {
            // ロックオンしていない時は自由旋回
            if (_tpsCamController.LockOnTarget == null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                // スピードモードなので少し速めの 0.2f で回転（滑らかさ）
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
            }
        }

        // ダッシュ判定
        bool isDashing = Input.GetKey(KeyCode.LeftShift) && playerStatus.currentEnergy > 0.1f;
        float currentSpeed = (isDashing ? _moveSpeed * dashMultiplier : _moveSpeed) * _debuffMoveMultiplier;

        if (isDashing) playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);

        return moveDir * currentSpeed;
    }

    /// <summary>
    /// 垂直方向（ジャンプ・重力）の移動計算
    /// </summary>
    private Vector3 HandleVerticalMovement(bool isGrounded)
    {
        // 接地時は微小な下向き速度を維持（CharacterControllerの接地判定を安定させるため）
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool isFlyingUp = Input.GetKey(KeyCode.Space);
        if (canFly && isFlyingUp && playerStatus.currentEnergy > 0.1f)
        {
            // 上昇中
            _velocity.y = verticalSpeed * _debuffJumpMultiplier;
            playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        }
        else if (!isGrounded)
        {
            // 空中落下中（重力適用）
            float fallMult = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallMult;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    /// <summary>
    /// 攻撃中などの移動不可状態でも重力だけは適用させる処理
    /// </summary>
    private void ApplyGravityOnly(bool isGrounded)
    {
        if (!isGrounded)
        {
            float fallMult = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallMult;
        }
        else _velocity.y = -0.1f;

        _playerController.Move(Vector3.up * _velocity.y * Time.deltaTime);
    }
    #endregion

    #region 5. 攻撃アクション
    // =======================================================

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) PerformAttack(); // 左クリックで攻撃
        if (Input.GetKeyDown(KeyCode.E)) _modesAndVisuals.SwitchWeapon(); // 武器モード切替
        if (Input.GetKeyDown(KeyCode.Alpha1)) _modesAndVisuals.ChangeArmorBySlot(0); // アーマー1
        if (Input.GetKeyDown(KeyCode.Alpha2)) _modesAndVisuals.ChangeArmorBySlot(1); // アーマー2
    }

    /// <summary>
    /// 現在の武器モードに応じて攻撃ルーチンを開始する
    /// </summary>
    private void PerformAttack()
    {
        bool isAttack2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);
        _rotationBeforeAttack = transform.rotation; // 攻撃終了後に元の向きに戻すために保存

        // アニメーションの再生
        var speedAnim = GetComponentInChildren<SpeedAnimation>(true);
        if (speedAnim != null && speedAnim.gameObject.activeInHierarchy)
            speedAnim.PlayAttackAnimation(isAttack2);

        // ルーチンの開始
        if (isAttack2) StartCoroutine(HandleSpeedCrouchBeamRoutine());
        else StartCoroutine(HandleSpeedMeleeRoutine());
    }

    /// <summary>
    /// [コルーチン] スピードモード近接連撃ルーチン
    /// </summary>
    private IEnumerator HandleSpeedMeleeRoutine()
    {
        Animator anim = GetComponentInChildren<Animator>();
        _isAttacking = true;
        if (anim != null) anim.applyRootMotion = false; // 制御をスクリプトで行うためルートモーションを切る

        StartAttackStun(); // 硬直開始
        _stunTimer = 1.5f; // この攻撃全体の想定時間

        Quaternion attackRotation = transform.rotation; // 攻撃中の向きを固定

        // 1-3回目：通常振り
        for (int i = 0; i < 3; i++)
        {
            PlaySound(_meleeSwingSound);
            ApplyMeleeSphereDamage(meleeDamage);

            // 1振りの待機時間（向きを固定し続ける）
            float stepTimer = 0;
            while (stepTimer < 0.25f)
            {
                transform.rotation = attackRotation;
                stepTimer += Time.deltaTime;
                yield return null;
            }
        }

        // 4回目：トドメのひっかき
        yield return new WaitForSeconds(0.1f);
        PlaySound(_scratchAttackSound);
        ApplyMeleeSphereDamage(meleeDamage * 1.5f);

        // トドメの後のバックステップ（演出）
        float timer = 0f;
        while (timer < 0.25f)
        {
            transform.rotation = attackRotation;
            _playerController.Move(-transform.forward * 12f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.applyRootMotion = true;
        yield return StartCoroutine(RestoreRotationRoutine(_rotationBeforeAttack)); // 向きを滑らかに戻す
        ResetAttackStates();
    }

    /// <summary>
    /// [コルーチン] しゃがみビーム発射ルーチン
    /// </summary>
    private IEnumerator HandleSpeedCrouchBeamRoutine()
    {
        // エナジー不足なら中断
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) yield break;

        Animator anim = GetComponentInChildren<Animator>();
        _isAttacking = true;
        if (anim != null) anim.applyRootMotion = false;

        StartAttackStun();
        _stunTimer = 1.0f;

        Quaternion attackRotation = transform.rotation;

        // 溜め時間
        yield return WaitForRotationFixed(attackRotation, 0.3f);

        // ビーム発射実行
        ExecuteBeamLogic();

        // 発射後の後隙
        yield return WaitForRotationFixed(attackRotation, 0.7f);

        if (anim != null) anim.applyRootMotion = true;
        yield return StartCoroutine(RestoreRotationRoutine(_rotationBeforeAttack));
        ResetAttackStates();
    }
    #endregion

    #region 6. 射撃 & ダメージロジック
    // =======================================================

    /// <summary>
    /// ビームの発射と着弾判定
    /// </summary>
    private void ExecuteBeamLogic()
    {
        if (beamFirePoints == null || beamPrefab == null) return;

        // ターゲットの決定（ロックオンがいればその方向、いなければ正面）
        Transform lockOnTarget = _tpsCamController?.LockOnTarget;
        Vector3 targetPos = lockOnTarget != null
            ? GetLockOnTargetPosition(lockOnTarget, true)
            : transform.position + transform.forward * beamMaxDistance;

        foreach (var firePoint in beamFirePoints)
        {
            if (firePoint == null) continue;
            Vector3 origin = firePoint.position;
            Vector3 fireDirection = (targetPos - origin).normalized;

            // レイキャストによるヒット確認
            bool didHit = Physics.Raycast(origin, fireDirection, out RaycastHit hit, beamMaxDistance, ~0);
            Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            // ビームの生成と演出開始
            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    /// <summary>
    /// 前方の球体範囲内の敵にダメージを与える（近接用）
    /// </summary>
    private void ApplyMeleeSphereDamage(float damage)
    {
        // 判定の中心点を少し前にずらす
        Vector3 detectionCenter = transform.position + (Vector3.up * 1.0f) + (transform.forward * 1.5f);
        Collider[] hitColliders = Physics.OverlapSphere(detectionCenter, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform.root == transform.root) continue; // 自分自身は除外
            ApplyDamageToEnemy(hitCollider, damage);
        }
    }

    /// <summary>
    /// ヒットしたコライダーの親オブジェクトを走査し、敵コンポーネントがあればダメージを与える
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        bool isHit = false;

        // 親階層を遡って敵スクリプトを探す（複数の敵タイプに対応）
        Component[] components = hitCollider.GetComponentsInParent<Component>();
        foreach (var comp in components)
        {
            if (comp is SoldierMoveEnemy s1) { s1.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is SoliderEnemy s2) { s2.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is TutorialEnemyController s3) { s3.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is ScorpionEnemy s4) { s4.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is SuicideEnemy s5) { s5.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is DroneEnemy s6) { s6.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is VoxBodyPart s7) { s7.TakeDamage(damageAmount); isHit = true; break; }
            if (comp is VoxPart s8) { s8.TakeDamage(damageAmount); isHit = true; break; }
        }

        // ヒットした場合のみエフェクト生成
        if (isHit && hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
    }
    #endregion

    #region 7. 内部状態管理ユーティリティ
    // =======================================================

    /// <summary>
    /// スタン（硬直）時間の更新と、操作制限中かどうかの判定を行う
    /// </summary>
    private bool UpdateStunAndStates(bool isGrounded)
    {
        if (_isStunned)
        {
            _stunTimer -= Time.deltaTime;
            if (_stunTimer <= 0.0f)
            {
                _isStunned = false;
                if (isGrounded) _velocity.y = -0.1f;
            }
        }
        // 攻撃中、硬直中、向き復帰中のいずれかであれば true を返す
        return _isStunned || _isAttacking || _isRestoringRotation;
    }

    /// <summary>
    /// 攻撃時の硬直を開始する
    /// </summary>
    public void StartAttackStun()
    {
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = attackFixedDuration;
        _velocity.y = 0f; // 攻撃中に空中で止まるような演出用
    }

    /// <summary>
    /// 攻撃フラグをリセットする
    /// </summary>
    private void ResetAttackStates()
    {
        _isAttacking = false;
        _isStunned = false;
    }

    /// <summary>
    /// [コルーチン] 指定した角度へ滑らかに向きを戻す
    /// </summary>
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

    /// <summary>
    /// [コルーチン] 指定時間、向きを固定し続ける
    /// </summary>
    private IEnumerator WaitForRotationFixed(Quaternion rot, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            transform.rotation = rot;
            t += Time.deltaTime;
            yield return null;
        }
    }

    // 共通ヘルパー関数
    private void PlaySound(AudioClip clip) { if (_audioSource != null && clip != null) _audioSource.PlayOneShot(clip); }
    private void ApplyArmorStats() => _moveSpeed = baseMoveSpeed * (_modesAndVisuals.CurrentArmorStats != null ? _modesAndVisuals.CurrentArmorStats.moveSpeedMultiplier : 1.0f);
    private void RotateTowards(Vector3 target) { Vector3 dir = (target - transform.position).normalized; transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)); }
    private Vector3 GetLockOnTargetPosition(Transform t, bool offset = false) { if (t.TryGetComponent<Collider>(out var c)) return c.bounds.center; return offset ? t.position + Vector3.up * lockOnTargetHeightOffset : t.position; }

    // 外部（ダメージ床や敵からの攻撃）から呼ばれるダメージ処理
    public void TakeDamage(float amount) => playerStatus.TakeDamage(amount, (_modesAndVisuals.CurrentArmorStats != null) ? _modesAndVisuals.CurrentArmorStats.defenseMultiplier : 1.0f);
    public void SetDebuff(float moveMult, float jumpMult) { _debuffMoveMultiplier = moveMult; _debuffJumpMultiplier = jumpMult; }
    public void ResetDebuff() { _debuffMoveMultiplier = 1.0f; _debuffJumpMultiplier = 1.0f; }

    // シーンビューでのデバッグ用表示
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 previewCenter = transform.position + (Vector3.up * 1.0f) + (transform.forward * 1.5f);
        Gizmos.DrawWireSphere(previewCenter, meleeAttackRange);
    }
    #endregion
}