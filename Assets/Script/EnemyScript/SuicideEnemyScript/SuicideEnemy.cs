using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public class SuicideEnemy : MonoBehaviour
{
    // 敵のHP設定
    [Header("Health Settings")]
    [Tooltip("敵の最大HP")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("UI Settings")]
    public Slider healthSlider;        // Slider本体
    public GameObject healthBarCanvas; // HPバーのCanvas
    public Image healthBarFillImage;   // SliderのFill(中身)のImage
    public Gradient healthGradient;    // HPに応じた色の変化設定

    // 💡 Tagで自動取得するため private に変更 (Inspectorでの設定は不要になります)
    private Transform playerTarget;

    // NavMeshAgentコンポーネント
    private NavMeshAgent agent;

    // プレイヤーに接近する際の速さ
    public float moveSpeed = 5.0f;

    // 自爆を開始するプレイヤーとの距離
    public float suicideDistance = 2.0f;

    // 自爆のダメージ範囲 (爆発の半径)
    public float explosionRadius = 5.0f;

    // 自爆がプレイヤーに与えるダメージ
    public int explosionDamage = 2000;

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

    [Header("Tire Settings")]
    public Transform[] tires;        // 4つのタイヤをインスペクターでアサイン
    public float tireRadius = 0.5f;   // タイヤの半径

    void Start()
    {
        currentHP = maxHP;

        // --- HPバーの初期化 (追加) ---
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = maxHP;
            UpdateHealthBarColor();
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = moveSpeed;

        timer = wanderTimer;
        FindPlayerTarget();

        if (obstacleLayer.value == 0) obstacleLayer = LayerMask.GetMask("Default");
    }

    /// <summary>
    /// 💡 Tag "Player" を持つオブジェクトを検索して設定します
    /// </summary>
    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    void Update()
    {
        if (isSuiciding || agent == null || !agent.enabled) return;

        // --- ビルボード処理: HPバーを常にカメラに向ける (追加) ---
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null) { Wander(); return; }
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
            if (distanceToPlayer > 20f || !IsPlayerVisible()) Wander();
            else
            {
                agent.isStopped = false;
                agent.SetDestination(playerTarget.position);
            }
        }

        // タイヤの回転処理
        if (tires != null && agent.enabled)
        {
            float speed = agent.velocity.magnitude;
            float rotationDegree = (speed * Time.deltaTime / tireRadius) * Mathf.Rad2Deg;

            foreach (Transform tire in tires)
            {
                if (tire != null)
                {
                    // ローカルのX軸で回転
                    tire.Rotate(Vector3.right, rotationDegree);
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
            if (agent != null && agent.enabled)
            {
                agent.SetDestination(newPos);
                agent.isStopped = false;
            }
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
        // 少し高い位置（目線の高さ）からレイを飛ばす
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, direction.magnitude))
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

        if (healthSlider != null)
        {
            healthSlider.value = currentHP;
            UpdateHealthBarColor();
        }

        if (currentHP <= 0) SuicideAttack();
    }

    private void UpdateHealthBarColor()
    {
        if (healthBarFillImage != null && healthSlider != null)
        {
            float healthRatio = currentHP / maxHP;
            healthBarFillImage.color = healthGradient.Evaluate(healthRatio);
        }
    }

    void SuicideAttack()
    {
        if (isSuiciding) return;
        isSuiciding = true;

        // --- 自爆時にHPバーを即座に隠す (追加) ---
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);

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
            if (hitCollider.CompareTag("Player"))
            {
                bool damageApplied = false;

                // 3つのコントローラーに対応
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

                if (damageApplied) break;
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