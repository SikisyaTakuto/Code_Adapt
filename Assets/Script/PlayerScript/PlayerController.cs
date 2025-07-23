using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections; // Action を使うために必要

public class PlayerController : MonoBehaviour
{
    // --- ベースとなる能力値 (変更不可) ---
    [Header("Base Stats")]
    [Tooltip("プレイヤーの基本的な移動速度。")]
    public float baseMoveSpeed = 15.0f;
    [Tooltip("ブースト時の移動速度の乗数。")]
    public float baseBoostMultiplier = 2.0f;
    [Tooltip("上昇・下降時の垂直方向の速度。")]
    public float baseVerticalSpeed = 10.0f;
    [Tooltip("エネルギー消費の基本レート。")]
    public float baseEnergyConsumptionRate = 15.0f;
    [Tooltip("エネルギー回復の基本レート。")]
    public float baseEnergyRecoveryRate = 10.0f;
    [Tooltip("近接攻撃の基本範囲。")]
    public float baseMeleeAttackRange = 2.0f;
    [Tooltip("基本的な近接攻撃ダメージ。")]
    public float baseMeleeDamage = 50.0f;
    [Tooltip("基本的なビーム攻撃ダメージ。")]
    public float baseBeamDamage = 50.0f;
    // [Tooltip("ビット攻撃の基本的なエネルギー消費量。")] // 特殊攻撃の項目をコメントアウト
    // public float baseBitAttackEnergyCost = 20.0f; // 特殊攻撃の項目をコメントアウト
    [Tooltip("ビーム攻撃の基本的なエネルギー消費量。")]
    public float baseBeamAttackEnergyCost = 30.0f;

    // --- 現在の能力値 (ArmorControllerによって変更される) ---
    [Header("Current Stats (Modified by Armor)")]
    [Tooltip("現在の移動速度。ArmorControllerによって変更される。")]
    public float moveSpeed;
    [Tooltip("現在のブースト乗数。ArmorControllerによって変更される。")]
    public float boostMultiplier;
    [Tooltip("現在の垂直速度。ArmorControllerによって変更される。")]
    public float verticalSpeed;
    [Tooltip("現在のエネルギー消費レート。ArmorControllerによって変更される。")]
    public float energyConsumptionRate;
    [Tooltip("現在のエネルギー回復レート。ArmorControllerによって変更される。")]
    public float energyRecoveryRate;
    [Tooltip("現在の近接攻撃範囲。ArmorControllerによって変更される。")]
    public float meleeAttackRange;
    [Tooltip("現在の近接攻撃ダメージ。ArmorControllerによって変更される。")]
    public float meleeDamage;
    [Tooltip("現在のビーム攻撃ダメージ。ArmorControllerによって変更される。")]
    public float beamDamage;
    // [Tooltip("現在のビット攻撃エネルギー消費量。ArmorControllerによって変更される。")] // 特殊攻撃の項目をコメントアウト
    // public float bitAttackEnergyCost; // 特殊攻撃の項目をコメントアウト
    [Tooltip("現在のビーム攻撃エネルギー消費量。ArmorControllerによって変更される。")]
    public float beamAttackEnergyCost;
    // [Tooltip("現在のビット攻撃ダメージ。ArmorControllerによって変更される。")] // 特殊攻撃の項目をコメントアウト
    // public float bitDamage; // 特殊攻撃の項目をコメントアウト

    [Tooltip("飛行機能が有効かどうかのフラグ。")]
    public bool canFly = true;
    // [Tooltip("ソードビット攻撃が使用可能かどうかのフラグ。")] // 特殊攻撃の項目をコメントアウト
    // public bool canUseSwordBitAttack = false; // 特殊攻撃の項目をコメントアウト

    [Tooltip("プレイヤーに適用される重力の強さ。")]
    public float gravity = -9.81f;
    [Tooltip("地面と判定するためのレイヤーマスク。")]
    public LayerMask groundLayer;

