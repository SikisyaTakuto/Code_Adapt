using UnityEngine;
using UnityEngine.UI; // UI要素を扱うために追加
using System.Collections.Generic; // Listを使うために追加
using System.Linq; // OrderByを使うために追加

public class PlayerController : MonoBehaviour
{
    // 移動速度
    public float moveSpeed = 15.0f;
    // ブースト時の速度倍率
    public float boostMultiplier = 2.0f;
    // 上昇/下降速度
    public float verticalSpeed = 10.0f;
    // 重力の強さ
    public float gravity = -9.81f;
    // 地面判定のレイヤーマスク
    public LayerMask groundLayer;

    // --- エネルギーゲージ関連の変数 ---
    public float maxEnergy = 100.0f;
    public float currentEnergy;
    public float energyConsumptionRate = 15.0f;
    public float energyRecoveryRate = 10.0f;
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
    public float bitAttackEnergyCost = 20.0f; // ビット攻撃1回あたりのエネルギー消費 (変数名を変更)
    public float lockOnRange = 30.0f; // 敵をロックオンできる最大距離
    public LayerMask enemyLayer; // 敵のレイヤー
    public int maxLockedEnemies = 6; // ロックできる敵の最大数

    private List<Transform> lockedEnemies = new List<Transform>(); // ロックされた敵のリスト
    private bool isAttacking = false; // 攻撃中フラグ (プレイヤーの動きを固定するため)
    private float attackTimer = 0.0f; // 攻撃アニメーションや状態の継続時間タイマー (必要に応じて)
    public float attackFixedDuration = 0.8f; // 攻撃中にプレイヤーが固定される時間

    // --- ビットのスポーン位置を複数設定 ---
    public List<Transform> bitSpawnPoints = new List<Transform>();
    public float bitArcHeight = 2.0f; // 上昇軌道のアーチの高さ

    // --- 近接攻撃関連の変数 ---
    [Header("Melee Attack Settings")]
    public float meleeAttackRange = 2.0f; // 近接攻撃の有効範囲
    public float meleeAttackRadius = 1.0f; // 近接攻撃の有効半径 (SphereCast用)
    public float meleeAttackCooldown = 0.5f; // 近接攻撃のクールダウン時間
    private float lastMeleeAttackTime = -Mathf.Infinity; // 最後に近接攻撃をした時間
    private int currentMeleeCombo = 0; // 現在の近接攻撃コンボ段階
    public int maxMeleeCombo = 5; // 近接攻撃の最大コンボ段階
    public float comboResetTime = 1.0f; // コンボがリセットされるまでの時間
    private float lastMeleeInputTime; // 最後に近接攻撃入力があった時間

