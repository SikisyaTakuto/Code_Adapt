using System.Collections;
using UnityEngine;

/// <summary>
/// 【バスター（重武装）モード制御クラス】
/// 概要: ビームやガトリングを用いた遠距離火力特化のアクションを制御します。
/// 特徴: 攻撃中の移動制限（硬直）、慣性を利用したバックステップ、自動エイム支援などを搭載。
/// </summary>
public class TutorialBusterController : MonoBehaviour
{
    #region 1. 依存コンポーネント & 関連オブジェクト
    // =======================================================

    [Header("Core Dependencies")]
    [SerializeField] private CharacterController _playerController; // 移動制御の主体
    [SerializeField] private TPSCameraController _tpsCamController; // カメラ・ロックオン情報の取得
    public TutorialPlayerModesAndVisuals  _modesAndVisuals;                // 武装・アーマーの切り替え管理
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
    public float beamDamage = 100.0f;       // ビーム1発の威力
    public float beamAttackEnergyCost = 50.0f; // ビーム消費EN
    public float beamMaxDistance = 100f;   // 射程距離
    public float lockOnTargetHeightOffset = 1.0f; // ロックオン対象の中心位置調整

    [Header("Special Attack (Full Burst)")]
    public float backstepForce = 20f;       // 全弾発射時の反動（後ろに飛ぶ力）
    public float gatlingFireRate = 0.05f;   // ガトリングの連射間隔（秒）
    public int gatlingBurstCount = 20;      // ガトリングの連射数
    public float gatlingEnergyCostTotal = 30f; // 特殊攻撃全体のEN消費量
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
        // _modesAndVisuals.CurrentArmorStats には合算された値が入っている
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
        bool isMode2 = (_modesAndVisuals.CurrentWeaponMode == TutorialPlayerModesAndVisuals.WeaponMode.Attack2);
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
    /// 特殊攻撃コルーチン: バックステップ → ビーム → ガトリング長連射
    /// </summary>
    private IEnumerator FullBurstRoutine()
    {
        _isAttacking = true;
        _isStunned = true;
        _rotationBeforeAttack = transform.rotation;

        // 【重要】開始時のターゲット座標をバックアップとして保存
        Vector3 initialTargetPos = GetAutoAimTargetPosition();
        RotateTowards(initialTargetPos);
        Quaternion attackRotation = transform.rotation;

        // 1. 【バックステップ】
        Vector3 backDir = -transform.forward;
        _velocity = backDir * backstepForce + Vector3.up * 2f;
        _stunTimer = 2.0f;

        // 2. 【第一波：メインビーム】
        if (playerStatus.ConsumeEnergy(beamAttackEnergyCost))
        {
            // initialTargetPos を使うことで確実に正面（または敵）に撃つ
            FireSpecificGuns(true, false, initialTargetPos, true);
        }

        yield return new WaitForSeconds(0.8f);

        // 3. 【第二波：ガトリング連射】
        int burstLimit = gatlingBurstCount * 4;
        float energyPerShot = gatlingEnergyCostTotal / burstLimit;

        for (int i = 0; i < burstLimit; i++)
        {
            if (playerStatus.currentEnergy <= 0.1f) break;
            playerStatus.ConsumeEnergy(energyPerShot);

            _isStunned = true;
            _stunTimer = 1.0f;

            // 【修正】ロックオンが生きている間は追跡し、外れたら最後にいた方向(initialTargetPos)を維持
            Vector3 currentTargetPos;
            if (_tpsCamController != null && _tpsCamController.LockOnTarget != null)
            {
                currentTargetPos = GetAutoAimTargetPosition();
                // 敵が動いている場合は、向き(attackRotation)も更新し続ける
                Vector3 dir = (currentTargetPos - transform.position).normalized;
                attackRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            }
            else
            {
                currentTargetPos = initialTargetPos;
            }

            // 揺れ計算
            float baseSway = Mathf.Sin(i * 0.3f) * 2.0f;
            FireSpecificGuns(false, true, currentTargetPos, true, baseSway);

            float shotInterval = 0;
            while (shotInterval < gatlingFireRate)
            {
                // 向きを強制固定（これでアニメーションによる回転を上書き）
                transform.rotation = attackRotation;
                shotInterval += Time.deltaTime;
                yield return null;
            }
        }

        // 4. 【後隙】
        float followThroughTime = 1.5f;
        float timer = 0;
        while (timer < followThroughTime)
        {
            _isStunned = true;
            _stunTimer = 0.5f;
            transform.rotation = attackRotation; // ここでも向きを固定
            timer += Time.deltaTime;
            yield return null;
        }

        transform.rotation = _rotationBeforeAttack;
        _isAttacking = false;
        _stunTimer = 0.01f;
    }
    #endregion

