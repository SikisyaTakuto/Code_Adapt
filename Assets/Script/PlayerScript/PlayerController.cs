using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // OrderByを使うために追加
using System; // Actionを使うために追加

public class PlayerController : MonoBehaviour
{
    // --- ベースとなる能力値 (変更不可) ---
    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f;
    public float baseBoostMultiplier = 2.0f;
    public float baseVerticalSpeed = 10.0f;
    public float baseEnergyConsumptionRate = 15.0f;
    public float baseEnergyRecoveryRate = 10.0f;
    public float baseMeleeAttackRange = 2.0f;
    public float baseMeleeDamage = 10.0f; // 基本の近接ダメージ
    public float baseBeamDamage = 50.0f; // 基本のビームダメージ
    public float baseBitAttackEnergyCost = 20.0f; // 基本のビット攻撃エネルギー消費
    public float baseBeamAttackEnergyCost = 30.0f; // ★追加：基本のビーム攻撃エネルギー消費

    // --- 現在の能力値 (ArmorControllerによって変更される) ---
    [Header("Current Stats (Modified by Armor)")]
    public float moveSpeed;
    public float boostMultiplier;
    public float verticalSpeed;
    public float energyConsumptionRate;
    public float energyRecoveryRate;
    public float meleeAttackRange;
    public float meleeDamage;
    public float beamDamage;
    public float bitAttackEnergyCost;
    public float beamAttackEnergyCost; // ★追加：現在のビーム攻撃エネルギー消費

    // 飛行機能の有効/無効
    public bool canFly = true;
    // ソードビット攻撃の有効/無効
    public bool canUseSwordBitAttack = false;


    // 重力の強さ
    public float gravity = -9.81f;
    // 地面判定のレイヤーマスク
    public LayerMask groundLayer;

    // --- エネルギーゲージ関連の変数 ---
    public float maxEnergy = 100.0f;
    public float currentEnergy;
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime;

    // UIのSliderへの参照 (任意: エネルギーゲージの表示用)
    public Slider energySlider;

    private CharacterController controller;
    private Vector3 velocity; // Y軸方向の速度を管理するための変数

    // TPSカメラコントローラーへの参照
    private TPSCameraController tpsCamController;

    // --- ビット攻撃関連の変数 ---
    [Header("Bit Attack Settings")]
    public GameObject bitPrefab; // 射出するビットのPrefab
    public float bitLaunchHeight = 5.0f; // ビットがプレイヤーの後ろから上昇する高さ
    public float bitLaunchDuration = 0.5f; // ビットが上昇するまでの時間
    public float bitAttackSpeed = 20.0f; // ビットが敵に向かって飛ぶ速度
    public float lockOnRange = 30.0f; // 敵をロックオンできる最大距離
    public LayerMask enemyLayer; // 敵のレイヤー
    public int maxLockedEnemies = 6; // ロックできる敵の最大数
    public float bitDamage = 25.0f; // ビット攻撃のダメージ // 追加: ビット攻撃のダメージ

    private List<Transform> lockedEnemies = new List<Transform>(); // ロックされた敵のリスト
    private bool isAttacking = false; // 攻撃中フラグ (プレイヤーの動きを固定するため)
    private float attackTimer = 0.0f; // 攻撃アニメーションや状態の継続時間タイマー (必要に応じて)
    public float attackFixedDuration = 0.8f; // 攻撃中にプレイヤーが固定される時間

    // --- ビットのスポーン位置を複数設定 ---
    public List<Transform> bitSpawnPoints = new List<Transform>();
    public float bitArcHeight = 2.0f; // 上昇軌道のアーチの高さ
    // --- 近接攻撃関連の変数 ---
    [Header("Melee Attack Settings")]
    public float meleeAttackRadius = 1.0f; // 近接攻撃の有効半径 (SphereCast用)
    public float meleeAttackCooldown = 0.5f; // 近接攻撃のクールダウン時間
    private float lastMeleeAttackTime = -Mathf.Infinity; // 最後に近接攻撃をした時間
    private int currentMeleeCombo = 0; // 現在の近接攻撃コンボ段階
    public int maxMeleeCombo = 5; // 近接攻撃の最大コンボ段階
    public float comboResetTime = 1.0f; // コンボがリセットされるまでの時間
    private float lastMeleeInputTime; // 最後に近接攻撃入力があった時間
    public float autoLockOnMeleeRange = 5.0f; // 近接攻撃の自動ロックオン範囲
    public bool preferLockedMeleeTarget = true; // 近接攻撃時にロックオン可能な敵を優先するか
    private Transform currentLockedMeleeTarget; // 現在ロックオンしている近接攻撃ターゲット

