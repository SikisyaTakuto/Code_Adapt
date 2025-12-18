using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public class ScorpionEnemy : MonoBehaviour
{
    // --- HP設定 ---
    [Header("ヘルス設定")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    private Slider healthBarSlider;

    // VFX設定
    [Header("エフェクト設定")]
    public GameObject explosionPrefab;

    [Header("アニメーション設定")]
    public float deathAnimationDuration = 3.0f;

    [Header("ターゲット設定")]
    private Transform playerTarget; // 💡 Tagで自動取得するため private に変更
    public float detectionRange = 15f;
    public Transform beamOrigin;

    [Range(0, 180)]
    public float attackAngle = 30f;

    [Header("攻撃設定")]
    public float attackRate = 1f;
    public GameObject beamPrefab;
    public float beamSpeed = 30f;
    public int beamDamage = 20;

    private const string WALL_TAG = "Wall";

    [Header("硬直設定")]
    public float hardStopDuration = 2f;

    [Header("移動設定")]
    public float rotationSpeed = 5f;
    public float wanderRadius = 10f;
    public float destinationThreshold = 1.5f;
    public float maxIdleTime = 5f;

    [Header("衝突回避設定 (NavMesh用)")]
    public float wallAvoidanceDistance = 1.5f;
    public LayerMask obstacleLayer;

    private float nextAttackTime = 0f;
    private float hardStopEndTime = 0f;
    private NavMeshAgent agent;
    private float lastMoveTime = 0f;
    private Animator animator;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 💡 起動時にプレイヤーをTagで検索
        FindPlayerWithTag();

        lastMoveTime = Time.time;
        Wander();
    }

    /// <summary>
    /// 💡 Tag "Player" を持つオブジェクトを検索して設定
    /// </summary>
    private void FindPlayerWithTag()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    private void Update()
    {
        // デバッグ用: Oキーで即死
        if (Input.GetKeyDown(KeyCode.O)) { TakeDamage(maxHealth); return; }

        if (isDead || Time.time < hardStopEndTime)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        // 💡 ターゲットがいない場合は再検索を試みる
        if (playerTarget == null)
        {
            FindPlayerWithTag();
            if (playerTarget == null)
            {
                // ターゲットが不在なら徘徊だけ行う
                HandleWanderLogic();
                return;
            }
        }

        if (agent == null || !agent.enabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            lastMoveTime = Time.time;
            CheckForWallCollision();
        }

        // --- 索敵・攻撃ロジック ---
        if (distanceToPlayer <= detectionRange)
        {
            agent.isStopped = true;
            LookAtPlayer();

            if (Time.time >= nextAttackTime && IsPlayerInFrontView())
            {
                AttackPlayer();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            HandleWanderLogic();
        }
    }

    // 徘徊ロジックを共通化
    private void HandleWanderLogic()
    {
        if (agent == null || !agent.enabled) return;

        agent.isStopped = false;
        bool needNewDestination = !agent.hasPath || agent.remainingDistance < destinationThreshold || (Time.time - lastMoveTime) >= maxIdleTime;
        if (needNewDestination) Wander();
    }

    // --- HPバー制御 ---
    public void SetHealthBar(Slider slider)
    {
        healthBarSlider = slider;
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
            healthBarSlider.gameObject.SetActive(true);
        }
    }

    public void UpdateHealthBarValue()
    {
        if (healthBarSlider != null) healthBarSlider.value = currentHealth;
    }

    public void ClearHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(false);
            healthBarSlider = null;
        }
    }

    // --- ダメージ・死亡処理 ---
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        UpdateHealthBarValue();
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (animator != null) animator.SetBool("Dead", true);
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        ClearHealthBar();
        StartCoroutine(DeathSequence(deathAnimationDuration));
    }

    private IEnumerator DeathSequence(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void AttackPlayer()
    {
        if (beamOrigin == null || beamPrefab == null || playerTarget == null) return;

        Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
        Vector3 direction = (targetPos - beamOrigin.position).normalized;
        float range = detectionRange + 5f;

        RaycastHit hit;
        bool didHit = Physics.Raycast(beamOrigin.position, direction, out hit, range);

        Debug.DrawRay(beamOrigin.position, direction * range, Color.red, 1.0f);

        Vector3 endPoint = didHit ? hit.point : beamOrigin.position + (direction * range);

        GameObject beamObj = Instantiate(beamPrefab, beamOrigin.position, Quaternion.LookRotation(direction));
        EnemyBeamController beamController = beamObj.GetComponent<EnemyBeamController>();

        if (beamController != null)
        {
            beamController.Fire(beamOrigin.position, endPoint, didHit, didHit ? hit.collider.gameObject : null);
        }

        hardStopEndTime = Time.time + hardStopDuration;
    }

    private void CheckForWallCollision()
    {
        if (agent.isStopped || agent.remainingDistance <= agent.stoppingDistance) return;
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

    private bool IsPlayerInFrontView()
    {
        if (playerTarget == null) return false;
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= attackAngle / 2f;
    }

    private void LookAtPlayer()
    {
        if (playerTarget == null) return;
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void Wander()
    {
        if (agent == null || !agent.enabled) return;
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            lastMoveTime = Time.time;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (Application.isEditor && transform != null)
        {
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }
    }
}