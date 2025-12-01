using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI; // UIコンポーネント(Slider)を使用するため追加

public class ScorpionEnemy : MonoBehaviour
{
    // --- HP設定 ---
    [Header("ヘルス設定")]
    public float maxHealth = 100f; // 最大HP
    private float currentHealth;    // 現在のHP
    private bool isDead = false;    // 死亡フラグ

    // 💡 NEW: HPバーへの参照 (TPSCameraControllerから設定される)
    private Slider healthBarSlider;

    // VFX設定
    [Header("エフェクト設定")]
    public GameObject explosionPrefab;

    // 死亡アニメーション設定
    [Header("アニメーション設定")]
    public float deathAnimationDuration = 3.0f;

    // --- 索敵用パラメータ ---
    [Header("ターゲット設定")]
    public Transform playerTarget;              // PlayerのTransformを事前に設定
    public float detectionRange = 15f;          // Playerを発見する範囲
    public Transform beamOrigin;                // ビームの発射地点となるTransform

    [Range(0, 180)] // 視界角(Degree)
    public float attackAngle = 30f;             // 攻撃可能な視界角度(全角)

    [Header("攻撃設定")]
    public float attackRate = 1f;               // 1秒間に攻撃する回数
    public GameObject beamPrefab;               // 発射するビームのPrefab
    public float beamSpeed = 30f;               // ビームの速度

    private const string WALL_TAG = "Wall";

    [Header("硬直設定")]
    public float hardStopDuration = 2f;         // 攻撃後の硬直時間(秒)

    [Header("移動設定")]
    public float rotationSpeed = 5f;             // Player追尾時の回転速度
    public float wanderRadius = 10f;             // ランダム移動の最大半径
    public float destinationThreshold = 1.5f;    // 移動目標地点と見なす距離
    public float maxIdleTime = 5f;               // 新しい移動目標を設定するまでの最大停止時間(秒)

    [Header("衝突回避設定 (NavMesh用)")]
    public float wallAvoidanceDistance = 1.5f; // NavMesh Agentの進行方向へのチェック距離
    public LayerMask obstacleLayer;              // 障害物となるレイヤー

    // --- 内部変数 ---
    private float nextAttackTime = 0f;          // 次に攻撃可能な時間
    private float hardStopEndTime = 0f;         // 硬直が終わる時間
    private NavMeshAgent agent;                 // NavMeshAgentコンポーネント
    private float lastMoveTime = 0f;            // 最後に移動した時間
    private Animator animator;                  // Animatorコンポーネントへの参照