    // --- エネルギーゲージ関連の変数 ---
    [Header("Energy Gauge Settings")]
    [Tooltip("最大エネルギー量。")]
    public float maxEnergy = 100.0f;
    [Tooltip("現在のエネルギー量。")]
    public float currentEnergy;
    [Tooltip("エネルギー消費後、回復を開始するまでの遅延時間。")]
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime; // 最後にエネルギーを消費した時間

    [Tooltip("UI上のエネルギーゲージ（Slider）への参照。")]
    public Slider energySlider;
    private bool hasTriggeredEnergyDepletedEvent = false; // エネルギー枯渇イベントが発火済みか

    private CharacterController controller;
    private Vector3 velocity;

    private TPSCameraController tpsCamController;

    // --- ビット攻撃関連の変数 --- // 特殊攻撃の項目をコメントアウト
    // [Header("Bit Attack Settings")] // 特殊攻撃の項目をコメントアウト
    // [Tooltip("射出するビットのPrefab。")] // 特殊攻撃の項目をコメントアウト
    // public GameObject bitPrefab; // 特殊攻撃の項目をコメントアウト
    // [Tooltip("ビットがプレイヤーの後方から上昇する高さ。")] // 特殊攻撃の項目をコメントアウト
    // public float bitLaunchHeight = 5.0f; // 特殊攻撃の項目をコメントアウト
    // [Tooltip("ビットが上昇するまでの時間。")] // 特殊攻撃の項目をコメントアウト
    // public float bitLaunchDuration = 0.5f; // 特殊攻撃の項目をコメントアウト
    // [Tooltip("ビットが敵に向かって飛ぶ速度。")] // 特殊攻撃の項目をコメントアウト
    // public float bitAttackSpeed = 20.0f; // 特殊攻撃の項目をコメントアウト
    // [Tooltip("敵をロックオンできる最大距離。")] // 特殊攻撃の項目をコメントアウト
    // public float lockOnRange = 30.0f; // 特殊攻撃の項目をコメントアウト
    [Tooltip("敵のレイヤーマスク。")] // 特殊攻撃の項目をコメントアウト
    public LayerMask enemyLayer; // 特殊攻撃の項目をコメントアウト
    // [Tooltip("ロックできる敵の最大数。")] // 特殊攻撃の項目をコメントアウト
    // public int maxLockedEnemies = 6; // 特殊攻撃の項目をコメントアウト
    // [Tooltip("ビット攻撃が与えるダメージ。")] // 特殊攻撃の項目をコメントアウト
    // public float bitDamage = 25.0f; // 特殊攻撃の項目をコメントアウト

    private List<Transform> lockedEnemies = new List<Transform>();
    private bool isAttacking = false;
    private float attackTimer = 0.0f;
    [Tooltip("攻撃中にプレイヤーが固定される時間。")]
    public float attackFixedDuration = 0.8f;

    // [Tooltip("ビットのスポーン位置を複数設定するためのリスト。")] // 特殊攻撃の項目をコメントアウト
    // public List<Transform> bitSpawnPoints = new List<Transform>(); // 特殊攻撃の項目をコメントアウト
    // [Tooltip("ビットが上昇する軌道のアーチの高さ。")] // 特殊攻撃の項目をコメントアウト
    // public float bitArcHeight = 2.0f; // 特殊攻撃の項目をコメントアウト

    // --- 近接攻撃関連の変数 ---
    [Header("Melee Attack Settings")]
    [Tooltip("近接攻撃の有効半径（SphereCast用）。")]
    public float meleeAttackRadius = 1.0f;
    [Tooltip("近接攻撃のクールダウン時間。")]
    public float meleeAttackCooldown = 0.5f;
    private float lastMeleeAttackTime = -Mathf.Infinity;
    private int currentMeleeCombo = 0;
    [Tooltip("近接攻撃の最大コンボ段階。")]
    public int maxMeleeCombo = 5;
    [Tooltip("コンボがリセットされるまでの時間。")]
    public float comboResetTime = 1.0f;
    private float lastMeleeInputTime;
    [Tooltip("近接攻撃の自動ロックオン範囲。")]
    public float autoLockOnMeleeRange = 5.0f;
    [Tooltip("近接攻撃時にロックオン可能な敵を優先するかどうか。")]
    public bool preferLockedMeleeTarget = true;
    private Transform currentLockedMeleeTarget;

