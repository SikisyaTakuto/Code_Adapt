using UnityEngine;
using UnityEngine.AI;
using System.Collections; // コルーチンを使用するため必要

public class ScorpionEnemy : MonoBehaviour
{
    // --- HP設定 ---
    [Header("ヘルス設定")]
    public float maxHealth = 100f; // 最大HP
    private float currentHealth;   // 現在のHP
    private bool isDead = false;   // 死亡フラグ

    // 新規追加: 爆発エフェクトのPrefab
    [Header("エフェクト設定")]
    public GameObject explosionPrefab;

    // 死亡アニメーション時間 (Inspectorで設定)
    [Header("アニメーション設定")]
    public float deathAnimationDuration = 3.0f;

    // --- 公開パラメータ ---
    [Header("ターゲット設定")]
    public Transform playerTarget;             // PlayerのTransformをここに設定
    public float detectionRange = 15f;         // Playerを検出する範囲
    public Transform beamOrigin;               // ビームの発射元となるTransform (サソリの尾の先など)

    [Range(0, 180)] // 視野角（Degree）
    public float attackAngle = 30f;            // 攻撃可能な正面視野角（全角）

    [Header("攻撃設定")]
    public float attackRate = 1f;              // 1秒間に攻撃する回数 
    public GameObject beamPrefab;              // 発射するビームのPrefab
    public float beamSpeed = 30f;              // ビームの速度

    // ? 修正: 壁のタグをここで定義 (攻撃処理にも使用)
    private const string WALL_TAG = "Wall";

    [Header("硬直設定")]
    public float hardStopDuration = 2f;        // 攻撃後の硬直時間（秒）

    [Header("移動設定")]
    public float rotationSpeed = 5f;             // Player追跡時の回転速度
    public float wanderRadius = 10f;             // ランダム移動の最大半径
    public float destinationThreshold = 1.5f;    // 目的地到達と見なす距離
    public float maxIdleTime = 5f;             // 新しい目的地を設定するまでの最大静止時間（秒）

    // ?? 新規追加: 壁回避のための設定
    [Header("衝突回避設定 (NavMesh用)")]
    public float wallAvoidanceDistance = 1.5f; // NavMesh Agentの進行方向のチェック距離
    public LayerMask obstacleLayer;             // 障害物となるレイヤー (WallやDefaultなど)


    // --- 内部変数 ---
    private float nextAttackTime = 0f;          // 次に攻撃可能な時間
    private float hardStopEndTime = 0f;         // 硬直が解除される時間
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

        // Playerターゲットの自動検出 (AWAKEに追加)
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

        // 死亡時、硬直中、またはターゲットがない場合は処理をスキップ
        if (isDead || playerTarget == null || Time.time < hardStopEndTime)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        if (agent == null || !agent.enabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // --- 移動状態のチェックと更新 ---
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            lastMoveTime = Time.time;

            // ?? 新規追加: 移動中に壁に近づきすぎていないかチェック
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
    //          衝突回避処理 (NavMesh用)
    // -------------------------------------------------------------------

    /// <summary>
    /// NavMeshAgentの進行方向に壁がないかチェックし、あれば強制的に移動を中断・再探索させる
    /// </summary>
    private void CheckForWallCollision()
    {
        // Agentが移動中で、まだ目的地に到達していない場合のみチェック
        if (agent.isStopped || agent.remainingDistance <= agent.stoppingDistance)
        {
            return;
        }

        RaycastHit hit;
        // Agentの進行方向（velocityを正規化したもの）
        Vector3 movementDirection = agent.velocity.normalized;

        // Raycastで前方に壁があるかチェック
        // Agentの進行方向（velocity）を使ってチェックすることで、NavMeshAgentの軌道を先読みします。
        if (Physics.Raycast(transform.position, movementDirection, out hit, wallAvoidanceDistance, obstacleLayer))
        {
            // Raycastが何かを検出し、それがWALL_TAGを持っている場合
            if (hit.collider.CompareTag(WALL_TAG))
            {
                Debug.LogWarning($"[{gameObject.name}] **移動方向の目の前に壁を検出**！NavMeshAgentのすり抜けを防止し、新しい目的地を探します。");

                // 強制的に移動を停止
                agent.isStopped = true;

                // 新しい目的地を探す（Wanderロジックを再実行）
                Wander();
            }
        }
    }

    // -------------------------------------------------------------------
    //          ヘルスと死亡処理 (変更なし)
    // -------------------------------------------------------------------

    /// <summary>
    /// 外部からダメージを受け取るための公開メソッド
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

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

        // 1. AnimatorのDeadパラメータをtrueに設定してアニメーションを開始
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

        // 3. 死亡アニメーションの再生後に爆発・削除を行うコルーチンを開始
        StartCoroutine(DeathSequence(deathAnimationDuration));
    }

    /// <summary>
    /// 死亡アニメーションが終了するのを待ち、爆発エフェクトを再生してからオブジェクトを削除するコルーチン
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
    //          その他ユーティリティ (変更なし)
    // -------------------------------------------------------------------

    /// <summary>
    /// Playerがエネミーの前方視野角内にいるかをチェックする
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
    /// ドローン本体の向きをPlayerの方向へ向ける（スムーズな回転）
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
    /// NavMeshAgentを使って周囲をランダムに移動する新しい目的地を設定する
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
            Debug.LogError("ビームの発射元またはPrefabが設定されていません。");
            return;
        }

        Vector3 directionToPlayer = playerTarget.position - beamOrigin.position;
        Quaternion beamTargetRotation = Quaternion.LookRotation(directionToPlayer);

        GameObject beam = Instantiate(beamPrefab, beamOrigin.position, beamTargetRotation);

        Rigidbody rb = beam.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = beam.transform.forward * beamSpeed;
        }
        else
        {
            Debug.LogWarning("ビームPrefabにRigidbodyがありません。移動ロジックを追加してください。");
        }

        hardStopEndTime = Time.time + hardStopDuration;
    }

    // 範囲を可視化するためのGizmo (エディタでのみ表示)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (Application.isEditor && transform != null)
        {
            // 1. 視野角の可視化
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

            // 3. ?? 新規追加: 移動中の壁回避Raycastの可視化
            if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 movementDirection = agent.velocity.normalized;

                // 壁検出Rayをマゼンタ色で表示
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, movementDirection * wallAvoidanceDistance);
            }
        }
    }
}