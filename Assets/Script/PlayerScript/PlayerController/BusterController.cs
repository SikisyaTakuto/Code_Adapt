using System.Collections;
using UnityEngine;

/// <summary>
/// 【バスター（重武装）モード制御クラス】
/// 概要: ビームやガトリングを用いた遠距離火力特化のアクションを制御します。
/// 特徴: 攻撃中の移動制限（硬直）、慣性を利用したバックステップ、自動エイム支援などを搭載。
/// </summary>
public class BusterController : MonoBehaviour
{
    #region 1. 依存コンポーネント & 関連オブジェクト
    // =======================================================

    [Header("Core Dependencies")]
    [SerializeField] private CharacterController _playerController; // 移動制御の主体
    [SerializeField] private TPSCameraController _tpsCamController; // カメラ・ロックオン情報の取得
    public PlayerModesAndVisuals _modesAndVisuals;                // 武装・アーマーの切り替え管理
    public PlayerStatus playerStatus;                             // ステータス（HP/EN/死亡フラグ）の管理元

    [Header("VFX & Fire Points")]
    public BeamController beamPrefab;      // ビーム攻撃の実体
    public Transform[] beamFirePoints;      // 肩部など：高火力ビームの発射点
    public Transform[] gatlingFirePoints;   // 腕部など：連射ガトリングの発射点
    public GameObject gatlingBulletPrefab; // ガトリング弾のプレハブ
    public GameObject hitEffectPrefab;     // 敵着弾時の火花などのエフェクト
    public LayerMask enemyLayer;           // 攻撃が当たるレイヤー設定

    private BusterAnimation _busterAnim;    // アニメーション制御用キャッシュ
    #endregion

    #region 2. 設定パラメータ (調整用)
    // =======================================================

    [Header("Movement Settings")]
    public float baseMoveSpeed = 15.0f;   // 通常移動速度
    public float dashMultiplier = 2.5f;   // ダッシュ（ブースト）時の倍率
    public float verticalSpeed = 10.0f;   // 上昇速度
    public float gravity = -9.81f;        // 重力の強さ
    public float fastFallMultiplier = 3.0f; // 落下を速める倍率（滞空感の調整）
    public bool canFly = true;            // 飛行可能フラグ

    [Header("Stun & Timings")]
    public float attackFixedDuration = 0.8f; // 通常攻撃時の操作不能時間
    public float landStunDuration = 0.2f;    // 高所着地時の隙

    [Header("Combat Settings")]
    public float beamDamage = 50.0f;       // ビーム1発の威力
    public float beamAttackEnergyCost = 30.0f; // ビーム消費EN
    public float beamMaxDistance = 100f;   // 射程距離
    public float lockOnTargetHeightOffset = 1.0f; // ロックオン対象の中心位置調整

    [Header("Special Attack (Full Burst)")]
    public float backstepForce = 20f;       // 全弾発射時の反動（後ろに飛ぶ力）
    public float gatlingFireRate = 0.05f;   // ガトリングの連射間隔（秒）
    public int gatlingBurstCount = 10;      // ガトリングの連射数
    public float gatlingEnergyCostTotal = 10f; // 特殊攻撃全体のEN消費量
    #endregion

    #region 3. 内部ステータス & キャッシュ
    // =======================================================

    private bool _isAttacking = false;      // 現在攻撃アクション中か
    private bool _isStunned = false;        // 現在硬直（操作不能）状態か
    private float _stunTimer = 0.0f;        // 硬直終了までのカウントダウン
    private Quaternion _rotationBeforeAttack; // 攻撃終了後に元の向きに戻すための保存用

    private Vector3 _velocity;              // 現在の移動速度（慣性・重力含む）
    private float _moveSpeed;               // アーマー性能適用後の最終速度
    private bool _wasGrounded = false;      // 前フレームの接地判定
    private bool _isBoosting = false;       // 入力システムからのブーストフラグ
    private float _verticalInput = 0f;      // 上下移動の入力値

    private float _debuffMoveMultiplier = 1.0f; // 鈍い動き等のデバフ倍率
    private float _debuffJumpMultiplier = 1.0f;
    #endregion

    #region 4. ライフサイクル (Life Cycle)
    // =======================================================

    void Awake()
    {
        InitializeComponents();
        _busterAnim = GetComponentInChildren<BusterAnimation>();
    }