    [Tooltip("近接攻撃時の突進速度。")]
    public float meleeDashSpeed = 20.0f;
    [Tooltip("近接攻撃時の突進距離。meleeAttackRangeと同じか少し長めに設定すると良い。")]
    public float meleeDashDistance = 2.0f;
    [Tooltip("近接攻撃時の突進にかかる時間。")]
    public float meleeDashDuration = 0.1f;

    // --- ビーム攻撃関連の変数 ---
    [Header("Beam Attack Settings")]
    [Tooltip("ビームの最大射程距離。")]
    public float beamAttackRange = 30.0f;
    [Tooltip("ビーム攻撃のクールダウン時間。")]
    public float beamCooldown = 0.5f;
    private float lastBeamAttackTime = -Mathf.Infinity;
    [Tooltip("ビームのエフェクトPrefab（任意）。")]
    public GameObject beamEffectPrefab;
    [Tooltip("ビームの開始位置（例: プレイヤーの目の前など）。")]
    public Transform beamSpawnPoint;
    [Tooltip("ビームの線の太さ。")] // ★追加
    public float beamWidth = 0.5f; // ★追加
    [Tooltip("ビームの表示時間。")] // ★追加
    public float beamDisplayDuration = 0.5f; // ★追加

    [Header("Auto Lock-on Beam Settings")]
    [Tooltip("ビーム攻撃の自動ロックオン最大距離。")]
    public float autoLockOnRange = 40.0f;
    [Tooltip("ロックオン可能な敵がいる場合、そちらを優先するかどうか。")]
    public bool preferLockedTarget = true;
    private Transform currentLockedBeamTarget;

    // --- 装備中の武器Prefab ---
    private GameObject currentPrimaryWeaponInstance;
    private GameObject currentSecondaryWeaponInstance;
    [Tooltip("主武器を取り付けるTransform。")]
    public Transform primaryWeaponAttachPoint;
    [Tooltip("副武器を取り付けるTransform。")]
    public Transform secondaryWeaponAttachPoint;

    [Tooltip("プレイヤーが入力できるかどうかのフラグ。チュートリアルなどで使用。")]
    public bool canReceiveInput = true;

    // チュートリアル用イベント
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    // public Action onBitAttackPerformed; // 特殊攻撃の項目をコメントアウト
    public Action<int> onArmorModeChanged;
    public event Action onEnergyDepleted; // ★追加: エネルギー枯渇時に発火するイベント

    // チュートリアル用タイマー
    private float _wasdMoveTimer = 0f;
    public float WASDMoveTimer { get { return _wasdMoveTimer; } }

    private float _jumpTimer = 0f;
    public float JumpTimer { get { return _jumpTimer; } }

    private float _descendTimer = 0f;
    public float DescendTimer { get { return _descendTimer; } }

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

