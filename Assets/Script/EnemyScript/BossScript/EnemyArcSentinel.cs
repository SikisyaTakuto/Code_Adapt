using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyArcSentinel : MonoBehaviour
{
    // ヘビーエネミーの状態を拡張
    private enum SentinelState { Idle, Following, Attacking, LaserCharging, Summoning, Recovering }
    private SentinelState currentState = SentinelState.Idle;

    // --- Components ---
    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;
    private Transform playerTransform;

    // --- Flags & Cooldowns ---
    private bool canAttackRanged = true;
    private bool canAttackMissile = true;
    private bool canAttackMelee = true; // パルスノヴァ
    private bool canSummon = true;
    private bool isAttacking = false;

    // --------------------------------------------------------------------------------
    // #region [Health & Movement Settings]
    // --------------------------------------------------------------------------------

    [Header("Health & Movement Settings (Sentinel)")]
    [Tooltip("ボスの最大HP。大幅に増加。")]
    public float maxHealth = 1500f; // ボスのため大幅強化
    [Tooltip("NavMeshAgentの移動速度。遅く設定。")]
    public float moveSpeed = 1.5f; // 重装甲で遅い
    [Tooltip("攻撃を開始する際のプレイヤーとの距離。")]
    public float activateAttackDistance = 40f; // 広い範囲で攻撃開始

    // --------------------------------------------------------------------------------
    // #region [Attack Settings]
    // --------------------------------------------------------------------------------

    // Missile Attack (UNCHANGED)
    [Header("Attack 1: Missile Strike")]
    public Transform missileSpawnPoint;
    public GameObject heavyMissilePrefab;
    public float missileAttackCooldown = 5.0f;
    public float missilePreparationTime = 1.0f;

    // Ranged Attack (Cannonball - UNCHANGED)
    [Header("Attack 2: Ranged Cannon")]
    public float rangedAttackRange = 35f;
    public float rangedAttackCooldown = 4.0f;
    public GameObject cannonballPrefab;
    public float cannonballSpeed = 30f;
    public float cannonballDamage = 75f;

    // Melee Attack (PULSE NOVA - REPLACED STOMP)
    [Header("Attack 3: Pulse Nova (Melee)")]
    [Tooltip("パルスノヴァ攻撃の射程 (範囲)。")]
    public float novaAttackRange = 8f;
    [Tooltip("パルスノヴァ攻撃のクールダウン。")]
    public float novaAttackCooldown = 3.0f;
    [Tooltip("パルスノヴァ攻撃のダメージ。")]
    public float novaDamage = 120f;

    // Laser Attack (REPLACED CHARGE)
    [Header("Attack 4: Arc Laser (Charged)")]
    [Tooltip("レーザー攻撃を開始する距離。")]
    public float laserAttackRange = 30f;
    [Tooltip("レーザー攻撃のクールダウン。")]
    public float laserAttackCooldown = 12.0f; // 長いクールダウン
    [Tooltip("レーザーチャージ時間（予兆）。")]
    public float laserChargeTime = 3.0f;
    [Tooltip("レーザーの持続時間。")]
    public float laserDuration = 2.0f;
    [Tooltip("レーザーのダメージ。")]
    public float laserDamage = 200f; // 高威力
    public GameObject laserVFXPrefab; // レーザーエフェクト用

    // Summon Attack (NEW)
    [Header("Attack 5: Minion Summon")]
    [Tooltip("敵を召喚するクールダウン。")]
    public float summonCooldown = 15.0f;
    [Tooltip("召喚する敵のPrefab。")]
    public GameObject minionPrefab;
    [Tooltip("一度に召喚する数。")]
    public int summonCount = 3;
    [Tooltip("召喚する位置の半径。")]
    public float summonRadius = 5f;

    // --------------------------------------------------------------------------------

    // Awake, Start, Update, TryAttack, MissileAttackRoutine, RangedAttackRoutineは基本的にHeavyControllerを踏襲

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();

        agent.speed = moveSpeed;
        enemyHealth.maxHealth = maxHealth;
        enemyHealth.currentHealth = maxHealth;

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
        currentState = playerTransform == null ? SentinelState.Idle : SentinelState.Following;
        agent.isStopped = (currentState == SentinelState.Idle);
    }

    void Update()
    {
        if (enemyHealth.currentHealth <= 0 || playerTransform == null)
        {
            agent.isStopped = true;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 状態に応じた処理（Idle, Following以外は移動停止）
        switch (currentState)
        {
            case SentinelState.Idle:
                if (distanceToPlayer <= activateAttackDistance)
                {
                    currentState = SentinelState.Following;
                }
                agent.isStopped = true;
                break;

            case SentinelState.Following:
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
                TryAttack(distanceToPlayer);

                if (distanceToPlayer > activateAttackDistance * 1.5f)
                {
                    currentState = SentinelState.Idle;
                }
                break;

            case SentinelState.Attacking:
            case SentinelState.LaserCharging:
            case SentinelState.Summoning:
            case SentinelState.Recovering:
                agent.isStopped = true;
                break;
        }

        // 常にプレイヤーの方向を向く（攻撃中やチャージ中も）
        Vector3 lookAtPlayer = playerTransform.position;
        lookAtPlayer.y = transform.position.y;
        transform.LookAt(lookAtPlayer);
    }

    // --------------------------------------------------------------------------------
    // #region [Attack Logic]
    // --------------------------------------------------------------------------------

    void TryAttack(float distanceToPlayer)
    {
        if (isAttacking) return;

        // 1. レーザー攻撃 (広範囲で長距離の主要攻撃、優先度高)
        if (canAttackLaser() && distanceToPlayer <= laserAttackRange)
        {
            StartCoroutine(LaserAttackRoutine());
            return;
        }

        // 2. 召喚攻撃 (クールダウンが長く、優先度中)
        if (canSummon && distanceToPlayer <= activateAttackDistance)
        {
            StartCoroutine(SummonRoutine());
            return;
        }

        // 3. パルスノヴァ (近距離の防御攻撃)
        if (canAttackMelee && distanceToPlayer <= novaAttackRange)
        {
            StartCoroutine(NovaAttackRoutine());
            return;
        }

        // 4. 砲撃 (中距離)
        if (canAttackRanged && distanceToPlayer <= rangedAttackRange)
        {
            StartCoroutine(RangedAttackRoutine());
            return;
        }

        // 5. ミサイル (汎用的な遠距離攻撃)
        if (canAttackMissile)
        {
            StartCoroutine(MissileAttackRoutine());
            return;
        }
    }

    // --- MissileAttackRoutine & RangedAttackRoutine (省略 - オリジナルと同様) ---
    IEnumerator MissileAttackRoutine()
    {
        isAttacking = true;
        canAttackMissile = false;
        currentState = SentinelState.Attacking;
        Debug.Log("ミサイル攻撃準備中...");
        yield return new WaitForSeconds(missilePreparationTime);

        if (heavyMissilePrefab != null && missileSpawnPoint != null)
        {
            Instantiate(heavyMissilePrefab, missileSpawnPoint.position, missileSpawnPoint.rotation);
            Debug.Log("ミサイル発射！");
        }

        yield return new WaitForSeconds(missileAttackCooldown);
        canAttackMissile = true;
        isAttacking = false;
        currentState = SentinelState.Following;
    }

    IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;
        canAttackRanged = false;
        currentState = SentinelState.Attacking;
        Debug.Log("砲撃準備中...");
        yield return new WaitForSeconds(0.5f); // 準備時間

        if (cannonballPrefab != null && missileSpawnPoint != null)
        {
            GameObject cannonball = Instantiate(cannonballPrefab, missileSpawnPoint.position, Quaternion.identity);
            Vector3 direction = (playerTransform.position - missileSpawnPoint.position).normalized;
            // Rigidbodyがある前提
            if (cannonball.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = direction * cannonballSpeed;
            }

            // Cannonballスクリプトがない場合、Destroyで寿命管理
            Destroy(cannonball, 3.0f);
            Debug.Log("砲撃！");
        }

        yield return new WaitForSeconds(rangedAttackCooldown);
        canAttackRanged = true;
        isAttacking = false;
        currentState = SentinelState.Following;
    }

    // --- パルスノヴァ攻撃 (近距離) ---
    IEnumerator NovaAttackRoutine()
    {
        isAttacking = true;
        canAttackMelee = false;
        currentState = SentinelState.Attacking;

        Debug.Log("パルスノヴァ攻撃準備中...");
        yield return new WaitForSeconds(0.5f); // 準備時間

        // 実際には、エフェクト生成やアニメーション再生を行う
        // 

        // ダメージ判定
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, novaAttackRange, LayerMask.GetMask("Player"));
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                // playerHealth?.TakeDamage(novaDamage);
                Debug.Log($"パルスノヴァでプレイヤーに {novaDamage} ダメージ！");
            }
        }

        yield return new WaitForSeconds(novaAttackCooldown);
        canAttackMelee = true;
        isAttacking = false;
        currentState = SentinelState.Following;
    }

    // --- レーザー攻撃 (Laser - NEW) ---
    bool canAttackLaser()
    {
        // 体力が一定以下になったら、レーザー攻撃のクールダウンを短縮するなど、ボス特有のロジックを追加可能
        return canSummon && canAttackRanged && canAttackMissile && enemyHealth.currentHealth < maxHealth * 0.7f; // 例: HP70%以下でミサイル、砲撃、召喚が全てクールダウン中の時に使用可能
        // シンプルにクールダウンのみ
        // return canSummon;
    }

    IEnumerator LaserAttackRoutine()
    {
        isAttacking = true;
        canSummon = false; // レーザー攻撃のクールダウンとして召喚のクールダウンを流用
        currentState = SentinelState.LaserCharging;

        Debug.Log("アークレーザー チャージ開始！");
        // **チャージフェーズ**
        yield return new WaitForSeconds(laserChargeTime);

        // **発射フェーズ**
        Debug.Log("アークレーザー 発射！");

        // レーザーエフェクトの生成とダメージ判定（LaserVFXPrefabに専用のスクリプトが必要）
        if (laserVFXPrefab != null)
        {
            // レーザーのインスタンス生成
            GameObject laserInstance = Instantiate(laserVFXPrefab, missileSpawnPoint.position, missileSpawnPoint.rotation, missileSpawnPoint);
            // レーザーの持続時間
            Destroy(laserInstance, laserDuration);
        }

        // ここでは即時判定 (実際のレーザーは Raycast などで連続ダメージを与える)
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= laserAttackRange)
        {
            // PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            // playerHealth?.TakeDamage(laserDamage);
            Debug.Log($"アークレーザーでプレイヤーに {laserDamage} ダメージ！");
        }

        yield return new WaitForSeconds(laserDuration);

        // **クールダウンフェーズ**
        Debug.Log("レーザー冷却中...");
        currentState = SentinelState.Recovering;
        yield return new WaitForSeconds(laserAttackCooldown);

        canSummon = true;
        isAttacking = false;
        currentState = SentinelState.Following;
    }

    // --- 召喚攻撃 (Summon - NEW) ---
    IEnumerator SummonRoutine()
    {
        isAttacking = true;
        canSummon = false;
        currentState = SentinelState.Summoning;

        Debug.Log("ミニオン召喚準備中...");
        yield return new WaitForSeconds(1.0f); // 準備時間

        if (minionPrefab != null)
        {
            for (int i = 0; i < summonCount; i++)
            {
                // 敵の周囲にランダムな位置を決定
                Vector3 randomPoint = transform.position + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * Random.Range(1f, summonRadius);
                randomPoint.y = transform.position.y; // Y軸を固定

                // NavMeshAgentに乗せるため、NavMesh上かチェックするロジックが必要だが、ここでは簡略化
                Instantiate(minionPrefab, randomPoint, Quaternion.identity);
            }
            Debug.Log($"{summonCount}体のミニオンを召喚しました！");
        }

        yield return new WaitForSeconds(summonCooldown);
        canSummon = true;
        isAttacking = false;
        currentState = SentinelState.Following;
    }

    // --------------------------------------------------------------------------------
    // #region [Gizmos]
    // --------------------------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // 攻撃開始距離
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activateAttackDistance);

        // 砲撃射程
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange);

        // パルスノヴァ射程
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, novaAttackRange);

        // レーザー射程
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, laserAttackRange);

        // 召喚範囲
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}