    #region 8. 発射・ダメージ処理 (Projectile & Damage)
    // =======================================================

    // 引数の最後に float swayAmount = 0 を追加
    private void FireSpecificGuns(bool useBeam, bool useGatling, Vector3 targetPosition, bool isLockedOn, float swayAmount = 0)
    {
        Vector3 playerForward = transform.forward;

        if (useBeam && beamFirePoints != null)
        {
            foreach (var fp in beamFirePoints)
                if (fp != null) FireProjectile(fp, targetPosition, isLockedOn, playerForward, true);
        }

        if (useGatling && gatlingFirePoints != null)
        {
            // forループに変更してインデックスを取得できるようにする
            for (int i = 0; i < gatlingFirePoints.Length; i++)
            {
                Transform fp = gatlingFirePoints[i];
                if (fp == null) continue;

                // インデックスが 0 なら右→左(そのまま)、1 なら左→右(反転)
                // 銃口が2つの場合、片方は swayAmount、もう片方は -swayAmount になる
                float individualSway = (i % 2 == 0) ? swayAmount : -swayAmount;
                Vector3 swayOffset = transform.right * individualSway;

                // 個別の揺れを加算して発射
                FireProjectile(fp, targetPosition + swayOffset, isLockedOn, playerForward, false);
            }
        }
    }

    private void FireProjectile(Transform firePoint, Vector3 targetPos, bool isLockedOn, Vector3 forward, bool isBeam)
    {
        // ??【修正】発射位置の微調整
        // 銃口の少し後ろ(0.5m)から判定を開始することで、敵の体に銃口が埋まっていてもヒットさせます。
        Vector3 originForRay = firePoint.position - firePoint.forward * 0.5f;
        Vector3 visualOrigin = firePoint.position; // 見た目の開始位置は銃口のまま

        Vector3 fireDirection = isLockedOn ? (targetPos - visualOrigin).normalized : forward;

        if (!isBeam)
        {
            float spread = 0.5f;
            Quaternion randomRotation = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0);
            fireDirection = randomRotation * fireDirection;
        }