        // if (bitSpawnPoints.Count == 0) // 特殊攻撃の項目をコメントアウト
        // { // 特殊攻撃の項目をコメントアウト
        //     Debug.LogWarning("PlayerController: bitSpawnPointsが設定されていません。Hierarchyに空のゲームオブジェクトを作成し、このリストにドラッグ＆ドロップしてください。"); // 特殊攻撃の項目をコメントアウト
        // } // 特殊攻撃の項目をコメントアウト
        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("PlayerController: Beam Spawn Pointが設定されていません。Hierarchyに空のゲームオブジェクトを作成し、このフィールドにドラッグ＆ドロップしてください。");
        }

        moveSpeed = baseMoveSpeed;
        boostMultiplier = baseBoostMultiplier;
        verticalSpeed = baseVerticalSpeed;
        energyConsumptionRate = baseEnergyConsumptionRate;
        energyRecoveryRate = baseEnergyRecoveryRate;
        meleeAttackRange = baseMeleeAttackRange;
        meleeDamage = baseMeleeDamage;
        beamDamage = baseBeamDamage;
        // bitAttackEnergyCost = baseBitAttackEnergyCost; // 特殊攻撃の項目をコメントアウト
        beamAttackEnergyCost = baseBeamAttackEnergyCost;
        // bitDamage = baseBitAttackEnergyCost; // 特殊攻撃の項目をコメントアウト
    }

    void Update()
    {
        // チュートリアル中の入力制御と攻撃中の固定
        if (!canReceiveInput)
        {
            if (isAttacking)
            {
                HandleAttackState();
            }
            // チュートリアル中もタイマーは更新しない
            _wasdMoveTimer = 0f;
            _jumpTimer = 0f;
            _descendTimer = 0f;
            return;
        }

        if (isAttacking)
        {
            HandleAttackState();
            // 攻撃中は移動タイマーなどを更新しない
            _wasdMoveTimer = 0f;
            _jumpTimer = 0f;
            _descendTimer = 0f;
            return;
        }

        if (tpsCamController != null)
        {
            tpsCamController.RotatePlayerToCameraDirection();
        }

        // --- 攻撃入力処理 ---
        if (Input.GetMouseButtonDown(0))
        {
            PerformMeleeAttack();
            onMeleeAttackPerformed?.Invoke();
        }
        // else if (Input.GetMouseButtonDown(2) && canUseSwordBitAttack) // 特殊攻撃の項目をコメントアウト
        // { // 特殊攻撃の項目をコメントアウト
        //     PerformBitAttack(); // 特殊攻撃の項目をコメントアウト
        //     onBitAttackPerformed?.Invoke(); // 特殊攻撃の項目をコメントアウト
        // } // 特殊攻撃の項目をコメントアウト
        else if (Input.GetMouseButtonDown(1))
        {
            PerformBeamAttack();
            onBeamAttackPerformed?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            onArmorModeChanged?.Invoke(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            onArmorModeChanged?.Invoke(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            onArmorModeChanged?.Invoke(2);
        }


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

        // WASD入力の監視（チュートリアル用） - 移動している間だけタイマー増加
        if (moveDirection.magnitude > 0.1f) // 実際にある程度移動している場合
        {
            _wasdMoveTimer += Time.deltaTime;
        }
        else
        {
            _wasdMoveTimer = 0f; // 入力が途切れたらタイマーをリセット
        }

        if (canFly)
        {
            if (Input.GetKey(KeyCode.Space) && currentEnergy > 0)
            {
                velocity.y = verticalSpeed;
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
                isConsumingEnergy = true;
                _jumpTimer += Time.deltaTime; // ジャンプタイマー更新
            }
            else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && currentEnergy > 0)
            {
                velocity.y = -verticalSpeed;
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
                isConsumingEnergy = true;
                _descendTimer += Time.deltaTime; // 降下タイマー更新
            }
            else // スペース/Altが離されたらタイマーをリセット
            {
                _jumpTimer = 0f;
                _descendTimer = 0f;
            }
        }

        // 重力処理
        if (!isGrounded && !Input.GetKey(KeyCode.Space) && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            velocity.y += gravity * Time.deltaTime;
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

        // エネルギー枯渇イベントの発火
        if (currentEnergy <= 0.1f && !hasTriggeredEnergyDepletedEvent) // ほぼ0になったら発火
        {
            onEnergyDepleted?.Invoke();
            hasTriggeredEnergyDepletedEvent = true;
        }
        // エネルギーが回復し始めたらフラグをリセット
        if (currentEnergy > 0.1f && hasTriggeredEnergyDepletedEvent && !isConsumingEnergy)
        {
            hasTriggeredEnergyDepletedEvent = false;
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

    /// <summary>
    /// チュートリアル用の入力追跡フラグとタイマーをリセットする。
    /// </summary>
    public void ResetInputTracking()
    {
        _wasdMoveTimer = 0f;
        _jumpTimer = 0f;
        _descendTimer = 0f;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    Transform FindBeamTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnRange, enemyLayer);
        if (hitColliders.Length == 0) { return null; }
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;
        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }

    Transform FindMeleeTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnMeleeRange, enemyLayer);
        if (hitColliders.Length == 0) { return null; }
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;
        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }

    // void LockOnEnemies() // 特殊攻撃の項目をコメントアウト
    // { // 特殊攻撃の項目をコメントアウト
    //     lockedEnemies.Clear(); // 特殊攻撃の項目をコメントアウト
    //     Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer); // 特殊攻撃の項目をコメントアウト
    //     var sortedEnemies = hitColliders.OrderBy(col => Vector3.Distance(transform.position, col.transform.position)).Take(maxLockedEnemies); // 特殊攻撃の項目をコメントアウト
    //     foreach (Collider col in sortedEnemies) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         if (col.transform != transform) // 特殊攻撃の項目をコメントアウト
    //         { // 特殊攻撃の項目をコメントアウト
    //             lockedEnemies.Add(col.transform); // 特殊攻撃の項目をコメントアウト
    //             Debug.Log($"Locked on: {col.name}"); // 特殊攻撃の項目をコメントアウト
    //         } // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト
    //     if (lockedEnemies.Count > 0) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Vector3 lookAtTarget = lockedEnemies[0].position; // 特殊攻撃の項目をコメントアウト
    //         lookAtTarget.y = transform.position.y; // 特殊攻撃の項目をコメントアウト
    //         transform.LookAt(lookAtTarget); // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト
    // } // 特殊攻撃の項目をコメントアウト

    // void PerformBitAttack() // 特殊攻撃の項目をコメントアウト
    // { // 特殊攻撃の項目をコメントアウト
    //     if (bitSpawnPoints.Count == 0) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Debug.LogWarning("Bit spawn points are not set up in the Inspector. Cannot perform bit attack."); // 特殊攻撃の項目をコメントアウト
    //         return; // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト

    //     LockOnEnemies(); // 特殊攻撃の項目をコメントアウト

    //     if (lockedEnemies.Count == 0) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Debug.Log("No enemies to lock on. Bit attack cancelled."); // 特殊攻撃の項目をコメントアウト
    //         return; // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト

    //     if (currentEnergy < bitAttackEnergyCost * lockedEnemies.Count) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Debug.Log($"Not enough energy for Bit Attack! Need {bitAttackEnergyCost * lockedEnemies.Count} energy."); // 特殊攻撃の項目をコメントアウト
    //         return; // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト

    //     currentEnergy -= bitAttackEnergyCost * lockedEnemies.Count; // 特殊攻撃の項目をコメントアウト
    //     UpdateEnergyUI(); // 特殊攻撃の項目をコメントアウト

    //     isAttacking = true; // 特殊攻撃の項目をコメントアウト
    //     attackTimer = 0.0f; // 特殊攻撃の項目をコメントアウト

    //     int bitsToSpawn = Mathf.Min(lockedEnemies.Count, bitSpawnPoints.Count); // 特殊攻撃の項目をコメントアウト

    //     for (int i = 0; i < bitsToSpawn; i++) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Transform target = lockedEnemies[i]; // 特殊攻撃の項目をコメントアウト
    //         Transform spawnPoint = bitSpawnPoints[i]; // 特殊攻撃の項目をコメントアウト
    //         StartCoroutine(LaunchBit(bitPrefab, spawnPoint.position, target, bitLaunchDuration, bitAttackSpeed, bitDamage, bitArcHeight)); // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト
    //     onBitAttackPerformed?.Invoke(); // イベント発火 // 特殊攻撃の項目をコメントアウト
    // } // 特殊攻撃の項目をコメントアウト

    // IEnumerator LaunchBit(GameObject bitPrefab, Vector3 startPos, Transform target, float launchDuration, float attackSpeed, float damage, float arcHeight) // 特殊攻撃の項目をコメントアウト
    // { // 特殊攻撃の項目をコメントアウト
    //     GameObject bitInstance = Instantiate(bitPrefab, startPos, Quaternion.identity); // 特殊攻撃の項目をコメントアウト
    //     float startTime = Time.time; // 特殊攻撃の項目をコメントアウト
    //     Vector3 initialUpPos = startPos + Vector3.up * bitLaunchHeight; // 最初の上昇地点 // 特殊攻撃の項目をコメントアウト

    //     // 上昇アニメーション // 特殊攻撃の項目をコメントアウト
    //     while (Time.time < startTime + launchDuration) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         float t = (Time.time - startTime) / launchDuration; // 特殊攻撃の項目をコメントアウト
    //         bitInstance.transform.position = Vector3.Lerp(startPos, initialUpPos, t); // 特殊攻撃の項目をコメントアウト
    //         yield return null; // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト

    //     // ターゲット追尾 // 特殊攻撃の項目をコメントアウト
    //     while (bitInstance != null && target != null && target.gameObject.activeInHierarchy) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Vector3 directionToTarget = (target.position - bitInstance.transform.position).normalized; // 特殊攻撃の項目をコメントアウト
    //         bitInstance.transform.position += directionToTarget * attackSpeed * Time.deltaTime; // 特殊攻撃の項目をコメントアウト
    //         bitInstance.transform.LookAt(target); // 常にターゲットの方を向く // 特殊攻撃の項目をコメントアウト

    //         // ターゲットに十分近づいたら攻撃して破棄 // 特殊攻撃の項目をコメントアウト
    //         if (Vector3.Distance(bitInstance.transform.position, target.position) < 1.0f) // 適切な距離を設定 // 特殊攻撃の項目をコメントアウト
    //         { // 特殊攻撃の項目をコメントアウト
    //             // 敵にダメージを与える処理 (例: EnemyHealthスクリプトのTakeDamageメソッドを呼び出す) // 特殊攻撃の項目をコメントアウト
    //             EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>(); // 特殊攻撃の項目をコメントアウト
    //             if (enemyHealth == null) // 特殊攻撃の項目をコメントアウト
    //             { // 特殊攻撃の項目をコメントアウト
    //                 enemyHealth = target.GetComponentInChildren<EnemyHealth>(); // 特殊攻撃の項目をコメントアウト
    //             } // 特殊攻撃の項目をコメントアウト
    //             if (enemyHealth != null) // 特殊攻撃の項目をコメントアウト
    //             { // 特殊攻撃の項目をコメントアウト
    //                 enemyHealth.TakeDamage(damage); // 特殊攻撃の項目をコメントアウト
    //             } // 特殊攻撃の項目をコメントアウト
    //             Destroy(bitInstance); // 特殊攻撃の項目をコメントアウト
    //             yield break; // コルーチンを終了 // 特殊攻撃の項目をコメントアウト
    //         } // 特殊攻撃の項目をコメントアウト
    //         yield return null; // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト

    //     // ターゲットが消滅した場合など、追尾できなくなったらビットを破棄 // 特殊攻撃の項目をコメントアウト
    //     if (bitInstance != null) // 特殊攻撃の項目をコメントアウト
    //     { // 特殊攻撃の項目をコメントアウト
    //         Destroy(bitInstance); // 特殊攻撃の項目をコメントアウト
    //     } // 特殊攻撃の項目をコメントアウト
    // } // 特殊攻撃の項目をコメントアウト

    void PerformMeleeAttack()
    {
        if (Time.time < lastMeleeAttackTime + meleeAttackCooldown)
        {
            Debug.Log("Melee attack is on cooldown.");
            return;
        }

        lastMeleeAttackTime = Time.time;
        lastMeleeInputTime = Time.time; // コンボタイマーを更新

        // 攻撃中の固定開始
        isAttacking = true;
        attackTimer = 0.0f;

        // ターゲットを見つける
        currentLockedMeleeTarget = FindMeleeTarget();

        // ターゲットが存在し、かつ優先設定がされている場合、ターゲットの方向を向く
        if (currentLockedMeleeTarget != null && preferLockedMeleeTarget)
        {
            Vector3 lookAtTarget = currentLockedMeleeTarget.position;
            lookAtTarget.y = transform.position.y;
            transform.LookAt(lookAtTarget);
        }

        // 近接攻撃の突進コルーチンを開始
        StartCoroutine(MeleeDashAndAttack());

        // コンボ段階を進める
        currentMeleeCombo = (currentMeleeCombo + 1) % (maxMeleeCombo + 1); // 0からmaxMeleeComboまで
        if (currentMeleeCombo == 0) currentMeleeCombo = 1; // 0になったら1に戻す（1から開始）

        Debug.Log($"Melee Attack! Combo: {currentMeleeCombo}");
        onMeleeAttackPerformed?.Invoke(); // イベント発火
    }

    IEnumerator MeleeDashAndAttack()
    {
        Vector3 startPosition = transform.position;
        Vector3 dashTargetPosition;

        // ターゲットがいる場合はターゲットに向かって突進、いない場合はプレイヤーの正面に突進
        if (currentLockedMeleeTarget != null && preferLockedMeleeTarget)
        {
            dashTargetPosition = currentLockedMeleeTarget.position - transform.forward * 0.5f; // ターゲットの手前
        }
        else
        {
            dashTargetPosition = transform.position + transform.forward * meleeDashDistance;
        }

        float dashStartTime = Time.time;
        while (Time.time < dashStartTime + meleeDashDuration)
        {
            float t = (Time.time - dashStartTime) / meleeDashDuration;
            // CharacterController.Move はワールド座標での移動量を期待するので、Lerpで位置を計算し、差分でMoveを呼ぶ
            Vector3 newPosition = Vector3.Lerp(startPosition, dashTargetPosition, t);
            controller.Move(newPosition - transform.position); // 現在位置との差分をMoveに渡す
            yield return null;
        }

        // 突進終了後、攻撃判定
        PerformMeleeDamageCheck();
    }

    void PerformMeleeDamageCheck()
    {
        // プレイヤーの現在の位置から前方にSphereCast
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, meleeAttackRadius, transform.forward, meleeAttackRange, enemyLayer);

        foreach (RaycastHit hit in hits)
        {
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hit.collider.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(meleeDamage);
                Debug.Log($"{hit.collider.name} に近接攻撃で {meleeDamage} ダメージを与えました。");
            }
        }
    }

    void PerformBeamAttack()
    {
        if (Time.time < lastBeamAttackTime + beamCooldown)
        {
            Debug.Log("Beam attack is on cooldown.");
            return;
        }
        if (currentEnergy < beamAttackEnergyCost)
        {
            Debug.Log("Not enough energy for Beam Attack!");
            return;
        }

        currentEnergy -= beamAttackEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;

        // 攻撃中の固定開始
        isAttacking = true;
        attackTimer = 0.0f;

        // ターゲットを見つける
        currentLockedBeamTarget = FindBeamTarget();

        Vector3 startPoint = beamSpawnPoint.position;
        Vector3 endPoint;
        RaycastHit hitInfo;
        Transform hitEnemyTransform = null;

        if (currentLockedBeamTarget != null && preferLockedTarget)
        {
            endPoint = currentLockedBeamTarget.position;
            // ターゲットの方向を向く
            Vector3 lookAtTarget = currentLockedBeamTarget.position;
            lookAtTarget.y = transform.position.y; // Y軸は固定（水平方向のみ向く）
            transform.LookAt(lookAtTarget);

            // ロックオンしたターゲットに対してRaycastを飛ばし、間に障害物がないか確認
            if (Physics.Raycast(startPoint, (endPoint - startPoint).normalized, out hitInfo, beamAttackRange, enemyLayer))
            {
                if (hitInfo.collider.transform == currentLockedBeamTarget)
                {
                    endPoint = hitInfo.point;
                    hitEnemyTransform = currentLockedBeamTarget;
                }
                else
                {
                    // 途中に別の敵や障害物があった場合
                    endPoint = hitInfo.point;
                    hitEnemyTransform = hitInfo.collider.transform;
                }
            }
            else
            {
                // ロックオンした敵にRayが届かない場合（間に何もヒットしなかった場合）、最大射程まで伸ばす
                endPoint = startPoint + (endPoint - startPoint).normalized * beamAttackRange;
            }
        }
        else
        {
            // カメラの中心からRayを飛ばし、ヒットした場所をターゲットにする
            Ray ray = tpsCamController.GetCameraRay();
            if (Physics.Raycast(ray, out hitInfo, beamAttackRange, enemyLayer))
            {
                endPoint = hitInfo.point;
                hitEnemyTransform = hitInfo.collider.transform;
            }
            else
            {
                endPoint = ray.origin + ray.direction * beamAttackRange;
            }
        }

        // ビームエフェクトとラインレンダラーの表示
        StartCoroutine(ShowBeamEffectAndLine(startPoint, endPoint, hitEnemyTransform));

        onBeamAttackPerformed?.Invoke(); // イベント発火
    }

    IEnumerator ShowBeamEffectAndLine(Vector3 startPoint, Vector3 endPoint, Transform hitEnemyTransform)
    {
        GameObject beamVisualizer = new GameObject("BeamVisualizer");
        beamVisualizer.transform.position = startPoint;

        // Line Renderer の設定
        LineRenderer lineRenderer = beamVisualizer.AddComponent<LineRenderer>();
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 標準のシェーダー
        lineRenderer.startColor = Color.cyan; // ビームの色
        lineRenderer.endColor = Color.blue;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        // ビームエフェクトの生成（あれば）
        GameObject beamEffectInstance = null;
        if (beamEffectPrefab != null)
        {
            beamEffectInstance = Instantiate(beamEffectPrefab, startPoint, Quaternion.identity);
            beamEffectInstance.transform.LookAt(endPoint); // エフェクトをターゲット方向に向ける
            beamEffectInstance.transform.parent = beamVisualizer.transform; // ラインレンダラーの子にするかはお好みで
        }

        // 敵にダメージを与える
        if (hitEnemyTransform != null)
        {
            EnemyHealth enemyHealth = hitEnemyTransform.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hitEnemyTransform.GetComponentInChildren<EnemyHealth>();
            }
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(beamDamage);
                Debug.Log($"{hitEnemyTransform.name} にビーム攻撃で {beamDamage} ダメージを与えました。");
            }
        }

        // 指定時間表示した後、破棄
        yield return new WaitForSeconds(beamDisplayDuration);

        Destroy(beamVisualizer); // Line Rendererと子オブジェクト（エフェクト）をまとめて破棄
    }

    // 攻撃中のプレイヤーの状態を処理
    void HandleAttackState()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            attackTimer = 0.0f;
        }
    }

    /// <summary>
    /// 武器を装備/切り替えるメソッド
    /// </summary>
    /// <param name="primary">主武器のデータ</param>
    /// <param name="secondary">副武器のデータ</param>
    public void EquipWeapons(WeaponData primary, WeaponData secondary)
    {
        // 既存の武器を破棄
        if (currentPrimaryWeaponInstance != null) Destroy(currentPrimaryWeaponInstance);
        if (currentSecondaryWeaponInstance != null) Destroy(currentSecondaryWeaponInstance);

        // 主武器を装備
        if (primary != null && primary.weaponPrefab != null && primaryWeaponAttachPoint != null)
        {
            currentPrimaryWeaponInstance = Instantiate(primary.weaponPrefab, primaryWeaponAttachPoint);
            currentPrimaryWeaponInstance.transform.localPosition = Vector3.zero;
            currentPrimaryWeaponInstance.transform.localRotation = Quaternion.identity;
        }

        // 副武器を装備
        if (secondary != null && secondary.weaponPrefab != null && secondaryWeaponAttachPoint != null)
        {
            currentSecondaryWeaponInstance = Instantiate(secondary.weaponPrefab, secondaryWeaponAttachPoint);
            currentSecondaryWeaponInstance.transform.localPosition = Vector3.zero;
            currentSecondaryWeaponInstance.transform.localRotation = Quaternion.identity;
        }
    }
}