    // --- ビーム攻撃関連の変数 ---
    [Header("Beam Attack Settings")]
    public float beamAttackRange = 50.0f; // ビームの最大射程距離
    public float beamDamage = 50.0f; // ビームのダメージ
    public float beamEnergyCost = 10.0f; // ビーム攻撃1回あたりのエネルギー消費
    public float beamCooldown = 0.5f; // ビーム攻撃のクールダウン時間
    private float lastBeamAttackTime = -Mathf.Infinity; // 最後にビーム攻撃をした時間
    public GameObject beamEffectPrefab; // ビームのエフェクトPrefab (任意)
    public Transform beamSpawnPoint; // ビームの開始位置 (例: プレイヤーの目の前など)

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
    }

    void Update()
    {
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
        }
        // ホイール押込みでビット攻撃
        else if (Input.GetMouseButtonDown(2)) // 2はホイール押込み
        {
            PerformBitAttack();
        }
        // 右クリックでビーム攻撃
        else if (Input.GetMouseButtonDown(1)) // 1は右クリック
        {
            PerformBeamAttack();
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

        if (Input.GetKey(KeyCode.Space) && currentEnergy > 0)
        {
            velocity.y = verticalSpeed;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && currentEnergy > 0)
        {
            velocity.y = -verticalSpeed;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        else if (!isGrounded)
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
    /// 周囲の敵をロックオンする
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

            StartCoroutine(LaunchBit(spawnPoint.position, targetEnemy)); // Transformのpositionを渡す
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

        // 近接攻撃の範囲内の敵を検出
        Vector3 attackOrigin = transform.position + transform.forward * meleeAttackRange * 0.5f; // プレイヤーの前方少し離れた位置から
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, meleeAttackRadius, enemyLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            // 敵のHealthコンポーネントを探してダメージを与える
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                // コンボ段階によってダメージを変化させる例
                int damage = 10 + (currentMeleeCombo - 1) * 5; // 1段階目10、2段階目15...
                enemyHealth.TakeDamage(damage);
                Debug.Log($"{hitCollider.name} に {damage} ダメージを与えました。(コンボ {currentMeleeCombo})");
            }
        }
    }

    /// <summary>
    /// ビーム攻撃を実行する
    /// </summary>
    void PerformBeamAttack()
    {
        // クールダウン中、またはエネルギー不足、または既に攻撃中の場合は実行しない
        if (Time.time < lastBeamAttackTime + beamCooldown || currentEnergy < beamEnergyCost || isAttacking)
        {
            if (currentEnergy < beamEnergyCost)
            {
                Debug.Log("Not enough energy for Beam Attack!");
            }
            return;
        }

        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("Beam Spawn Point is not assigned. Cannot perform beam attack.");
            return;
        }

        currentEnergy -= beamEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;

        Debug.Log("ビーム攻撃！");

        // プレイヤーの向きをカメラの水平方向に合わせる
        if (tpsCamController != null)
        {
            tpsCamController.RotatePlayerToCameraDirection();
        }

        // ビームのRaycast開始位置と方向を決定
        Vector3 rayOrigin = beamSpawnPoint.position;
        Vector3 rayDirection = tpsCamController != null ? tpsCamController.transform.forward : transform.forward;

        RaycastHit hit;
        // Raycastで敵を検出
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, beamAttackRange, enemyLayer))
        {
            // ヒットした敵にダメージ
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(beamDamage);
                Debug.Log($"{hit.collider.name} にビームで {beamDamage} ダメージを与えました。");
            }

            // ヒット位置にビームエフェクトを生成 (もしあれば)
            if (beamEffectPrefab != null)
            {
                // ビームの開始点からヒット点までの間にエフェクトを調整することも可能
                Instantiate(beamEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            // 何もヒットしなかった場合でも、ビームの終点にエフェクトを生成するなどの処理
            if (beamEffectPrefab != null)
            {
                Instantiate(beamEffectPrefab, rayOrigin + rayDirection * beamAttackRange, Quaternion.identity);
            }
        }

        // ビーム攻撃中は一時的にプレイヤーの動きを固定
        isAttacking = true;
        attackTimer = 0.0f;
        // ビーム攻撃のアニメーションやエフェクトが続く時間に合わせてattackFixedDurationを調整
        // ただし、ビームは瞬間的な攻撃なので、ここでは短めに設定
        attackFixedDuration = 0.2f; // 例: ビーム発射のアニメーション時間
    }

    /// <summary>
    /// 攻撃中のプレイヤーの状態を処理（動き固定、向きの維持など）
    /// </summary>
    void HandleAttackState()
    {
        // 攻撃アニメーションやエフェクトの再生中にプレイヤーの動きを固定
        // ビット攻撃中はロックオンした敵に向ける
        if (Input.GetMouseButtonDown(2) && lockedEnemies.Count > 0 && lockedEnemies[0] != null) // ビット攻撃中の場合
        {
            Vector3 lookAtTarget = lockedEnemies[0].position;
            lookAtTarget.y = transform.position.y;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        // ビーム攻撃中はカメラの向きを維持
        else if (Input.GetMouseButtonDown(1) && tpsCamController != null) // ビーム攻撃中の場合
        {
            tpsCamController.RotatePlayerToCameraDirection(); // 再度プレイヤーをカメラ方向に強制
        }


        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            // ロックオンした敵が破壊された可能性があるため、クリーンアップ
            lockedEnemies.RemoveAll(t => t == null);
            // ロックオン解除のUI表示など、必要な処理を追加
            Debug.Log("Attack sequence finished.");
            // attackFixedDuration をデフォルトに戻すか、各攻撃のメソッド内で設定する
            attackFixedDuration = 0.8f; // ここでデフォルトに戻す例
        }
    }

    /// <summary>
    /// ビットを射出し、敵に向かって飛ばすコルーチン
    /// </summary>
    /// <param name="initialSpawnPosition">ビットの初期スポーン位置（ワールド座標）</param>
    /// <param name="target">ビットが向かうターゲット</param>
    System.Collections.IEnumerator LaunchBit(Vector3 initialSpawnPosition, Transform target)
    {
        if (bitPrefab != null)
        {
            GameObject bitInstance = Instantiate(bitPrefab, initialSpawnPosition, Quaternion.identity);
            Bit bitScript = bitInstance.GetComponent<Bit>();

            if (bitScript != null)
            {
                bitScript.InitializeBit(initialSpawnPosition, target, bitLaunchHeight, bitLaunchDuration, bitAttackSpeed, bitArcHeight, enemyLayer);
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

    // デバッグ表示用 (Gizmos)
    void OnDrawGizmosSelected()
    {
        // 近接攻撃の範囲を視覚化
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * meleeAttackRange * 0.5f, meleeAttackRadius);

        // ビーム攻撃の射程を視覚化
        Gizmos.color = Color.blue;
        if (beamSpawnPoint != null)
        {
            Gizmos.DrawRay(beamSpawnPoint.position, tpsCamController != null ? tpsCamController.transform.forward * beamAttackRange : transform.forward * beamAttackRange);
            Gizmos.DrawSphere(beamSpawnPoint.position + (tpsCamController != null ? tpsCamController.transform.forward : transform.forward) * beamAttackRange, 0.5f); // 終点に球
        }
        else
        {
            Gizmos.DrawRay(transform.position, transform.forward * beamAttackRange);
            Gizmos.DrawSphere(transform.position + transform.forward * beamAttackRange, 0.5f);
        }
    }
}