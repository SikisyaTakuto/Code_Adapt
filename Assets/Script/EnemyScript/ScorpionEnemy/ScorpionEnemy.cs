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
    public Transform playerTarget;
    public float detectionRange = 15f;
    public Transform beamOrigin;

    [Range(0, 180)]
    public float attackAngle = 30f;

    [Header("攻撃設定")]
    public float attackRate = 1f;
    public GameObject beamPrefab; // ※このPrefabに付いているスクリプトも修正が必要です
    public float beamSpeed = 30f;
    public int beamDamage = 20; // ビームのダメージ量を追加

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

        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) playerTarget = playerObject.transform;
        }

        lastMoveTime = Time.time;
        Wander();
    }

    private void Update()
    {
        // デバッグ用: Oキーで即死
        if (Input.GetKeyDown(KeyCode.O)) { TakeDamage(maxHealth); return; }

        if (isDead || playerTarget == null || Time.time < hardStopEndTime)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        if (agent == null || !agent.enabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            lastMoveTime = Time.time;
            CheckForWallCollision();
        }

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
            agent.isStopped = false;
            bool needNewDestination = !agent.hasPath || agent.remainingDistance < destinationThreshold || (Time.time - lastMoveTime) >= maxIdleTime;
            if (needNewDestination) Wander();
        }
    }

    // --- HPバー制御 (TPSCameraController用) ---
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

    // ScorpionEnemy.cs の AttackPlayer 内を以下に差し替え
    private void AttackPlayer()
    {
        if (beamOrigin == null || beamPrefab == null) return;

        // プレイヤーの方向を計算（中心を狙うように少し上にオフセットすると確実です）
        Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
        Vector3 direction = (targetPos - beamOrigin.position).normalized;
        float range = detectionRange + 5f;

        RaycastHit hit;
        // 全てのレイヤーを対象にするか、障害物とプレイヤーのレイヤーを指定
        bool didHit = Physics.Raycast(beamOrigin.position, direction, out hit, range);

        // デバッグ用の線（Sceneビューで確認用）
        Debug.DrawRay(beamOrigin.position, direction * range, Color.red, 1.0f);

        Vector3 endPoint = didHit ? hit.point : beamOrigin.position + (direction * range);

        GameObject beamObj = Instantiate(beamPrefab, beamOrigin.position, Quaternion.LookRotation(direction));
        EnemyBeamController beamController = beamObj.GetComponent<EnemyBeamController>();

        if (beamController != null)
        {
            // ヒットした相手を渡す
            beamController.Fire(beamOrigin.position, endPoint, didHit, didHit ? hit.collider.gameObject : null);

            if (didHit)
            {
                Debug.Log("ビームが的中しました: " + hit.collider.name);
            }
        }

        hardStopEndTime = Time.time + hardStopDuration;
    }

    // --- 移動・索敵ロジック (変更なし) ---
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
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= attackAngle / 2f;
    }

    private void LookAtPlayer()
    {
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
            // 1. 視界角の可視化
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            // 2. Wandering Radius の可視化
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);

            // 3. 衝突回避Raycastの可視化
            if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 movementDirection = agent.velocity.normalized;

                // 衝突回避Rayをマゼンタで表示
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, movementDirection * wallAvoidanceDistance);
            }
        }
    }
}