    void Update()
    {
        // 【死亡判定】体力がゼロならすべての更新を停止
        if (playerStatus == null || playerStatus.IsDead) return;

        bool isGroundedNow = _playerController.isGrounded;

        // 【硬直管理】タイマーを減らし、ゼロになったら硬直を解除する
        UpdateStunTimer(isGroundedNow);

        if (_isStunned)
        {
            // 硬直中の挙動：重力と、攻撃による慣性移動（バックステップ等）のみを処理
            HandleStunnedPhysics(isGroundedNow);
            _wasGrounded = isGroundedNow;
            return;
        }

        // 【回転制御】非攻撃時、カメラの向きやロックオン対象へ体を向ける
        HandleRotation();

        // 【移動性能更新】現在のアーマー補正を適用
        ApplyArmorStats();

        // 【入力・移動実行】
        HandleInput();
        Vector3 finalMove = HandleVerticalMovement(isGroundedNow) + HandleHorizontalMovement();
        _playerController.Move(finalMove * Time.deltaTime);

        _wasGrounded = isGroundedNow;
    }

    private void InitializeComponents()
    {
        // 依存している各コンポーネントを自動取得（手動設定も可）
        if (_playerController == null) _playerController = GetComponentInParent<CharacterController>();
        if (_tpsCamController == null) _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (playerStatus == null) playerStatus = GetComponentInParent<PlayerStatus>();
    }
    #endregion

    #region 5. 移動・回転制御 (Movement & Rotation)
    // =======================================================

    private void HandleRotation()
    {
        if (_tpsCamController == null || _isAttacking) return;

        // ロックオン中のみ、強制的にターゲットの方向を維持する
        if (_tpsCamController.LockOnTarget != null)
        {
            RotateTowards(GetLockOnTargetPosition(_tpsCamController.LockOnTarget));
        }
        // 非ロックオン時の「カメラ正面を向く」処理は、
        // WASD移動による自由回転を優先するためここでは削除（HandleHorizontalMovementに統合）
    }

    private Vector3 HandleHorizontalMovement()
    {
        // GetAxisよりもレスポンスが良いGetAxisRawを推奨
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h == 0f && v == 0f) return Vector3.zero;

        // 1. カメラの向き（Y軸のみ）を取得
        Vector3 cameraForward = _tpsCamController.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = _tpsCamController.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        // 2. カメラの向きを軸とした「入力方向」の移動ベクトルを作成
        Vector3 moveDirection = (cameraForward * v + cameraRight * h).normalized;

