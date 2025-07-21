using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyHeavyController : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("敵の最大HP。")]
    public float maxHealth = 300f;
    private EnemyHealth enemyHealth;

    [Header("Attack Settings")]
    [Tooltip("ミサイルが生成される位置のTransform。")]
    public Transform missileSpawnPoint;
    [Tooltip("ミサイルのPrefab。")]
    public GameObject heavyMissilePrefab; // HeavyMissileスクリプト付きのPrefab
    [Tooltip("ミサイル攻撃のクールダウン時間。")]
    public float missileAttackCooldown = 5.0f;
    [Tooltip("ミサイル発射時の待機時間（チャージ）。")]
    public float missilePreparationTime = 1.5f;

    [Tooltip("遠距離攻撃（砲撃）の射程。")]
    public float rangedAttackRange = 30f;
    [Tooltip("遠距離攻撃のクールダウン。")]
    public float rangedAttackCooldown = 4.0f;
    [Tooltip("砲撃時のPrefab（オプション）。")]
    public GameObject cannonballPrefab; // 砲弾のPrefab
    [Tooltip("砲弾の速度。")]
    public float cannonballSpeed = 20f;
    [Tooltip("砲弾のダメージ。")]
    public float cannonballDamage = 50f;

    [Tooltip("近距離攻撃（踏みつけ）の射程。")]
    public float meleeAttackRange = 5f;
    [Tooltip("近距離攻撃（踏みつけ）のクールダウン。")]
    public float meleeAttackCooldown = 2.0f;
    [Tooltip("踏みつけ攻撃時のジャンプ力。")]
    public float stompJumpForce = 10f;
    [Tooltip("踏みつけ攻撃時の落下速度。")]
    public float stompFallSpeed = 20f;
    [Tooltip("踏みつけ攻撃のダメージ。")]
    public float stompDamage = 100f;

    [Tooltip("突進攻撃の射程。")]
    public float chargeAttackRange = 15f; // 突進攻撃を開始する距離
    [Tooltip("突進攻撃のクールダウン。")]
    public float chargeAttackCooldown = 8.0f;
    [Tooltip("突進時の移動速度。")]
    public float chargeSpeed = 15f;
    [Tooltip("突進攻撃の持続時間。")]
    public float chargeDuration = 1.5f;
    [Tooltip("突進攻撃のダメージ。")]
    public float chargeDamage = 70f;
    [Tooltip("突進攻撃後の硬直時間。")]
    public float chargeRecoveryTime = 1.0f;

    [Tooltip("攻撃を開始する際のプレイヤーとの距離。")]
    public float activateAttackDistance = 25f; // この距離内にプレイヤーがいたら攻撃行動を開始

    private bool canAttackMissile = true;
    private bool canAttackRanged = true;
    private bool canAttackMelee = true;
    private bool canAttackCharge = true;
    private bool isAttacking = false; // いずれかの攻撃が実行中かどうか

    [Header("Movement Settings")]
    [Tooltip("NavMeshAgentの移動速度。")]
    public float moveSpeed = 3.0f;
    [Tooltip("目的地に到達したと見なす距離。")]
    public float destinationThreshold = 1.5f;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private Rigidbody rb; // 踏みつけ攻撃などでRigidbodyが必要な場合

    private enum HeavyEnemyState { Idle, Following, Attacking, Charging, Stomping, Recovering }
    private HeavyEnemyState currentState = HeavyEnemyState.Idle;


    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>(); // RigidbodyをAwakeで取得

        if (rb == null)
        {
            Debug.LogWarning("Rigidbodyが見つかりません。踏みつけ攻撃は正しく機能しない可能性があります。", this);
        }

        agent.speed = moveSpeed;
        enemyHealth.maxHealth = maxHealth;
        enemyHealth.currentHealth = maxHealth; // StartではなくAwakeで初期化

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("プレイヤーオブジェクトに 'Player' タグが見つかりません。敵は攻撃行動を行いません。");
        }
    }

    void Start()
    {
        // 初期状態を設定
        if (playerTransform == null)
        {
            currentState = HeavyEnemyState.Idle;
            agent.isStopped = true;
        }
        else
        {
            currentState = HeavyEnemyState.Following;
            agent.isStopped = false;
        }
    }

    void Update()
    {
        if (enemyHealth.currentHealth <= 0 || playerTransform == null)
        {
            agent.isStopped = true;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 状態に応じた処理
        switch (currentState)
        {
            case HeavyEnemyState.Idle:
                // プレイヤーが一定距離に入ったらFollowingに移行
                if (distanceToPlayer <= activateAttackDistance)
                {
                    currentState = HeavyEnemyState.Following;
                }
                agent.isStopped = true;
                break;

            case HeavyEnemyState.Following:
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);

                // 攻撃条件のチェック
                TryAttack(distanceToPlayer);

                // プレイヤーが遠すぎたらIdleに戻る（任意）
                if (distanceToPlayer > activateAttackDistance * 1.5f) // 例として1.5倍の距離
                {
                    currentState = HeavyEnemyState.Idle;
                }
                break;

            case HeavyEnemyState.Attacking:
                agent.isStopped = true; // 攻撃中は移動停止
                // 攻撃コルーチンが終了したらFollowingに戻る
                break;

            case HeavyEnemyState.Charging:
                // 突進中はHeavyMissileが移動を制御
                break;

            case HeavyEnemyState.Stomping:
                // 踏みつけ中はHeavyMissileが移動を制御
                break;

            case HeavyEnemyState.Recovering:
                agent.isStopped = true; // 硬直中は停止
                break;
        }

        // 常にプレイヤーの方向を向く（Y軸固定）
        Vector3 lookAtPlayer = playerTransform.position;
        lookAtPlayer.y = transform.position.y;
        transform.LookAt(lookAtPlayer);
    }

    /// <summary>
    /// 距離に応じて攻撃を試みる
    /// </summary>
    void TryAttack(float distanceToPlayer)
    {
        if (isAttacking) return; // 他の攻撃が実行中ならスキップ

        // 突進攻撃の優先度を上げる（遠すぎず、近すぎない距離で）
        if (canAttackCharge && distanceToPlayer > meleeAttackRange * 1.5f && distanceToPlayer <= chargeAttackRange)
        {
            StartCoroutine(ChargeAttackRoutine());
            return;
        }

        // 近距離攻撃（踏みつけ）
        if (canAttackMelee && distanceToPlayer <= meleeAttackRange)
        {
            StartCoroutine(StompAttackRoutine());
            return;
        }

        // 遠距離攻撃（砲撃）
        if (canAttackRanged && distanceToPlayer <= rangedAttackRange)
        {
            StartCoroutine(RangedAttackRoutine());
            return;
        }

        // ミサイル攻撃 (常に遠距離攻撃として考慮できる)
        if (canAttackMissile)
        {
            StartCoroutine(MissileAttackRoutine());
            return;
        }
    }

    /// <summary>
    /// ミサイル攻撃のコルーチン
    /// </summary>
    IEnumerator MissileAttackRoutine()
    {
        isAttacking = true;
        canAttackMissile = false;
        currentState = HeavyEnemyState.Attacking;
        agent.isStopped = true;

        Debug.Log("ミサイル攻撃準備中...");
        yield return new WaitForSeconds(missilePreparationTime);

        if (heavyMissilePrefab != null && missileSpawnPoint != null)
        {
            // ミサイル生成
            GameObject missileInstance = Instantiate(heavyMissilePrefab, missileSpawnPoint.position, missileSpawnPoint.rotation);
            // HeavyMissileスクリプトが自動でターゲットを設定し、追尾とダメージ処理を行う
            Debug.Log("ミサイル発射！");
        }

        yield return new WaitForSeconds(missileAttackCooldown);
        canAttackMissile = true;
        isAttacking = false;
        currentState = HeavyEnemyState.Following; // 攻撃終了後、通常移動に戻る
    }

    /// <summary>
    /// 遠距離攻撃（砲撃）のコルーチン
    /// </summary>
    IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;
        canAttackRanged = false;
        currentState = HeavyEnemyState.Attacking;
        agent.isStopped = true;

        Debug.Log("砲撃準備中...");
        yield return new WaitForSeconds(1.0f); // 砲撃の準備時間

        if (cannonballPrefab != null)
        {
            // 砲弾を生成し、プレイヤーの方向へ発射
            GameObject cannonball = Instantiate(cannonballPrefab, missileSpawnPoint.position, Quaternion.identity); // missileSpawnPointを流用
            Vector3 direction = (playerTransform.position - missileSpawnPoint.position).normalized;
            cannonball.GetComponent<Rigidbody>().linearVelocity = direction * cannonballSpeed;

            // 砲弾のダメージを設定（Cannonballスクリプトを作成する場合）
            Cannonball projectileScript = cannonball.GetComponent<Cannonball>();
            if (projectileScript != null)
            {
                projectileScript.damageAmount = cannonballDamage;
            }
            // Cannonballスクリプトがない場合、Destroyで寿命管理
            Destroy(cannonball, 3.0f); // 3秒後に消滅
            Debug.Log("砲撃！");
        }

        yield return new WaitForSeconds(rangedAttackCooldown);
        canAttackRanged = true;
        isAttacking = false;
        currentState = HeavyEnemyState.Following;
    }

    /// <summary>
    /// 近距離攻撃（踏みつけ）のコルーチン
    /// </summary>
    IEnumerator StompAttackRoutine()
    {
        isAttacking = true;
        canAttackMelee = false;
        currentState = HeavyEnemyState.Stomping; // 踏みつけ状態
        agent.isStopped = true; // 移動停止

        Debug.Log("踏みつけ準備中...");
        // プレイヤーの真上にジャンプ
        Vector3 targetJumpPosition = new Vector3(playerTransform.position.x, transform.position.y + 5f, playerTransform.position.z); // 5m上空へ
        agent.enabled = false; // ジャンプ中はNavMeshAgentを無効化
        if (rb != null)
        {
            rb.isKinematic = false; // 物理演算を有効化
            rb.AddForce(Vector3.up * stompJumpForce, ForceMode.Impulse); // 上方向へジャンプ
        }

        float jumpTime = 0.5f; // ジャンプ上昇の時間
        yield return new WaitForSeconds(jumpTime); // 上昇待ち

        Debug.Log("落下！");
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * stompFallSpeed; // 急速落下
        }

        // 地面に着地するまで待つ（または一定時間）
        yield return new WaitUntil(() => rb == null || rb.linearVelocity.y <= 0.1f && !Physics.Raycast(transform.position, Vector3.down, 0.1f, LayerMask.GetMask("Ground"))); // 地面レイヤーに衝突するまで待つ（例）
        // もしくは固定の時間待つ
        yield return new WaitForSeconds(0.5f); // 着地までの予測時間

        // 着地時のダメージ判定 (円形判定など)
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= meleeAttackRange)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(stompDamage);
                Debug.Log($"踏みつけ攻撃でプレイヤーに {stompDamage} ダメージ！");
            }
        }

        // Rigidbodyを元に戻す
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero; // 残った速度をリセット
        }
        agent.enabled = true; // NavMeshAgentを有効化

        yield return new WaitForSeconds(meleeAttackCooldown);
        canAttackMelee = true;
        isAttacking = false;
        currentState = HeavyEnemyState.Following;
    }

    /// <summary>
    /// 突進攻撃のコルーチン
    /// </summary>
    IEnumerator ChargeAttackRoutine()
    {
        isAttacking = true;
        canAttackCharge = false;
        currentState = HeavyEnemyState.Charging; // 突進状態
        agent.isStopped = true; // 移動停止

        Debug.Log("突進攻撃準備中...");
        // プレイヤーの方を向く（厳密に）
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dirToPlayer.x, 0, dirToPlayer.z));
        transform.rotation = lookRotation;

        yield return new WaitForSeconds(0.8f); // 突進前の短い準備時間

        Debug.Log("突進開始！");
        float chargeTimer = chargeDuration;
        Vector3 chargeDirection = transform.forward; // 突進開始時の前方方向

        while (chargeTimer > 0)
        {
            agent.Move(chargeDirection * chargeSpeed * Time.deltaTime); // NavMeshAgentで移動（障害物を避ける）
            // もしくは直接transform.positionを動かす
            // transform.position += chargeDirection * chargeSpeed * Time.deltaTime;

            // 突進中にプレイヤーに当たったらダメージ
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f, LayerMask.GetMask("Player")); // 敵の周囲1.5m
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(chargeDamage);
                        Debug.Log($"突進攻撃でプレイヤーに {chargeDamage} ダメージ！");
                    }
                    // ダメージを与えたら突進を中断するかどうか (今回は続ける)
                    // break;
                }
            }

            chargeTimer -= Time.deltaTime;
            yield return null; // 1フレーム待機
        }

        Debug.Log("突進終了、硬直中...");
        currentState = HeavyEnemyState.Recovering; // 硬直状態へ
        yield return new WaitForSeconds(chargeRecoveryTime); // 硬直時間

        canAttackCharge = true;
        isAttacking = false;
        currentState = HeavyEnemyState.Following;
        agent.isStopped = false; // 移動再開
    }


    // デバッグ表示用
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activateAttackDistance); // 敵が攻撃を開始する距離

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange); // 砲撃射程

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange); // 踏みつけ射程

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chargeAttackRange); // 突進射程
    }
}