    private void Awake()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent componentが見つかりません。敵にNavMeshAgentをアタッチしてください。");
            enabled = false;
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator componentが見つかりません。敵にAnimatorをアタッチしてください。");
        }

        // Playerターゲットの自動検出
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        lastMoveTime = Time.time;
        Wander();
    }

    private void Update()
    {
        // デバッグ用コード: OキーでHPを0にする
        if (Input.GetKeyDown(KeyCode.O))
        {
            TakeDamage(maxHealth);
            return;
        }

        // 死亡中、硬直中、またはターゲットがない場合は処理をスキップ
        if (isDead || playerTarget == null || Time.time < hardStopEndTime)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        if (agent == null || !agent.enabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // --- 移動時間のチェックと更新 ---
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            lastMoveTime = Time.time;
            CheckForWallCollision();
        }

        // 2. Playerが攻撃範囲内にいるか？
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

            bool needNewDestination =
                !agent.hasPath ||
                agent.remainingDistance < destinationThreshold ||
                (Time.time - lastMoveTime) >= maxIdleTime;

            if (needNewDestination)
            {
                Wander();
            }
        }
    }

    // -------------------------------------------------------------------
    //      HPバー制御のための公開メソッド (TPSCameraControllerから呼び出される)
    // -------------------------------------------------------------------

    /// <summary>
    /// ロックオン時にカメラコントローラーからHPバー（Slider）を設定します。
    /// </summary>
    public void SetHealthBar(Slider slider)
    {
        healthBarSlider = slider;
        if (healthBarSlider != null)
        {
            // Sliderの最大値を設定し、現在のHPで値を初期化
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
            healthBarSlider.gameObject.SetActive(true); // HPバーを表示
        }
    }

    /// <summary>
    /// HPバーの現在の値を更新します。
    /// </summary>
    public void UpdateHealthBarValue()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
    }

    /// <summary>
    /// HPバーへの参照を削除し、UIを非表示にします（ロックオン解除時など）。
    /// </summary>
    public void ClearHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(false); // HPバーを非表示
            healthBarSlider = null; // 参照をクリア
        }
    }

    // -------------------------------------------------------------------
    //      衝突回避処理 (NavMesh用)
    // -------------------------------------------------------------------

    /// <summary>
    /// NavMeshAgentの進行方向の壁をチェックし、あれば強制的に移動目標を再設定します。
    /// </summary>
    private void CheckForWallCollision()
    {
        // Agentが移動中で、かつまだ移動目標に到着していない場合のみチェック
        if (agent.isStopped || agent.remainingDistance <= agent.stoppingDistance)
        {
            return;
        }

        RaycastHit hit;
        // Agentの進行方向 (velocityを正規化)
        Vector3 movementDirection = agent.velocity.normalized;

        // Raycastで前方に壁があるかチェック
        if (Physics.Raycast(transform.position, movementDirection, out hit, wallAvoidanceDistance, obstacleLayer))
        {
            // Raycastがヒットし、それがWALL_TAGを持っていた場合
            if (hit.collider.CompareTag(WALL_TAG))
            {
                Debug.LogWarning($"[{gameObject.name}] **移動方向の壁に衝突**! NavMeshAgentの動きを一時停止し、新しい移動目標を探します。");

                // 強制的に移動を停止
                agent.isStopped = true;

                // 新しい移動目標を探す (Wanderロジックを再実行)
                Wander();
            }
        }
    }

    // -------------------------------------------------------------------
    //      ヘルスと死亡処理
    // -------------------------------------------------------------------

    /// <summary>
    /// 外部からダメージを受け入れるためのメソッド
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        // 💡 UPDATE: ダメージを受けるたびにHPバーを更新
        UpdateHealthBarValue();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log(gameObject.name + "は破壊されました！");

        // 1. AnimatorのDeadパラメータをtrueに設定し、アニメーションを開始
        if (animator != null)
        {
            animator.SetBool("Dead", true);
        }

        // 2. NavMeshAgentを停止
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 💡 NEW: 死亡時、HPバーの参照をクリア
        ClearHealthBar();

        // 3. 死亡アニメーションの再生後に爆発エフェクトを再生してオブジェクトを削除するコルーチンを開始
        StartCoroutine(DeathSequence(deathAnimationDuration));
    }

    /// <summary>
    /// 死亡アニメーションが終了するのを待ち、爆発エフェクトを再生してオブジェクトを削除するコルーチン
    /// </summary>
    private IEnumerator DeathSequence(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------
    //      その他のユーティリティ
    // -------------------------------------------------------------------

    /// <summary>
    /// Playerがエネミーの前方視界角度内にいるかをチェックする
    /// </summary>
    private bool IsPlayerInFrontView()
    {
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);

        return angle <= attackAngle / 2f;
    }

    /// <summary>
    /// エネミーの向きをPlayerの方向に関連して回転させる（スムーズな回転）
    /// </summary>
    private void LookAtPlayer()
    {
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    /// <summary>
    /// NavMeshAgentを使ってランダムな場所へ移動する新しい移動目標を設定する
    /// </summary>
    private void Wander()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            lastMoveTime = Time.time;
        }
    }

    /// <summary>
    /// ビームを発射する
    /// </summary>
    private void AttackPlayer()
    {
        if (beamOrigin == null || beamPrefab == null)
        {
            Debug.LogError("ビームの発射源またはPrefabが設定されていません。");
            return;
        }

        Vector3 directionToPlayer = playerTarget.position - beamOrigin.position;
        Quaternion beamTargetRotation = Quaternion.LookRotation(directionToPlayer);

        GameObject beam = Instantiate(beamPrefab, beamOrigin.position, beamTargetRotation);

        Rigidbody rb = beam.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = beam.transform.forward * beamSpeed;
        }
        else
        {
            Debug.LogWarning("ビームPrefabにRigidbodyがありません。移動ロジックを追加してください。");
        }

        hardStopEndTime = Time.time + hardStopDuration;
    }

    // 範囲を確認できるようにするためのGizmo (エディタでのみ表示)
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