    // ★追加: 近接攻撃時の突進速度と突進距離
    public float meleeDashSpeed = 20.0f; // 近接攻撃時の突進速度
    public float meleeDashDistance = 2.0f; // 近接攻撃時の突進距離 (meleeAttackRangeと同じか少し長めに設定すると良い)
    public float meleeDashDuration = 0.1f; // 近接攻撃時の突進にかかる時間


    // --- ビーム攻撃関連の変数 ---
    [Header("Beam Attack Settings")]
    public float beamAttackRange = 50.0f; // ビームの最大射程距離
    public float beamCooldown = 0.5f; // ビーム攻撃のクールダウン時間
    private float lastBeamAttackTime = -Mathf.Infinity; // 最後にビーム攻撃をした時間
    public GameObject beamEffectPrefab; // ビームのエフェクトPrefab (任意)
    public Transform beamSpawnPoint; // ビームの開始位置 (例: プレイヤーの目の前など)

    //自動ロックオンビーム関連の変数
    [Header("Auto Lock-on Beam Settings")]
    public float autoLockOnRange = 40.0f; // 自動ロックオンの最大距離
    public bool preferLockedTarget = true; // ロックオン可能な敵がいる場合、そちらを優先するか
    private Transform currentLockedBeamTarget; // 現在ロックオンしているビームターゲット


    // --- 装備中の武器Prefab ---
    private GameObject currentPrimaryWeaponInstance;
    private GameObject currentSecondaryWeaponInstance;
    public Transform primaryWeaponAttachPoint; // 主武器を取り付けるTransform
    public Transform secondaryWeaponAttachPoint; // 副武器を取り付けるTransform

    // ★追加: チュートリアル用
    public bool canReceiveInput = true; // プレイヤーが入力できるかどうかのフラグ
    public Action onWASDMoveCompleted; // WASD移動完了時に発火するイベント
    public Action onJumpCompleted; // ジャンプ完了時に発火するイベント
    public Action onDescendCompleted; // 降下完了時に発火するイベント
    public Action onMeleeAttackPerformed; // 近接攻撃実行時に発火するイベント
    public Action onBeamAttackPerformed; // ビーム攻撃実行時に発火するイベント
    public Action onBitAttackPerformed; // 特殊攻撃実行時に発火するイベント
    public Action<int> onArmorModeChanged; // アーマーモード変更時に発火するイベント (引数はモード番号)

