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

    [Header("硬直設定")]
    public float hardStopDuration = 2f;        // 攻撃後の硬直時間（秒）

    [Header("移動設定")]
    public float rotationSpeed = 5f;             // Player追跡時の回転速度
    public float wanderRadius = 10f;             // ランダム移動の最大半径
    public float destinationThreshold = 1.5f;    // 目的地到達と見なす距離
    public float maxIdleTime = 5f;             // 新しい目的地を設定するまでの最大静止時間（秒）

    // --- 内部変数 ---
    private float nextAttackTime = 0f;          // 次に攻撃可能な時間
    private float hardStopEndTime = 0f;          // 硬直が解除される時間
    private NavMeshAgent agent;                  // NavMeshAgentコンポーネント
    private float lastMoveTime = 0f;             // 最後に移動した時間
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
        // アニメーションの再生時間だけ待機
        yield return new WaitForSeconds(duration);

        // 爆発エフェクトのインスタンス化と再生
        if (explosionPrefab != null)
        {
            // サソリの現在の位置に生成
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // サソリ本体を削除
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------
    //                       その他ユーティリティ
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