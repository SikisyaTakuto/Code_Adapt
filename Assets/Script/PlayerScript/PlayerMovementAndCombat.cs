using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// プレイヤーの移動、基本的な攻撃実行、およびHP/ダメージ処理を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementAndCombat : MonoBehaviour
{
    // =======================================================
    // I. データ構造 (Configuration Structs)
    // =======================================================

    [System.Serializable]
    public class ModeBeamConfiguration
    {
        [Tooltip("このモードでビームを発射するすべてのポイント (Transform)。")]
        public List<Transform> firePoints = new List<Transform>();
    }

    // =======================================================
    // II. 公開設定 (Public/Serialized Fields)
    // =======================================================

    [Header("1. Dependencies (依存関係)")]
    [Tooltip("アーマーモード、エネルギー管理、UI更新を制御するコントローラー。")]
    public PlayerModeController modeController;
    [Tooltip("カメラ、ロックオン、プレイヤーの回転を制御するコントローラー。")]
    public TPSCameraController tpsCamController;
    [Tooltip("ゲームオーバー処理を担当するマネージャー。")]
    public SceneBasedGameOverManager gameOverManager;

    // --- Movement & Physics ---
    [Header("2. Movement Stats (移動・物理設定)")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
    public float verticalSpeed = 10.0f;
    public float gravity = -9.81f;
    [Tooltip("標準重力に対する落下速度の乗数")]
    public float fastFallMultiplier = 3.0f;
    public bool canFly = true;

    [Header("3. Stun Settings (硬直設定)")]
    [Tooltip("着地時の硬直時間 (秒)。")]
    public float landingStunDuration = 0.2f;

    // --- Combat Settings ---
    [Header("4. Melee Attack Settings (近接攻撃設定)")]
    public float attackFixedDuration = 1.5f; // ★ ここを 0.8f から 1.5f に変更しました
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public LayerMask enemyLayer;
    public GameObject hitEffectPrefab;

    [Header("5. Beam Attack Settings (ビーム攻撃設定)")]
    public BeamController beamPrefab;
    [Tooltip("Normal(0: 2点), Buster(1: 4点), Speed(2: 2点)の順で設定します。")]
    public List<ModeBeamConfiguration> modeBeamConfigurations = new List<ModeBeamConfiguration>(3)
    {
        new ModeBeamConfiguration(),
        new ModeBeamConfiguration(),
        new ModeBeamConfiguration()
    };
    public float beamMaxDistance = 100f;
    public float beamDamage = 50.0f;
    [Tooltip("ロックオン時に敵のColliderがない場合、ビームを狙う高さのオフセット。")]
    public float lockOnTargetHeightOffset = 1.0f;

    [Header("6. Health & Damage Settings (HP・ダメージ設定)")]
    public float maxHP = 10000.0f;
    [Tooltip("受けたダメージがこの値を超えないように制限する。(一撃死対策の応急処置)")]
    public float damageCap = 10000.0f;


    // =======================================================
    // III. プライベート/キャッシュ変数 & HPプロパティ
    // =======================================================

    // HPプロパティ (読み取り専用)
    private float _currentHP;
    public float currentHP { get => _currentHP; private set => _currentHP = value; }

    // キャッシュ
    private CharacterController _controller;
    private Vector3 _velocity;

    // 状態フラグ
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private bool _isDead = false;

    // 硬直・接地状態
    private bool _isLandingStunned = false;
    private float _landingStunTimer = 0.0f;
    private bool _wasGroundedLastFrame = false; // 前のフレームの接地状態を記憶

    // 入力変数 (PlayerModeControllerと共有)
    [HideInInspector] public bool isBoosting = false;
    [HideInInspector] public float verticalInput = 0f;

    // =======================================================
    // IV. Unity Lifecycle Methods
    // =======================================================

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        currentHP = maxHP;

        if (modeController == null) Debug.LogError("PlayerModeController が設定されていません。");

        // 初期状態を検出するために、Awake時に現在の接地状態を保持
        _wasGroundedLastFrame = _controller.isGrounded;
    }

    void Start()
    {
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();
        }
    }

    void Update()
    {
        if (_isDead) return;

        bool isCurrentlyGrounded = _controller.isGrounded;

        // 1. 着地判定と硬直開始
        HandleLandingStunCheck(isCurrentlyGrounded);

        // 2. 硬直中の処理
        if (_isAttacking || _isLandingStunned)
        {
            HandleAttackState();
            HandleLandingStunState();

            // 攻撃硬直中は回転も完全に停止させる
            // 着地硬直中のみ回転処理を許可
            if (!_isAttacking)
            {
                HandleRotationWhileNotAttackingStunned();
            }

            _wasGroundedLastFrame = isCurrentlyGrounded; // 状態を保存
            return; // 完全硬直
        }

        // 3. 通常の移動・回転処理
        HandleNormalRotation();

        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
        _controller.Move(finalMove * Time.deltaTime);

        // 4. 次のフレームのための接地状態更新
        _wasGroundedLastFrame = isCurrentlyGrounded;
    }

    // =======================================================
    // V. Movement Logic (移動ロジック)
    // =======================================================

    /// <summary>水平方向の移動（前後左右）を計算します。</summary>
    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxis("Horizontal"); // 左スティックX
        float v = Input.GetAxis("Vertical");   // 左スティックY

        if (h == 0f && v == 0f) return Vector3.zero;

        Vector3 inputDirection = new Vector3(h, 0, v);
        Vector3 moveDirection;

        // カメラの向きを考慮した移動方向の計算
        if (tpsCamController != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraRotation * inputDirection;
        }
        else
        {
            moveDirection = transform.right * h + transform.forward * v;
        }

        moveDirection.Normalize();

        float currentSpeed = baseMoveSpeed * modeController.currentArmorStats.moveSpeedMultiplier;

        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || isBoosting) && modeController.currentEnergy > 0.01f;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            // エネルギー消費
            modeController.ConsumeEnergy(modeController.energyConsumptionRate * Time.deltaTime);
            modeController.ResetEnergyRecoveryTimer();
        }

        return moveDirection * currentSpeed;
    }

    /// <summary>垂直方向の移動（ジャンプ、落下、飛行）を計算し、重力を適用します。</summary>
    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _controller.isGrounded;

        // 接地している場合は垂直速度をリセット（CharacterControllerのめり込み防止）
        if (isGrounded && _velocity.y < -0.1f)
        {
            _velocity.y = -0.1f;
        }

        bool hasVerticalInput = false;

        bool isFlyingUp = Input.GetKey(KeyCode.Space) || verticalInput > 0.5f;
        bool isFlyingDown = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) || verticalInput < -0.5f;

        if (canFly && modeController.currentEnergy > 0.01f)
        {
            if (isFlyingUp)
            {
                _velocity.y = verticalSpeed;
                hasVerticalInput = true;
            }
            else if (isFlyingDown)
            {
                _velocity.y = -verticalSpeed;
                hasVerticalInput = true;
            }
        }

        if (hasVerticalInput)
        {
            // 飛行によるエネルギー消費
            modeController.ConsumeEnergy(modeController.energyConsumptionRate * Time.deltaTime);
            modeController.ResetEnergyRecoveryTimer();
        }
        else
        {
            // 重力と落下処理
            if (!isGrounded)
            {
                // 加速落下処理
                float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
                _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
            }
        }

        // エネルギー切れで上昇中の場合は停止
        if (modeController.currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    // =======================================================
    // VI. Combat & State Logic (戦闘・状態ロジック)
    // =======================================================

    /// <summary>現在の武器モードに基づいて攻撃を実行します。</summary>
    public void Attack()
    {
        // 攻撃中または着地硬直中は攻撃不可
        if (_isAttacking || _isLandingStunned) return;

        switch (modeController.currentWeaponMode)
        {
            case PlayerModeController.WeaponMode.Melee:
                HandleMeleeAttack();
                break;
            case PlayerModeController.WeaponMode.Beam:
                HandleBeamAttack();
                break;
        }
    }

    /// <summary>近接攻撃を実行します。</summary>
    private void HandleMeleeAttack()
    {
        StartAttackStun();

        Transform lockOnTarget = tpsCamController?.LockOnTarget;

        // メレー攻撃開始時にターゲットに回転する処理は維持
        if (lockOnTarget != null)
        {
            RotateTowards(GetLockOnTargetPosition(lockOnTarget));
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;
            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    /// <summary>ビーム攻撃を実行します。</summary>
    private void HandleBeamAttack()
    {
        // 設定チェック
        int modeIndex = (int)modeController.currentArmorMode;
        ModeBeamConfiguration config = GetCurrentBeamConfig(modeIndex);

        if (config == null || config.firePoints.Count == 0 || beamPrefab == null)
        {
            Debug.LogError($"現在のアーマーモード({modeController.currentArmorMode})に対応するビームの設定または発射点が不足しています。");
            return;
        }

        // エネルギーチェックと消費
        if (!ConsumeEnergyForBeamAttack()) return;

        StartAttackStun();

        // 全ての発射ポイントをループしてビームを生成する
        Transform lockOnTarget = tpsCamController?.LockOnTarget;
        bool isFirstFirePoint = true;

        foreach (Transform firePoint in config.firePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection;
            Vector3 targetPosition = Vector3.zero;

            // ロックオンターゲットの決定と回転 (ビームはロックオンを優先)
            if (lockOnTarget != null)
            {
                targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                fireDirection = (targetPosition - origin).normalized;

                // プレイヤー本体の回転は一度で十分 (攻撃開始時のみ)
                if (isFirstFirePoint)
                {
                    RotateTowards(targetPosition);
                    isFirstFirePoint = false;
                }
            }
            else
            {
                fireDirection = firePoint.forward;
            }

            // Raycastとダメージ処理
            RaycastHit hit;
            Vector3 endPoint;
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

            if (didHit)
            {
                endPoint = hit.point;
                ApplyDamageToEnemy(hit.collider, beamDamage);
            }
            else
            {
                endPoint = origin + fireDirection * beamMaxDistance;
            }

            // ビームの生成
            BeamController beamInstance = Instantiate(
                beamPrefab,
                origin,
                Quaternion.LookRotation(fireDirection)
            );
            beamInstance.Fire(origin, endPoint, didHit);
        }
    }

    /// <summary>攻撃硬直を開始します。</summary>
    private void StartAttackStun()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        // 攻撃中は垂直速度をリセット（空中での動作を安定させるため）
        _velocity.y = 0f;
    }

    /// <summary>攻撃硬直タイマーを管理します。</summary>
    private void HandleAttackState()
    {
        if (!_isAttacking) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            // 硬直解除後の垂直速度リセット（接地維持/落下再開）
            if (_controller.isGrounded)
            {
                _velocity.y = -0.1f;
            }
        }
    }

    /// <summary>着地硬直の条件をチェックし、硬直を開始します。</summary>
    private void HandleLandingStunCheck(bool isCurrentlyGrounded)
    {
        // 浮いていた状態から着地し、かつ速度が速い場合
        if (!_wasGroundedLastFrame && isCurrentlyGrounded && !_isAttacking && _velocity.y < -1f)
        {
            _isLandingStunned = true;
            _landingStunTimer = 0f;
            _velocity.y = -0.1f; // めり込み防止
        }
    }

    /// <summary>着地硬直タイマーを管理します。</summary>
    private void HandleLandingStunState()
    {
        if (!_isLandingStunned) return;

        _landingStunTimer += Time.deltaTime;
        if (_landingStunTimer >= landingStunDuration)
        {
            _isLandingStunned = false;
            _landingStunTimer = 0f;

            // 硬直解除後の垂直速度リセット
            if (_controller.isGrounded)
            {
                _velocity.y = -0.1f;
            }
        }
    }

    // =======================================================
    // VII. Damage & Death Logic (ダメージ・死亡ロジック)
    // =======================================================

    /// <summary>ダメージを受け、HPを減少させます。</summary>
    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        // 防御補正を適用
        float finalDamage = damageAmount * modeController.currentArmorStats.defenseMultiplier;

        // ダメージキャップを適用
        if (damageCap > 0)
        {
            finalDamage = Mathf.Min(finalDamage, damageCap);
        }

        currentHP -= finalDamage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        modeController.UpdateHPUI();

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            Debug.Log($"ダメージを受けました。最終ダメージ: {finalDamage}, 残りHP: {currentHP}");
        }
    }

    /// <summary>死亡処理を実行します。</summary>
    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        if (gameOverManager != null)
        {
            gameOverManager.GoToGameOverScene();
        }
        else
        {
            Debug.LogError("SceneBasedGameOverManagerが設定されていません。");
        }

        enabled = false;
    }

    // =======================================================
    // VIII. Helper & Utility Methods (ヘルパーメソッド)
    // =======================================================

    /// <summary>ビーム攻撃に必要なエネルギーを消費します。</summary>
    /// <returns>エネルギーが十分で消費が成功した場合 true。</returns>
    private bool ConsumeEnergyForBeamAttack()
    {
        if (modeController.currentEnergy < modeController.beamAttackEnergyCost)
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return false;
        }

        modeController.ConsumeEnergy(modeController.beamAttackEnergyCost);
        modeController.ResetEnergyRecoveryTimer();
        return true;
    }

    /// <summary>現在のアーマーモードに対応するビーム設定を取得します。</summary>
    private ModeBeamConfiguration GetCurrentBeamConfig(int modeIndex)
    {
        if (modeIndex >= 0 && modeIndex < modeBeamConfigurations.Count)
        {
            return modeBeamConfigurations[modeIndex];
        }
        return null;
    }

    /// <summary>ロックオンターゲットの位置を計算します。</summary>
    private Vector3 GetLockOnTargetPosition(Transform target, bool useOffsetIfNoCollider = false)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }
        else if (useOffsetIfNoCollider)
        {
            return target.position + Vector3.up * lockOnTargetHeightOffset;
        }
        return target.position;
    }

    /// <summary>プレイヤーをターゲット位置へ水平に回転させます。</summary>
    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        // XとZ成分のみを使用することで、水平方向の回転に限定
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        transform.rotation = targetRotation;
    }

    /// <summary>着地硬直中に回転処理を行います（攻撃硬直中は回転を禁止）。</summary>
    private void HandleRotationWhileNotAttackingStunned()
    {
        // 攻撃硬直中でない、つまり着地硬直中のみ実行
        if (_isLandingStunned)
        {
            if (tpsCamController == null || tpsCamController.LockOnTarget == null)
            {
                // ロックオンがない場合はカメラ方向に回転
                tpsCamController?.RotatePlayerToCameraDirection();
            }
            else
            {
                // ロックオンがある場合はターゲットに回転
                RotateTowards(GetLockOnTargetPosition(tpsCamController.LockOnTarget));
            }
        }
    }

    /// <summary>通常時のプレイヤー回転処理を行います。</summary>
    private void HandleNormalRotation()
    {
        if (tpsCamController == null || tpsCamController.LockOnTarget == null)
        {
            tpsCamController?.RotatePlayerToCameraDirection();
        }
        else
        {
            // ロックオン中はターゲットに向かって回転
            RotateTowards(GetLockOnTargetPosition(tpsCamController.LockOnTarget));
        }
    }


    /// <summary>敵のColliderにダメージを適用し、ヒットエフェクトを生成します。</summary>
    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 敵コンポーネントを探してダメージを与えるロジックを簡素化
        if (target.TryGetComponent<SoldierMoveEnemy>(out var soldierMoveEnemy))
        {
            soldierMoveEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<SoliderEnemy>(out var soliderEnemy))
        {
            soliderEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<TutorialEnemyController>(out var tutorialEnemy))
        {
            tutorialEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<ScorpionEnemy>(out var scorpion))
        {
            scorpion.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<SuicideEnemy>(out var suicide))
        {
            suicide.TakeDamage(damageAmount);
            isHit = true;
        }
        else if (target.TryGetComponent<DroneEnemy>(out var drone))
        {
            drone.TakeDamage(damageAmount);
            isHit = true;
        }


        if (isHit && hitEffectPrefab != null)
        {
            // ヒットエフェクト生成
            Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
        }
    }


    // =======================================================
    // IX. Editor Gizmos (エディタ描画)
    // =======================================================

    private void OnDrawGizmosSelected()
    {
        // 近接攻撃範囲の描画 (オレンジ色)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // ビームの射線描画
        int modeIndex = (int)modeController.currentArmorMode;
        ModeBeamConfiguration config = GetCurrentBeamConfig(modeIndex);

        if (config != null)
        {
            foreach (Transform firePoint in config.firePoints)
            {
                if (firePoint == null) continue;

                Vector3 origin = firePoint.position;
                Vector3 fireDirection = firePoint.forward;
                Transform lockOnTarget = tpsCamController?.LockOnTarget;

                // ロックオン時の射線方向の計算
                if (lockOnTarget != null)
                {
                    Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                    fireDirection = (targetPosition - origin).normalized;
                }

                RaycastHit hit;
                Vector3 endPoint;

                // Raycastの結果に基づいた描画
                if (Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0))
                {
                    Gizmos.color = Color.red; // ヒットした場合
                    endPoint = hit.point;
                    Gizmos.DrawSphere(endPoint, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.cyan; // ヒットしなかった場合
                    endPoint = origin + fireDirection * beamMaxDistance;
                }
                Gizmos.DrawLine(origin, endPoint);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(origin + fireDirection * beamMaxDistance, 0.05f);
            }
        }
    }
}