        // --- 【修正ポイント】ガトリングの場合は Raycast でダメージを与えない ---
        if (isBeam)
        {
            // ??【修正】至近距離の救済判定 (OverlapSphere)
            // Raycastが内側から突き抜けてしまうのを防ぐため、銃口付近に敵がいるかチェック
            Collider[] closeHits = Physics.OverlapSphere(visualOrigin, 0.8f, enemyLayer);
            if (closeHits.Length > 0)
            {
                ApplyDamageToEnemy(closeHits[0], beamDamage);

                BeamController beam = Instantiate(beamPrefab, visualOrigin, Quaternion.LookRotation(fireDirection));
                beam.Fire(visualOrigin, closeHits[0].bounds.center, true);
                return;
            }

            // 通常のビーム判定（少し後ろからRayを飛ばす）
            bool didHit = Physics.Raycast(originForRay, fireDirection, out RaycastHit hit, beamMaxDistance + 0.5f, ~0, QueryTriggerInteraction.Ignore);
            Vector3 endPoint = didHit ? hit.point : visualOrigin + fireDirection * beamMaxDistance;

            if (didHit) ApplyDamageToEnemy(hit.collider, beamDamage);

            BeamController beamInstance = Instantiate(beamPrefab, visualOrigin, Quaternion.LookRotation(fireDirection));
            beamInstance.Fire(visualOrigin, endPoint, didHit);
        }
        else if (gatlingBulletPrefab != null)
        {
            // ??【修正】ガトリング弾の生成位置も少し調整
            // 銃口が埋まっていても弾が内側から発生して衝突するように、生成位置を少しだけ手前に引くか、
            // 弾丸側のスクリプト(GatlingBullet)に渡す情報を強化します。

            GameObject bulletObj = Instantiate(gatlingBulletPrefab, visualOrigin, Quaternion.LookRotation(fireDirection));
            GatlingBullet bullet = bulletObj.GetComponent<GatlingBullet>();
            if (bullet != null) bullet.Launch(fireDirection);
        }
    }

    /// <summary>
    /// 命中したオブジェクトのコンポーネントをチェックし、適切なダメージメソッドを呼び出します。
    /// </summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damage)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 1. ボス・特殊部位への判定
        var hurtBox = target.GetComponent<BossHurtBox>();
        if (hurtBox == null) hurtBox = target.GetComponentInParent<BossHurtBox>();

        if (hurtBox != null)
        {
            hurtBox.OnHit(damage);
            isHit = true;
        }
        else if (target.TryGetComponent<ElsController>(out var boss))
        {
            boss.TakeDamage(damage);
            isHit = true;
        }

        // 2. ★【修正ポイント】サンドバッグ（TutorialEnemyController）へのダメージ判定を追加
        // 親階層も含めて検索することで、子オブジェクトのコライダーに当たっても反応するようにします
        var sandbag = target.GetComponentInParent<EnemyControllerTest>();

        if (sandbag != null)
        {
            sandbag.TakeDamage(damage);
            isHit = true;
        }

        // 3. 雑魚敵の種類に応じてダメージを適用
        if (!isHit) // まだヒット判定が出ていない場合のみ続行
        {
            if (target.TryGetComponent<SoldierMoveEnemy>(out var s1)) { s1.TakeDamage(damage); isHit = true; }
            else if (target.TryGetComponent<SoliderEnemy>(out var s2)) { s2.TakeDamage(damage); isHit = true; }
            else if (target.TryGetComponent<ScorpionEnemy>(out var s4)) { s4.TakeDamage(damage); isHit = true; }
            else if (target.TryGetComponent<SuicideEnemy>(out var s5)) { s5.TakeDamage(damage); isHit = true; }
            else if (target.TryGetComponent<DroneEnemy>(out var s6)) { s6.TakeDamage(damage); isHit = true; }
            else if (target.TryGetComponent<VoxBodyPart>(out var body)) { body.TakeDamage(damage); isHit = true; }
            else if (target.TryGetComponent<VoxPart>(out var part)) { part.TakeDamage(damage); isHit = true; }
        }

        // ヒットした場合、中心に火花などのエフェクトを出す
        if (isHit && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitCollider.bounds.center, Quaternion.identity);
        }
    }

    public void TakeDamage(float damage)
    {
        var stats = _modesAndVisuals.CurrentArmorStats;
        // stats.defenseMultiplier が 0.8 なら 20% 軽減してダメージを渡す
        float defense = (stats != null) ? stats.defenseMultiplier : 1.0f;
        playerStatus.TakeDamage(damage, defense);
    }
    #endregion

    #region 9. ユーティリティ (Utilities)
    // =======================================================

    /// <summary>
    /// 攻撃の目標地点を計算する。
    /// ロックオン中ならその敵の座標、そうでなければプレイヤー正面の遠くの座標を返す。
    /// </summary>
    private Vector3 GetAutoAimTargetPosition()
    {
        // 1. ロックオン対象がいればその中心を取得
        if (_tpsCamController != null && _tpsCamController.LockOnTarget != null)
        {
            // GetLockOnTargetPositionを使用して、コライダーの中心などを計算
            return GetLockOnTargetPosition(_tpsCamController.LockOnTarget, true);
        }

        // 2. 非ロックオン時：プレイヤーの正面(transform.forward)へ真っ直ぐ飛ばす
        // beamMaxDistance（射程）の分だけ先の座標をターゲット地点とする
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
        // 1キー => Index 0 (Speed/Normal)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _modesAndVisuals.ChangeArmorDirect(0);
        }
        // 2キー => Index 1 (Buster)
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _modesAndVisuals.ChangeArmorDirect(1);
        }
        // 3キー => Index 2 (Balance/Speed)
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _modesAndVisuals.ChangeArmorDirect(2);
        }
    }

    public void SetDebuff(float moveMult, float jumpMult) { _debuffMoveMultiplier = moveMult; _debuffJumpMultiplier = jumpMult; }
    public void ResetDebuff() { _debuffMoveMultiplier = 1.0f; _debuffJumpMultiplier = 1.0f; }
    #endregion
}