    private float _wasdMoveTimer = 0f;
    private float _jumpTimer = 0f;
    private float _descendTimer = 0f;
    private bool _hasMovedWASD = false; // WASDが一度でも入力されたか
    private bool _hasJumped = false; // スペースキーが一度でも押されたか
    private bool _hasDescended = false; // Altキーが一度でも押されたか


    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterControllerが見つかりません。このスクリプトはCharacterControllerが必要です。");
            enabled = false;
        }

        tpsCamController = FindObjectOfType<TPSCameraController>();
        if (tpsCamController == null)
        {
            Debug.LogError("PlayerController: TPSCameraControllerが見つかりません。カメラコントローラがシーンに存在するか、正しくアタッチされているか確認してください。");
        }

        currentEnergy = maxEnergy;
        UpdateEnergyUI();

        if (bitSpawnPoints.Count == 0)
        {
            Debug.LogWarning("PlayerController: bitSpawnPointsが設定されていません。Hierarchyに空のゲームオブジェクトを作成し、このリストにドラッグ＆ドロップしてください。");
        }
        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("PlayerController: Beam Spawn Pointが設定されていません。Hierarchyに空のゲームオブジェクトを作成し、このフィールドにドラッグ＆ドロップしてください。");
        }

        // 初期能力値を現在の能力値に設定
        moveSpeed = baseMoveSpeed;
        boostMultiplier = baseBoostMultiplier;
        verticalSpeed = baseVerticalSpeed;
        energyConsumptionRate = baseEnergyConsumptionRate;
        energyRecoveryRate = baseEnergyRecoveryRate;
        meleeAttackRange = baseMeleeAttackRange;
        meleeDamage = baseMeleeDamage;
        beamDamage = baseBeamDamage;
        bitAttackEnergyCost = baseBitAttackEnergyCost;
        beamAttackEnergyCost = baseBeamAttackEnergyCost; // ★追加：初期値を設定
        bitDamage = baseBitAttackEnergyCost; // ビットダメージも初期化

        // PlayerArmorControllerから初期化されるため、ここではデフォルトの武器は装備しない
    }

    void Update()
    {
        if (!canReceiveInput) // ★追加: 入力受付が無効なら処理をスキップ
        {
            // 攻撃中固定時間のタイマーは進める
            if (isAttacking)
            {
                HandleAttackState();
            }
            return;
        }

        // 攻撃中はプレイヤーの動きを固定
        if (isAttacking)
        {
            HandleAttackState(); // 攻撃中のプレイヤーの状態を処理
            return; // 攻撃中は他の移動処理をスキップ
        }

        // カメラの水平方向に合わせてプレイヤーの向きを調整 (攻撃中以外)
        if (tpsCamController != null)
        {
            tpsCamController.RotatePlayerToCameraDirection();
        }

        // --- 攻撃入力処理 ---
        // 左クリックで近接攻撃
        if (Input.GetMouseButtonDown(0)) // 0は左クリック
        {
            PerformMeleeAttack();
            onMeleeAttackPerformed?.Invoke(); // ★追加: イベント発火
        }
        // ホイール押込みでビット攻撃 (バランスアーマーのみ)
        else if (Input.GetMouseButtonDown(2) && canUseSwordBitAttack) // 2はホイール押込み
        {
            PerformBitAttack();
            onBitAttackPerformed?.Invoke(); // ★追加: イベント発火
        }
        // 右クリックでビーム攻撃
        else if (Input.GetMouseButtonDown(1)) // 1は右クリック
        {
            PerformBeamAttack(); // ここで自動ロックオンのロジックを呼び出す
            onBeamAttackPerformed?.Invoke(); // ★追加: イベント発火
        }
        // ★追加: アーマーモード切り替え
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            onArmorModeChanged?.Invoke(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            onArmorModeChanged?.Invoke(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            onArmorModeChanged?.Invoke(3);
        }


        // コンボタイマーのリセット
        if (Time.time - lastMeleeInputTime > comboResetTime)
        {
            currentMeleeCombo = 0;
        }

        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = Vector3.zero;
        if (tpsCamController != null)
        {
            Quaternion cameraHorizontalRotation = Quaternion.Euler(0, tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraHorizontalRotation * (Vector3.right * horizontalInput + Vector3.forward * verticalInput);
        }
        else
        {
            moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        }
        moveDirection.Normalize();

        bool isConsumingEnergy = false;

        float currentSpeed = moveSpeed;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentEnergy > 0)
        {
            currentSpeed *= boostMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        moveDirection *= currentSpeed;

        // ★追加: WASD入力の監視
        if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            _wasdMoveTimer += Time.deltaTime;
            _hasMovedWASD = true;
        }
        else
        {
            _wasdMoveTimer = 0f; // 入力が途切れたらリセット
        }

        // 飛行機能が有効な場合のみスペース/Altでの上昇下降を許可
        if (canFly)
        {
            if (Input.GetKey(KeyCode.Space) && currentEnergy > 0)
            {
                velocity.y = verticalSpeed;
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
                isConsumingEnergy = true;
                _jumpTimer += Time.deltaTime; // ★追加: ジャンプタイマー更新
                _hasJumped = true;
            }
            else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && currentEnergy > 0)
            {
                velocity.y = -verticalSpeed;
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
                isConsumingEnergy = true;
                _descendTimer += Time.deltaTime; // ★追加: 降下タイマー更新
                _hasDescended = true;
            }
            else if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
                // ★追加: スペース/Altが離されたらタイマーをリセット
                _jumpTimer = 0f;
                _descendTimer = 0f;
            }
        }
        else // 飛行機能が無効な場合は重力の影響を常に受ける
        {
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = -2f; // 地面に着地したらY速度をリセット
            }
        }

        if (currentEnergy <= 0)
        {
            currentEnergy = 0;
            if (moveDirection.magnitude > moveSpeed)
            {
                moveDirection = moveDirection.normalized * moveSpeed;
            }
            if (velocity.y > 0) velocity.y = 0;
        }

        if (isConsumingEnergy)
        {
            lastEnergyConsumptionTime = Time.time;
        }
        else if (Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            currentEnergy += energyRecoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        UpdateEnergyUI();

        Vector3 finalMove = moveDirection + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }

    // ★追加: チュートリアルマネージャーがタイマーをチェックするためのプロパティ
    public float WASDMoveTimer => _wasdMoveTimer;
    public float JumpTimer => _jumpTimer;
    public float DescendTimer => _descendTimer;
    public bool HasMovedWASD => _hasMovedWASD;
    public bool HasJumped => _hasJumped;
    public bool HasDescended => _hasDescended;

    public void ResetInputTracking()
    {
        _wasdMoveTimer = 0f;
        _jumpTimer = 0f;
        _descendTimer = 0f;
        _hasMovedWASD = false;
        _hasJumped = false;
        _hasDescended = false;
    }


    /// <summary>
    /// UIのエネルギーゲージ（Slider）を更新するメソッド
    /// </summary>
    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    /// <summary>
    /// 周囲の敵をロックオンする (ビーム用)
    /// </summary>
    /// <returns>ロックオンした敵のTransform。見つからなければnull。</returns>
    Transform FindBeamTarget()
    {
        // プレイヤーのTransformのpositionを基準にSphereCast
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnRange, enemyLayer);

        if (hitColliders.Length == 0)
        {
            return null; // 敵がいない
        }

        // 最も近い敵を見つける
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform) // プレイヤー自身を除外
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                // カメラの視界に入っているか、または非常に近い敵を優先するなどのロジックを追加可能
                // 現状は一番近い敵をターゲットにする
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }

    /// <summary>
    /// 周囲の敵をロックオンする (近接攻撃用)
    /// </summary>
    /// <returns>ロックオンした敵のTransform。見つからなければnull。</returns>
    Transform FindMeleeTarget()
    {
        // プレイヤーのTransformのpositionを基準にOverlapSphere
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnMeleeRange, enemyLayer);

        if (hitColliders.Length == 0)
        {
            return null; // 敵がいない
        }

        // 最も近い敵を見つける
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform) // プレイヤー自身を除外
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                // 近接攻撃なので、単に一番近い敵で良いことが多いが、
                // 将来的にはプレイヤーの正面方向の敵を優先するなど、より複雑なロジックも検討可能
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }


    /// <summary>
    /// 周囲の敵をロックオンする (ビット攻撃用)
    /// </summary>
    void LockOnEnemies()
    {
        lockedEnemies.Clear(); // ロックオンリストをクリア

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);

        // 距離が近い順にソートして、maxLockedEnemiesの数までロックオン
        var sortedEnemies = hitColliders.OrderBy(col => Vector3.Distance(transform.position, col.transform.position))
                                        .Take(maxLockedEnemies);

        foreach (Collider col in sortedEnemies)
        {
            if (col.transform != transform) // プレイヤー自身をロックオンしないように
            {
                lockedEnemies.Add(col.transform);
                Debug.Log($"Locked on: {col.name}");
            }
        }

        if (lockedEnemies.Count > 0)
        {
            // プレイヤーの向きを一番近いロックオン敵の方向に強制的に向ける
            Vector3 lookAtTarget = lockedEnemies[0].position;
            lookAtTarget.y = transform.position.y; // Y軸は固定
            transform.LookAt(lookAtTarget);
        }
    }

    /// <summary>
    /// ビット攻撃を実行する
    /// </summary>
    void PerformBitAttack()
    {
        // bitSpawnPointsが設定されていない場合は攻撃を中止
        if (bitSpawnPoints.Count == 0)
        {
            Debug.LogWarning("Bit spawn points are not set up in the Inspector. Cannot perform bit attack.");
            return;
        }

        // ロックできる敵の数（maxLockedEnemies）分のエネルギーが必要になるように調整
        if (currentEnergy < bitAttackEnergyCost * maxLockedEnemies) // 変数名変更
        {
            Debug.Log($"Not enough energy for Bit Attack! Need {bitAttackEnergyCost * maxLockedEnemies} energy.");
            return;
        }

        LockOnEnemies(); // 攻撃前に敵をロックオン

        if (lockedEnemies.Count == 0)
        {
            Debug.Log("No enemies to lock on. Bit attack cancelled.");
            return;
        }

        currentEnergy -= bitAttackEnergyCost * lockedEnemies.Count; // ロックした敵の数に応じてエネルギー消費 (変数名変更)
        UpdateEnergyUI();

        isAttacking = true; // 攻撃中フラグを立てる
        attackTimer = 0.0f; // タイマーをリセット

        // ロックした敵の数、またはbitSpawnPointsの数までビットを射出（少ない方に合わせる）
        int bitsToSpawn = Mathf.Min(lockedEnemies.Count, bitSpawnPoints.Count);

        for (int i = 0; i < bitsToSpawn; i++)
        {
            // 各ビットのスポーン位置をbitSpawnPointsから取得
            Transform spawnPoint = bitSpawnPoints[i];

            // i番目のビットをi番目のロックオン敵に紐付ける (敵が少ない場合はループの剰余を使用など)
            Transform targetEnemy = lockedEnemies[i % lockedEnemies.Count];

            // LaunchBitにbitDamageを渡す
            StartCoroutine(LaunchBit(spawnPoint.position, targetEnemy, bitDamage)); // Transformのpositionとダメージ値を渡す
        }
    }

    /// <summary>
    /// 近接攻撃を実行する (5段階コンボ)
    /// </summary>
    void PerformMeleeAttack()
    {
        // クールダウン中または既に攻撃中の場合は実行しない
        if (Time.time < lastMeleeAttackTime + meleeAttackCooldown || isAttacking)
        {
            return;
        }

        // コンボ段階を進める
        currentMeleeCombo = (currentMeleeCombo % maxMeleeCombo) + 1;
        Debug.Log($"近接攻撃！コンボ段階: {currentMeleeCombo}");

        lastMeleeAttackTime = Time.time;
        lastMeleeInputTime = Time.time; // コンボリセットタイマーを更新

        isAttacking = true; // 攻撃中フラグを立てる
        attackTimer = 0.0f; // タイマーをリセット
        attackFixedDuration = 0.3f; // 近接攻撃の固定時間を短めに設定 (アニメーションに合わせて調整)

        currentLockedMeleeTarget = null; // ロックオンターゲットをリセット
        if (preferLockedMeleeTarget)
        {
            currentLockedMeleeTarget = FindMeleeTarget();
        }

        // ★修正点1: ロックオンターゲットがいる場合、そちらの方を向く処理を優先
        if (currentLockedMeleeTarget != null)
        {
            Vector3 lookAtTarget = currentLockedMeleeTarget.position;
            lookAtTarget.y = transform.position.y; // Y軸は固定
            transform.LookAt(lookAtTarget);
            Debug.Log($"近接攻撃: ロックオンターゲット ({currentLockedMeleeTarget.name}) へ向かって攻撃！");

            // ★追加: 敵に向かって突進するコルーチンを開始
            StartCoroutine(MeleeDashToTarget(currentLockedMeleeTarget.position));
        }
        else
        {
            // ロックオンターゲットがいない場合、カメラの向きを維持
            if (tpsCamController != null)
            {
                tpsCamController.RotatePlayerToCameraDirection();
            }
            // ★追加: ターゲットがいない場合、現在向いている方向へ短く突進
            StartCoroutine(MeleeDashInCurrentDirection());
        }

        // --- ここからが近接攻撃のダメージ処理 ---
        // 近接攻撃の範囲内の敵を検出
        Vector3 attackOrigin = transform.position + transform.forward * meleeAttackRange * 0.5f; // プレイヤーの前方少し離れた位置から
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, meleeAttackRadius, enemyLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // もしEnemyHealthが直接コライダーにアタッチされていない場合、親オブジェクトを探す
                enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                // 敵のHealthスクリプトにダメージを与える
                float damage = meleeDamage + (currentMeleeCombo - 1) * (meleeDamage * 0.5f); // 例: ベースダメージにコンボボーナスを加算
                enemyHealth.TakeDamage(damage);
                Debug.Log($"{hitCollider.name} に {damage} ダメージを与えました。(コンボ {currentMeleeCombo})");
            }
        }
        // --- 近接攻撃のダメージ処理ここまで ---
    }

    /// <summary>
    /// ビーム攻撃を実行する
    /// ロックオン可能な敵がいればそちらを優先し、なければカメラの方向へ発射
    /// </summary>
    void PerformBeamAttack()
    {
        // クールダウン中、またはエネルギー不足、または既に攻撃中の場合は実行しない
        if (Time.time < lastBeamAttackTime + beamCooldown || currentEnergy < beamAttackEnergyCost || isAttacking)
        {
            if (currentEnergy < beamAttackEnergyCost)
            {
                Debug.Log("Not enough energy for Beam Attack!");
            }
            return;
        }

        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("Beam Spawn Point is not assigned. Cannot perform beam attack without a valid origin for effect.");
            return;
        }

        if (tpsCamController == null)
        {
            Debug.LogWarning("TPSCameraController is not assigned. Cannot perform camera-aligned beam attack.");
            return;
        }

        currentEnergy -= beamAttackEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;

        Debug.Log("ビーム攻撃！");

        // ロックオンターゲットを検索
        currentLockedBeamTarget = null; // ロックオンターゲットをリセット
        if (preferLockedTarget)
        {
            currentLockedBeamTarget = FindBeamTarget();
        }

        Vector3 rayOrigin;
        Vector3 rayDirection;

        if (currentLockedBeamTarget != null)
        {
            // ロックオンターゲットがいる場合、ターゲットの方向へビームを飛ばす
            // プレイヤーの向きをターゲットの水平方向に向ける
            Vector3 targetFlatPos = currentLockedBeamTarget.position;
            targetFlatPos.y = transform.position.y;
            transform.LookAt(targetFlatPos);

            // beamSpawnPoint からターゲット方向へのRayを設定
            rayOrigin = beamSpawnPoint.position;
            rayDirection = (currentLockedBeamTarget.position - beamSpawnPoint.position).normalized;

            Debug.Log($"ビーム: ロックオンターゲット ({currentLockedBeamTarget.name}) へ発射！");
        }
        else
        {
            // ロックオンターゲットがいない場合、カメラの方向へビームを飛ばす（既存の動作）
            // プレイヤーの向きをカメラの水平方向に合わせる
            tpsCamController.RotatePlayerToCameraDirection();

            // カメラからRayを取得し、そのRayの方向でRaycastを行う
            Ray cameraRay = tpsCamController.GetCameraRay();
            rayOrigin = cameraRay.origin;
            rayDirection = cameraRay.direction;

            Debug.Log("ビーム: カメラの方向へ発射！");
        }

        GameObject beamInstance = null; // ビームエフェクトのインスタンスを保持する変数
        RaycastHit hit;

        // --- ここからがビーム攻撃のダメージ処理 ---
        // Raycastで敵を検出
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, beamAttackRange, enemyLayer))
        {
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // もしEnemyHealthが直接コライダーにアタッチされていない場合、親オブジェクトを探す
                enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                // 敵のHealthスクリプトにダメージを与える
                enemyHealth.TakeDamage(beamDamage);
                Debug.Log($"{hit.collider.name} にビームで {beamDamage} ダメージを与えました。");
            }

            // ビームエフェクトをbeamSpawnPointから発射し、Rayの進行方向を向かせる
            if (beamEffectPrefab != null)
            {
                beamInstance = Instantiate(beamEffectPrefab, beamSpawnPoint.position, Quaternion.LookRotation(rayDirection));
            }
        }
        else
        {
            // 何もヒットしなかった場合、ビームエフェクトをbeamSpawnPointから発射し、Rayの進行方向を向かせる
            if (beamEffectPrefab != null)
            {
                beamInstance = Instantiate(beamEffectPrefab, beamSpawnPoint.position, Quaternion.LookRotation(rayDirection));
            }
        }
        // --- ビーム攻撃のダメージ処理ここまで ---

        // 生成したビームエフェクトを一定時間後に破棄する
        if (beamInstance != null)
        {
            Destroy(beamInstance, 0.5f); // 例: 0.5秒後に消滅
        }

        // ビーム攻撃中は一時的にプレイヤーの動きを固定
        isAttacking = true;
        attackTimer = 0.0f;
        attackFixedDuration = 0.2f; // 例: ビーム発射のアニメーション時間
    }

    /// <summary>
    /// 攻撃中のプレイヤーの状態を処理（動き固定、向きの維持など）
    /// </summary>
    void HandleAttackState()
    {
        // 攻撃アニメーションやエフェクトの再生中にプレイヤーの動きを固定
        // ビット攻撃中はロックオンした敵に向ける
        if (canUseSwordBitAttack && Input.GetMouseButtonDown(2) && lockedEnemies.Count > 0 && lockedEnemies[0] != null)
        {
            Vector3 lookAtTarget = lockedEnemies[0].position;
            lookAtTarget.y = transform.position.y;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        // 近接攻撃中は、ロックオンしている敵がいればそちらを向き、いなければカメラの向きを維持
        else if (Input.GetMouseButtonDown(0)) // 近接攻撃中の場合
        {
            if (currentLockedMeleeTarget != null)
            {
                // ロックオンターゲットが存在する場合、Y軸固定でターゲットの方を向く
                Vector3 lookAtTarget = currentLockedMeleeTarget.position;
                lookAtTarget.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            // else: ロックオンしていなければ、Updateで常にカメラ方向を向いているので特別処理は不要
        }
        // ビーム攻撃中は、ロックオンしている敵がいればそちらを向き、いなければカメラの向きを維持
        else if (Input.GetMouseButtonDown(1)) // ビーム攻撃中の場合
        {
            if (currentLockedBeamTarget != null)
            {
                // ロックオンターゲットが存在する場合、Y軸固定でターゲットの方を向く
                Vector3 lookAtTarget = currentLockedBeamTarget.position;
                lookAtTarget.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            else if (tpsCamController != null)
            {
                tpsCamController.RotatePlayerToCameraDirection(); // ロックオンしていなければカメラ方向に強制
            }
        }


        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            // 各攻撃のロックオンターゲットをクリア
            lockedEnemies.RemoveAll(t => t == null); // ビット攻撃用
            currentLockedBeamTarget = null; // ビーム攻撃用
            currentLockedMeleeTarget = null; // 近接攻撃用

            Debug.Log("Attack sequence finished.");
            attackFixedDuration = 0.8f; // ここでデフォルトに戻す例
        }
    }


    /// <summary>
    /// ビットを射出し、敵に向かって飛ばすコルーチン
    /// </summary>
    /// <param name="initialSpawnPosition">ビットの初期スポーン位置（ワールド座標）</param>
    /// <param name="target">ビットが向かうターゲット</param>
    /// <param name="damage">ビットが与えるダメージ</param> // 追加: ダメージ引数
    System.Collections.IEnumerator LaunchBit(Vector3 initialSpawnPosition, Transform target, float damage)
    {
        if (bitPrefab != null)
        {
            GameObject bitInstance = Instantiate(bitPrefab, initialSpawnPosition, Quaternion.identity);
            Bit bitScript = bitInstance.GetComponent<Bit>();

            if (bitScript != null)
            {
                // InitializeBitにダメージ値を渡す
                bitScript.InitializeBit(initialSpawnPosition, target, bitLaunchHeight, bitLaunchDuration, bitAttackSpeed, bitArcHeight, enemyLayer, damage);
            }
            else
            {
                Debug.LogWarning("Bit Prefab does not have a 'Bit' script attached!");
            }
        }
        else
        {
            Debug.LogError("Bit Prefab is not assigned!");
        }
        yield return null; // コルーチンとして機能させるために最低1フレーム待つ
    }

    /// <summary>
    /// 武器を装備する
    /// </summary>
    public void EquipWeapons(WeaponData primaryWeaponData, WeaponData secondaryWeaponData)
    {
        // 既存の武器を破棄
        if (currentPrimaryWeaponInstance != null) Destroy(currentPrimaryWeaponInstance);
        if (currentSecondaryWeaponInstance != null) Destroy(currentSecondaryWeaponInstance);

        // 主武器の装備
        if (primaryWeaponData != null && primaryWeaponData.weaponPrefab != null && primaryWeaponAttachPoint != null)
        {
            currentPrimaryWeaponInstance = Instantiate(primaryWeaponData.weaponPrefab, primaryWeaponAttachPoint);
            currentPrimaryWeaponInstance.transform.localPosition = Vector3.zero;
            currentPrimaryWeaponInstance.transform.localRotation = Quaternion.identity;
            Debug.Log($"Primary Weapon Equipped: {primaryWeaponData.weaponName}");
        }

        // 副武器の装備
        if (secondaryWeaponData != null && secondaryWeaponData.weaponPrefab != null && secondaryWeaponAttachPoint != null)
        {
            currentSecondaryWeaponInstance = Instantiate(secondaryWeaponData.weaponPrefab, secondaryWeaponAttachPoint);
            currentSecondaryWeaponInstance.transform.localPosition = Vector3.zero;
            currentSecondaryWeaponInstance.transform.localRotation = Quaternion.identity;
            Debug.Log($"Secondary Weapon Equipped: {secondaryWeaponData.weaponName}");
        }
    }


    // デバッグ表示用 (Gizmos)
    void OnDrawGizmosSelected()
    {
        // 近接攻撃の範囲を視覚化
        Gizmos.color = Color.red;
        // 近接攻撃の自動ロックオン範囲
        Gizmos.DrawWireSphere(transform.position, autoLockOnMeleeRange);
        // 通常の近接攻撃判定範囲
        Gizmos.DrawWireSphere(transform.position + transform.forward * meleeAttackRange * 0.5f, meleeAttackRadius);


        // ビーム攻撃の射程を、ロックオンターゲットが存在すればそちらへ、なければカメラのRayに基づいて視覚化
        Gizmos.color = Color.blue;
        if (tpsCamController != null)
        {
            Vector3 gizmoRayOrigin;
            Vector3 gizmoRayDirection;

            if (currentLockedBeamTarget != null) // ロックオンターゲットが存在する場合
            {
                gizmoRayOrigin = beamSpawnPoint != null ? beamSpawnPoint.position : transform.position;
                gizmoRayDirection = (currentLockedBeamTarget.position - gizmoRayOrigin).normalized;
            }
            else // ロックオンターゲットが存在しない場合（カメラのRayを使用）
            {
                Ray cameraRay = tpsCamController.GetCameraRay();
                gizmoRayOrigin = cameraRay.origin;
                gizmoRayDirection = cameraRay.direction;
            }

            Gizmos.DrawRay(gizmoRayOrigin, gizmoRayDirection * beamAttackRange);
            Gizmos.DrawSphere(gizmoRayOrigin + gizmoRayDirection * beamAttackRange, 0.5f); // 終点に球

            // 自動ロックオン範囲の表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, autoLockOnRange);
        }
        else if (beamSpawnPoint != null) // TPSCameraControllerがない場合のフォールバック
        {
            Gizmos.DrawRay(beamSpawnPoint.position, beamSpawnPoint.forward * beamAttackRange);
            Gizmos.DrawSphere(beamSpawnPoint.position + beamSpawnPoint.forward * beamAttackRange, 0.5f);
        }
        else // beamSpawnPointも設定されていない場合のフォールバック
        {
            Gizmos.DrawRay(transform.position, transform.forward * beamAttackRange);
            Gizmos.DrawSphere(transform.position + transform.forward * beamAttackRange, 0.5f);
        }
    }

    /// <summary>
    /// 近接攻撃時にターゲットに向かって突進するコルーチン
    /// </summary>
    /// <param name="targetPosition">突進目標地点</param>
    private System.Collections.IEnumerator MeleeDashToTarget(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        // ターゲットまでの距離を計算し、meleeDashDistanceを超えないようにする
        Vector3 direction = (targetPosition - startPosition).normalized;
        Vector3 endPosition = startPosition + direction * Mathf.Min(Vector3.Distance(startPosition, targetPosition) - meleeAttackRange * 0.5f, meleeDashDistance);
        // meleeAttackRange * 0.5f は、敵の「中心」に突進するのではなく、攻撃範囲の届く手前で止まるように調整

        float elapsedTime = 0f;

        while (elapsedTime < meleeDashDuration)
        {
            // CharacterController.Move を使って移動
            // CharacterController はコリジョンに自動的に反応するため、単に移動ベクトルを与える
            Vector3 currentMove = Vector3.Lerp(startPosition, endPosition, elapsedTime / meleeDashDuration) - transform.position;
            controller.Move(currentMove);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 突進終了時に最終的な位置に確実に到達させる（CharacterController.Move の特性上、完全に一致しない場合があるため）
        controller.Move(endPosition - transform.position);
    }

    /// <summary>
    /// 近接攻撃時に現在向いている方向へ短く突進するコルーチン（ターゲットがいない場合）
    /// </summary>
    private System.Collections.IEnumerator MeleeDashInCurrentDirection()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + transform.forward * meleeDashDistance;

        float elapsedTime = 0f;

        while (elapsedTime < meleeDashDuration)
        {
            Vector3 currentMove = Vector3.Lerp(startPosition, endPosition, elapsedTime / meleeDashDuration) - transform.position;
            controller.Move(currentMove);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        controller.Move(endPosition - transform.position);
    }
}