        // 3. 【修正】入力がある場合、モデルをその進行方向に向ける
        if (moveDirection != Vector3.zero && !_isAttacking)
        {
            // ロックオン中でない場合、または移動を優先したい場合に回転
            if (_tpsCamController.LockOnTarget == null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                // 0.15f は回転の滑らかさ。重装甲感を出すなら少し小さめ（0.1f等）でもOK
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.15f);
            }
        }

        float currentSpeed = _moveSpeed * _debuffMoveMultiplier;

        // ブースト処理
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
        // 地面にいる場合は微小な下向きの力を与え、接地判定を安定させる
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool isFlyingUp = (Input.GetKey(KeyCode.Space) || _verticalInput > 0.5f);
        bool hasVerticalInput = false;

        // 【飛行上昇】EN消費を伴う上昇処理
        if (canFly && playerStatus.currentEnergy > 0.1f && isFlyingUp)
        {
            _velocity.y = verticalSpeed * _debuffJumpMultiplier;
            hasVerticalInput = true;
        }

        if (hasVerticalInput) playerStatus.ConsumeEnergy(15.0f * Time.deltaTime);
        else if (!isGrounded)
        {
            // 【空中落下】重力を適用。落下中（y < 0）はより速く落ちるよう倍率をかける
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }

        // EN切れによる失速
        if (playerStatus.currentEnergy <= 0.1f && _velocity.y > 0) _velocity.y = 0;

        return new Vector3(0, _velocity.y, 0);
    }

    private void ApplyArmorStats()
    {
        // アーマーごとの移動速度倍率を適用
        var stats = _modesAndVisuals.CurrentArmorStats;
        _moveSpeed = baseMoveSpeed * (stats != null ? stats.moveSpeedMultiplier : 1.0f);
    }
    #endregion

    #region 6. 硬直ロジック (Stun Logic)
    // =======================================================

    private void UpdateStunTimer(bool isGrounded)
    {
        if (!_isStunned) return;

        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0.0f)
        {
            _isStunned = false;
            _isAttacking = false;
            // 硬直明け、地面なら落下ベクトルをリセット
            if (isGrounded) _velocity.y = -0.1f;
        }
    }

    private void HandleStunnedPhysics(bool isGroundedNow)
    {
        // 硬直中の垂直移動（落下のみ許可）
        if (!isGroundedNow)
        {
            float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
            _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
        }
        else _velocity.y = -0.1f;

        // 硬直中の水平移動（摩擦による減速計算）
        // 攻撃の反動で飛ばされた際、徐々に止まるようにLerpを使用
        float friction = 5f;
        _velocity.x = Mathf.Lerp(_velocity.x, 0, Time.deltaTime * friction);
        _velocity.z = Mathf.Lerp(_velocity.z, 0, Time.deltaTime * friction);

        _playerController.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// 攻撃による硬直を開始させる命令
    /// </summary>
    public void StartAttackStun()
    {
        _isAttacking = true;
        _isStunned = true;
        _stunTimer = attackFixedDuration;
        _velocity.y = 0f; // 空中射撃時の高度維持
    }
    #endregion

    #region 7. 攻撃ロジック (Combat Logic)
    // =======================================================

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) PerformAttack();
        HandleWeaponSwitchInput();
        HandleArmorSwitchInput();
    }

    private void PerformAttack()
    {
        if (_isAttacking || _isStunned) return;

        // WeaponMode設定に応じて、通常攻撃か特殊攻撃（Attack2）かを判定
        bool isMode2 = (_modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2);
        if (_busterAnim != null) _busterAnim.PlayAttackAnimation(isMode2);

        // 射撃を開始する瞬間のターゲット座標を取得
        Vector3 targetPos = GetAutoAimTargetPosition();

        if (isMode2)
            StartCoroutine(FullBurstRoutine()); // 全弾発射
        else
            StartCoroutine(HandleAttack1Routine(targetPos)); // 単発ビーム
    }

    /// <summary>
    /// 通常攻撃コルーチン: 回転を一時的にロックしてビームを放つ
    /// </summary>
    private IEnumerator HandleAttack1Routine(Vector3 targetPos)
    {
        if (!playerStatus.ConsumeEnergy(beamAttackEnergyCost)) yield break;

        _isAttacking = true;
        _rotationBeforeAttack = transform.rotation; // 射撃前の向きを保存

        // ターゲットに一瞬で向きを合わせる
        RotateTowards(targetPos);
        Quaternion attackRotation = transform.rotation;

        StartAttackStun();
        FireSpecificGuns(true, false, targetPos, true);

        // 【回転固定ループ】
        // アニメーションのルートモーションなどで体が勝手に回るのを防ぐ
        float elapsed = 0;
        while (elapsed < attackFixedDuration)
        {
            transform.rotation = attackRotation;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 射撃終了後、元の体の向き（カメラ方向など）に復帰
        transform.rotation = _rotationBeforeAttack;
        _isAttacking = false;
    }

    /// <summary>
    /// 特殊攻撃コルーチン: バックステップ → ビーム → ガトリング連射 のコンボ
    /// </summary>
    private IEnumerator FullBurstRoutine()
    {
        _isAttacking = true;
        _isStunned = true;
        _rotationBeforeAttack = transform.rotation;

        Vector3 initialTargetPos = GetAutoAimTargetPosition();
        RotateTowards(initialTargetPos);
        Quaternion attackRotation = transform.rotation;

        // 【バックステップ】後方へのベクトルと、わずかな浮き上がりを付与
        Vector3 backDir = -transform.forward;
        _velocity = backDir * backstepForce + Vector3.up * 2f;
        _stunTimer = 5.0f; // ループ制御のため一旦長めに設定

        // 第一波：メインビーム発射
        if (playerStatus.ConsumeEnergy(beamAttackEnergyCost))
        {
            FireSpecificGuns(true, false, initialTargetPos, true);
        }

        // 発射後の溜め時間（硬直）
        float timer = 0;
        while (timer < 1.0f)
        {
            transform.rotation = attackRotation;
            timer += Time.deltaTime;
            yield return null;
        }

        // 第二波：ガトリング連射開始
        if (playerStatus.ConsumeEnergy(gatlingEnergyCostTotal))
        {
            for (int i = 0; i < gatlingBurstCount; i++)
            {
                _isStunned = true;
                _stunTimer = 1.0f; // 連射中は常に硬直時間を上書き維持

                // 連射中も微調整としてターゲットを追い続ける
                Vector3 currentTargetPos = GetAutoAimTargetPosition();
                RotateTowards(currentTargetPos);
                attackRotation = transform.rotation;

                FireSpecificGuns(false, true, currentTargetPos, true);

                // 連射間隔待機中も回転を固定
                float shotInterval = 0;
                while (shotInterval < gatlingFireRate)
                {
                    transform.rotation = attackRotation;
                    shotInterval += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 【残心】すべての弾を撃ち終えた後のフォロースルー時間
        timer = 0;
        while (timer < 0.6f)
        {
            transform.rotation = attackRotation;
            timer += Time.deltaTime;
            yield return null;
        }

        // 状態をリセットして通常移動へ戻す
        transform.rotation = _rotationBeforeAttack;
        _isAttacking = false;
        _stunTimer = 0.01f;
    }
    #endregion

    #region 8. 発射・ダメージ処理 (Projectile & Damage)
    // =======================================================

    private void FireSpecificGuns(bool useBeam, bool useGatling, Vector3 targetPosition, bool isLockedOn)
    {
        Vector3 playerForward = transform.forward;

        // 複数ある発射ポイント（FirePoints）を巡回してプレハブを生成
        if (useBeam && beamFirePoints != null)
        {
            foreach (var fp in beamFirePoints)
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, true);
        }

        if (useGatling && gatlingFirePoints != null)
        {
            foreach (var fp in gatlingFirePoints)
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, false);
        }
    }

    private void FireProjectile(Transform firePoint, Vector3 targetPos, bool isLockedOn, Vector3 forward, bool isBeam)
    {
        Vector3 origin = firePoint.position;
        // ロックオン時は敵の方向、非ロックオン時は正面へ飛ばす
        Vector3 fireDirection = isLockedOn ? (targetPos - origin).normalized : forward;

        // 【即着弾判定】レイキャストを使用して壁や敵に当たるか確認
        bool didHit = Physics.Raycast(origin, fireDirection, out RaycastHit hit, beamMaxDistance, ~0);
        Vector3 endPoint = didHit ? hit.point : origin + fireDirection * beamMaxDistance;

        // ダメージ適用
        if (didHit) ApplyDamageToEnemy(hit.collider, isBeam ? beamDamage : 10.0f);

        // ビームまたは弾丸の視覚エフェクト生成
        if (isBeam)
        {
            BeamController beamInstance = Instantiate(beamPrefab, origin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(origin, endPoint, didHit);
        }
        else if (gatlingBulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(gatlingBulletPrefab, origin, Quaternion.LookRotation(fireDirection));
            GatlingBullet bullet = bulletObj.GetComponent<GatlingBullet>();
            if (bullet != null) bullet.Launch(fireDirection);
        }
    }

    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // --- 1. ボス(TestBoss)への判定を追加 ---
        var boss = target.GetComponentInParent<TestBoss>();
        if (boss != null)
        {
            boss.TakeDamage(damageAmount);
            isHit = true;
        }

        // 敵の各部位や種類に応じたダメージスクリプトの取得を試みる（ポリモーフィズムがない場合の暫定処理）
        if (target.TryGetComponent<SoldierMoveEnemy>(out var s1)) { s1.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SoliderEnemy>(out var s2)) { s2.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<TutorialEnemyController>(out var s3)) { s3.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var s4)) { s4.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var s5)) { s5.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var s6)) { s6.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<VoxBodyPart>(out var bodyPart)) { bodyPart.TakeDamage(damageAmount); isHit = true; }
        else if (target.TryGetComponent<VoxPart>(out var part)) { part.TakeDamage(damageAmount); isHit = true; }

        // ヒット時エフェクトの生成
        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // アーマーの防御力係数を取得してダメージ計算を委譲
        var stats = _modesAndVisuals.CurrentArmorStats;
        float defense = (stats != null) ? stats.defenseMultiplier : 1.0f;
        playerStatus.TakeDamage(damageAmount, defense);
    }
    #endregion

    #region 9. ユーティリティ (Utilities)
    // =======================================================

    /// <summary>
    /// ロックオン中ならターゲット、そうでなければ画面中央（レティクル）のワールド座標を返す
    /// </summary>
    private Vector3 GetAutoAimTargetPosition()
    {
        // 1. ロックオン対象がいればその中心
        if (_tpsCamController?.LockOnTarget != null)
            return GetLockOnTargetPosition(_tpsCamController.LockOnTarget, true);

        // 2. 非ロックオン時：カメラの中心から正面にレイを飛ばしてヒットした地点をターゲットにする
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, beamMaxDistance, ~0)) return hit.point;
            return ray.origin + ray.direction * beamMaxDistance;
        }

        return transform.position + transform.forward * beamMaxDistance;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        // 高度（y）を無視して、水平方向の向きを計算
        Vector3 dir = (targetPosition - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
    }

    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffset = false)
    {
        // ターゲットにコライダーがあればその中心、なければ足元＋オフセット
        if (target.TryGetComponent<Collider>(out var col)) return col.bounds.center;
        return useOffset ? target.position + Vector3.up * lockOnTargetHeightOffset : target.position;
    }

    private void HandleWeaponSwitchInput() { if (Input.GetKeyDown(KeyCode.E)) _modesAndVisuals.SwitchWeapon(); }
    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) _modesAndVisuals.ChangeArmorBySlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) _modesAndVisuals.ChangeArmorBySlot(1);
    }

    public void SetDebuff(float moveMult, float jumpMult) { _debuffMoveMultiplier = moveMult; _debuffJumpMultiplier = jumpMult; }
    public void ResetDebuff() { _debuffMoveMultiplier = 1.0f; _debuffJumpMultiplier = 1.0f; }
    #endregion
}