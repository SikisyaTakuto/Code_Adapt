using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// プレイヤーの移動、基本的な攻撃実行、およびHP/ダメージ処理を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementAndCombat : MonoBehaviour
{
    // ★ 追加したデータ構造：モードごとのビーム発射ポイントを格納
    [System.Serializable]
    public class ModeBeamConfiguration
    {
        [Tooltip("このモードでビームを発射するすべてのポイント (Transform)。")]
        public List<Transform> firePoints = new List<Transform>();
    }

    // 依存関係をパブリックプロパティまたはSerializeFieldで受け取る
    [Header("Dependencies")]
    public PlayerModeController modeController; // ★必須：ModeControllerへの参照
    public TPSCameraController tpsCamController;
    public SceneBasedGameOverManager gameOverManager;

    [Header("Base Movement Stats")]
    public float baseMoveSpeed = 15.0f;
    public float dashMultiplier = 2.5f;
    public float verticalSpeed = 10.0f;
    public float gravity = -9.81f;
    [Tooltip("標準重力に対する落下速度の乗数")]
    public float fastFallMultiplier = 3.0f;
    public bool canFly = true;

    [Header("Attack Settings")]
    public float attackFixedDuration = 0.8f;
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public LayerMask enemyLayer;
    public GameObject hitEffectPrefab;

    [Header("Beam Attack Settings")]
    public BeamController beamPrefab;

    // モードごとのビーム設定のリスト
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
    [Tooltip("受けたダメージがこの値を超えないように制限する。(一撃死対策の応急処置)")]
    public float damageCap = 10000.0f;


    [Header("Health Settings")]
    public float maxHP = 10000.0f;
    private float _currentHP;
    public float currentHP { get => _currentHP; private set => _currentHP = value; }

    // === プライベート/キャッシュ変数 ===
    private CharacterController _controller;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private bool _isDead = false;

    // === 入力変数 (PlayerModeControllerと共有) ===
    [HideInInspector] public bool isBoosting = false;
    [HideInInspector] public float verticalInput = 0f;
    private Vector3 _velocity;

    // =======================================================
    // Unity Lifecycle Methods
    // =======================================================

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        currentHP = maxHP;

        if (modeController == null) Debug.LogError("PlayerModeController が設定されていません。");
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

        if (_isAttacking)
        {
            HandleAttackState();

            if (!_controller.isGrounded)
            {
                _velocity.y += gravity * Time.deltaTime;
            }
            _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);

            return;
        }

        if (tpsCamController == null || tpsCamController.LockOnTarget == null)
        {
            tpsCamController?.RotatePlayerToCameraDirection();
        }

        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
        _controller.Move(finalMove * Time.deltaTime);
    }

    // =======================================================
    // Movement Logic
    // =======================================================

    private Vector3 HandleHorizontalMovement()
    {
        float h = Input.GetAxis("Horizontal"); // 左スティックX
        float v = Input.GetAxis("Vertical");   // 左スティックY

        if (h == 0f && v == 0f) return Vector3.zero;

        Vector3 inputDirection = new Vector3(h, 0, v);
        Vector3 moveDirection;

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

        // ModeControllerから現在の速度補正を取得
        float currentSpeed = baseMoveSpeed * modeController.currentArmorStats.moveSpeedMultiplier;
        bool isConsumingEnergy = false;

        bool isDashing = (Input.GetKey(KeyCode.LeftShift) || isBoosting) && modeController.currentEnergy > 0.01f;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            modeController.ConsumeEnergy(modeController.energyConsumptionRate * Time.deltaTime);
            isConsumingEnergy = true;
        }

        if (isConsumingEnergy) modeController.ResetEnergyRecoveryTimer();

        return moveDirection * currentSpeed;
    }

    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool hasVerticalInput = false;

        bool isFlyingUpKey = Input.GetKey(KeyCode.Space);
        bool isFlyingDownKey = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        bool isFlyingUpController = verticalInput > 0.5f;
        bool isFlyingDownController = verticalInput < -0.5f;


        if (canFly && modeController.currentEnergy > 0.01f)
        {
            if (isFlyingUpKey || isFlyingUpController)
            {
                _velocity.y = verticalSpeed;
                hasVerticalInput = true;
            }
            else if (isFlyingDownKey || isFlyingDownController)
            {
                _velocity.y = -verticalSpeed;
                hasVerticalInput = true;
            }
        }

        if (!hasVerticalInput)
        {
            if (!isGrounded)
            {
                float fallSpeedMultiplier = (_velocity.y < 0) ? fastFallMultiplier : 1.0f;
                _velocity.y += gravity * Time.deltaTime * fallSpeedMultiplier;
            }
        }
        else
        {
            modeController.ConsumeEnergy(modeController.energyConsumptionRate * Time.deltaTime);
            modeController.ResetEnergyRecoveryTimer();
        }

        if (modeController.currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
        }

        return new Vector3(0, _velocity.y, 0);
    }
    // =======================================================
    // Combat Logic
    // =======================================================

    public void Attack()
    {
        if (_isAttacking) return;

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

    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;

        Transform lockOnTarget = tpsCamController != null ? tpsCamController.LockOnTarget : null;

        if (lockOnTarget != null)
        {
            Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget);
            RotateTowards(targetPosition);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;
            ApplyDamageToEnemy(hitCollider, meleeDamage);
        }
    }

    private void HandleBeamAttack()
    {
        int modeIndex = (int)modeController.currentArmorMode;
        ModeBeamConfiguration config = null;

        if (modeIndex >= 0 && modeIndex < modeBeamConfigurations.Count)
        {
            config = modeBeamConfigurations[modeIndex];
        }

        if (config == null || config.firePoints.Count == 0 || beamPrefab == null)
        {
            Debug.LogError($"現在のアーマーモード({modeController.currentArmorMode})に対応するビームの設定または発射点が不足しています。");
            return;
        }

        // エネルギーは全ビームの発射に対して一度だけ消費
        if (modeController.currentEnergy < modeController.beamAttackEnergyCost)
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        modeController.ConsumeEnergy(modeController.beamAttackEnergyCost);
        modeController.ResetEnergyRecoveryTimer();

        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f;

        // 全ての発射ポイントをループしてビームを生成する
        foreach (Transform firePoint in config.firePoints)
        {
            if (firePoint == null) continue;

            Vector3 origin = firePoint.position;
            Vector3 fireDirection;
            Transform lockOnTarget = tpsCamController?.LockOnTarget;

            // ロックオンターゲットの決定と回転
            if (lockOnTarget != null)
            {
                Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                fireDirection = (targetPosition - origin).normalized;
                // プレイヤー本体の回転は一度で十分 (最初のビームの発射時に実行)
                if (firePoint == config.firePoints[0])
                {
                    RotateTowards(targetPosition);
                }
            }
            else
            {
                fireDirection = firePoint.forward;
            }

            RaycastHit hit;
            Vector3 endPoint;

            // Raycastはビームごとに実行
            bool didHit = Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0);

            if (didHit)
            {
                endPoint = hit.point;
                // 複数のビームが同時に発射され、同じ敵にヒットする場合、ダメージが重複することを許容する設計と仮定
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

    private void HandleAttackState()
    {
        if (!_isAttacking) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            if (modeController.currentWeaponMode == PlayerModeController.WeaponMode.Beam && !_controller.isGrounded)
            {
                _velocity.y = 0;
            }
            else if (_controller.isGrounded)
            {
                _velocity.y = -0.1f;
            }
        }
    }

    // =======================================================
    // Damage & Death Logic
    // =======================================================

    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        float finalDamage = damageAmount;

        // ModeControllerから防御補正を取得
        float defenseMulti = modeController.currentArmorStats.defenseMultiplier;

        // ダメージ倍率を適用する
        finalDamage *= defenseMulti;

        // ★ 修正箇所: ダメージキャップを適用し、一撃死を回避する応急処置 ★
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
    // Helper Methods & Enemy Damage
    // =======================================================

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

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        transform.rotation = targetRotation;
    }

    private void ApplyDamageToEnemy(Collider hitCollider, float damageAmount)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // 敵コンポーネントを探してダメージを与えるロジック
        if (target.TryGetComponent<SoldierMoveEnemy>(out var soldierMoveEnemy))
        {
            soldierMoveEnemy.TakeDamage(damageAmount);
            isHit = true;
        }
        if (target.TryGetComponent<SoliderEnemy>(out var soliderEnemy))
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
            Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // Gizmo描画も全ての発射ポイントをループ
        int modeIndex = (int)modeController.currentArmorMode;
        ModeBeamConfiguration config = null;

        if (modeIndex >= 0 && modeIndex < modeBeamConfigurations.Count)
        {
            config = modeBeamConfigurations[modeIndex];
        }

        if (config != null)
        {
            foreach (Transform firePoint in config.firePoints)
            {
                if (firePoint == null) continue;

                Vector3 origin = firePoint.position;
                Vector3 fireDirection = firePoint.forward;
                Transform lockOnTarget = tpsCamController != null ? tpsCamController.LockOnTarget : null;

                if (lockOnTarget != null)
                {
                    Vector3 targetPosition = GetLockOnTargetPosition(lockOnTarget, true);
                    fireDirection = (targetPosition - origin).normalized;
                }

                RaycastHit hit;
                Vector3 endPoint;

                if (Physics.Raycast(origin, fireDirection, out hit, beamMaxDistance, ~0))
                {
                    Gizmos.color = Color.red;
                    endPoint = hit.point;
                    Gizmos.DrawSphere(endPoint, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.cyan;
                    endPoint = origin + fireDirection * beamMaxDistance;
                }
                Gizmos.DrawLine(origin, endPoint);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(origin + fireDirection * beamMaxDistance, 0.05f);
            }
        }
    }
}