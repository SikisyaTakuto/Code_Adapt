using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SuicideEnemy : MonoBehaviour
{
    // 敵のHP設定
    [Header("Health Settings")]
    [Tooltip("敵の最大HP")]
    public float maxHP = 100f;
    private float currentHP;

    // プレイヤーのTransform (Inspectorから設定)
    public Transform playerTarget;

    // NavMeshAgentコンポーネント
    private NavMeshAgent agent;

    // プレイヤーに接近する際の速さ
    public float moveSpeed = 5.0f;

    // 自爆を開始するプレイヤーとの距離
    public float suicideDistance = 2.0f;

    // 自爆のダメージ範囲 (爆発の半径)
    public float explosionRadius = 5.0f;

    // 自爆がプレイヤーに与えるダメージ
    public int explosionDamage = 50;

    // 爆発エフェクトのPrefab (Inspectorから設定)
    public GameObject explosionEffectPrefab;

    // 攻撃中/自爆準備中かどうかを判別するフラグ
    private bool isSuiciding = false;

    // --- ランダム移動（パトロール）の設定 ---
    [Header("Wander/Patrol Settings")]
    [Tooltip("ランダムな目的地を設定する最大範囲")]
    public float wanderRadius = 10f;
    [Tooltip("次の移動目標を設定するまでのクールタイム")]
    public float wanderTimer = 5f;
    private float timer;

    // 🚧 壁回避のための設定
    [Header("衝突回避設定 (NavMesh用)")]
    [Tooltip("NavMesh Agentの進行方向のチェック距離")]
    public float wallAvoidanceDistance = 1.5f;
    [Tooltip("障害物となるレイヤー (WallやDefaultなど)")]
    public LayerMask obstacleLayer;

    // 🚧 壁のタグ
    private const string WALL_TAG = "Wall";

    void Start()
    {
        currentHP = maxHP;
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        timer = wanderTimer;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default");
        }
    }

    void Update()
    {
        if (isSuiciding || agent == null || !agent.enabled) return;

        if (playerTarget == null)
        {
            Wander();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        CheckForWallCollision();

        if (distanceToPlayer <= suicideDistance)
        {
            agent.isStopped = true;
            SuicideAttack();
        }
        else
        {
            // 20f以上の距離、または視界外なら徘徊
            if (distanceToPlayer > 20f || !IsPlayerVisible())
            {
                Wander();
            }
            else
            {
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTarget.position);
                }
            }
        }
    }

    private void CheckForWallCollision()
    {
        if (agent.isStopped || agent.velocity.sqrMagnitude < 0.01f) return;

        RaycastHit hit;
        Vector3 movementDirection = agent.velocity.normalized;

        if (Physics.Raycast(transform.position, movementDirection, out hit, wallAvoidanceDistance, obstacleLayer))
        {
            if (hit.collider.CompareTag(WALL_TAG))
            {
                agent.isStopped = true;
                Wander();
            }
        }
    }

    private void Wander()
    {
        timer += Time.deltaTime;
        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            agent.isStopped = false;
            timer = 0f;
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist + origin;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randDirection, out hit, dist, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return origin;
    }

    private bool IsPlayerVisible()
    {
        if (playerTarget == null) return false;
        Vector3 direction = playerTarget.position - transform.position;
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, direction.magnitude))
        {
            if (hit.collider.CompareTag("Player")) return true;
            return false;
        }
        return true;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isSuiciding) return;
        currentHP -= damageAmount;
        if (currentHP <= 0) SuicideAttack();
    }

    // -------------------------------------------------------------------
    // ⭐ 修正ポイント: 3つのコントローラーに対応した自爆ダメージ処理
    // -------------------------------------------------------------------
    void SuicideAttack()
    {
        if (isSuiciding) return;
        isSuiciding = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            // Tagが"Player"であることを確認
            if (hitCollider.CompareTag("Player"))
            {
                bool damageApplied = false;

                // 親、自身、子のすべての階層からコントローラーを探す
                var blance = hitCollider.GetComponentInParent<BlanceController>() ?? hitCollider.GetComponentInChildren<BlanceController>();
                if (blance != null)
                {
                    blance.TakeDamage(explosionDamage);
                    damageApplied = true;
                }

                if (!damageApplied)
                {
                    var buster = hitCollider.GetComponentInParent<BusterController>() ?? hitCollider.GetComponentInChildren<BusterController>();
                    if (buster != null)
                    {
                        buster.TakeDamage(explosionDamage);
                        damageApplied = true;
                    }
                }

                if (!damageApplied)
                {
                    var speed = hitCollider.GetComponentInParent<SpeedController>() ?? hitCollider.GetComponentInChildren<SpeedController>();
                    if (speed != null)
                    {
                        speed.TakeDamage(explosionDamage);
                        damageApplied = true;
                    }
                }

                if (damageApplied) break; // プレイヤーに当たったらループ終了
            }
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, suicideDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (Application.isEditor && agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 movementDirection = agent.velocity.normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, movementDirection * wallAvoidanceDistance);
        }
    }
}