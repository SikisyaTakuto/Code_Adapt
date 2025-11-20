using UnityEngine;
using UnityEngine.AI;
using System.Collections; // コルーチンを使用するために必要

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

    // 🚧 新規追加: 壁回避のための設定
    [Header("衝突回避設定 (NavMesh用)")]
    [Tooltip("NavMesh Agentの進行方向のチェック距離")]
    public float wallAvoidanceDistance = 1.5f;
    [Tooltip("障害物となるレイヤー (WallやDefaultなど)")]
    public LayerMask obstacleLayer;

    // 🚧 新規追加: 壁のタグをここで定義
    private const string WALL_TAG = "Wall";


    void Start()
    {
        // HPを最大値で初期化
        currentHP = maxHP;

        // NavMeshAgentコンポーネントを取得
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // 初期タイマーを設定
        timer = wanderTimer;

        // プレイヤーオブジェクトを検索して設定 (タグが"Player"の場合)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        // obstacleLayerが未設定の場合、Defaultレイヤーに設定（推奨）
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default");
        }
    }

    void Update()
    {
        // 自爆準備中またはNavMeshAgentが無効であれば処理を停止
        if (isSuiciding || agent == null || !agent.enabled) return;

        // プレイヤーがいない場合はランダム移動
        if (playerTarget == null)
        {
            Wander();
            return;
        }

        // プレイヤーまでの距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 追跡中またはWander中に壁にぶつかりそうか常にチェック
        CheckForWallCollision();

        // プレイヤーが自爆距離内にいるか
        if (distanceToPlayer <= suicideDistance)
        {
            // --- 自爆処理の開始 ---
            agent.isStopped = true; // 接近距離に入ったら追跡を停止
            SuicideAttack();
        }
        else
        {
            // Playerが遠くにいるか、Playerを失った場合、ランダム移動に切り替える
            // 20fは一例として遠い距離。detectionRangeなど外部パラメータを使う方が良い。
            if (distanceToPlayer > 20f || !IsPlayerVisible())
            {
                Wander();
            }
            else
            {
                // プレイヤーを追いかける (追跡モード)
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTarget.position);
                }
            }
        }
    }

    // -------------------------------------------------------------------
    //          衝突回避処理 (NavMesh用)
    // -------------------------------------------------------------------

    /// <summary>
    /// NavMeshAgentの進行方向に壁がないかチェックし、あれば強制的に移動を中断・再探索させる
    /// </summary>
    private void CheckForWallCollision()
    {
        // Agentが移動中で、まだ目的地に到達していない場合のみチェック
        // agent.velocity.sqrMagnitude > 0.01f で実際に移動していることを確認
        if (agent.isStopped || agent.velocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        RaycastHit hit;
        // Agentの進行方向（velocityを正規化したもの）
        Vector3 movementDirection = agent.velocity.normalized;

        // Raycastで前方に壁があるかチェック
        if (Physics.Raycast(transform.position, movementDirection, out hit, wallAvoidanceDistance, obstacleLayer))
        {
            // Raycastが何かを検出し、それがWALL_TAGを持っている場合
            if (hit.collider.CompareTag(WALL_TAG))
            {
                Debug.LogWarning($"[{gameObject.name}] **移動方向の目の前に壁を検出**！追跡を停止し、新しい目的地を探します。");

                // 強制的に移動を停止
                agent.isStopped = true;

                // 新しい目的地を探す（Wanderロジックを再実行）
                Wander();
            }
        }
    }

    // -------------------------------------------------------------------
    //          移動処理
    // -------------------------------------------------------------------

    /// <summary>
    /// ランダムな目的地へ移動させるメソッド
    /// </summary>
    private void Wander()
    {
        timer += Time.deltaTime;

        // タイマーがクールタイムを超えたら新しい目的地を設定
        if (timer >= wanderTimer)
        {
            // 現在地からwanderRadius内のNavMesh上のランダムな位置を取得
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);

            // 新しい目的地を設定
            agent.SetDestination(newPos);
            agent.isStopped = false;

            timer = 0f; // タイマーリセット
        }
    }

    /// <summary>
    /// 現在地周辺のNavMesh上のランダムな点を取得
    /// </summary>
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit hit;
        // NavMesh.SamplePositionで最も近いNavMesh上の点を取得
        // NavMesh.AllAreas (-1) を指定
        if (NavMesh.SamplePosition(randDirection, out hit, dist, NavMesh.AllAreas))
        {
            return hit.position;
        }
        // 見つからない場合は元の位置を返す (安全策)
        return origin;
    }

    /// <summary>
    /// プレイヤーが見えているかを簡易チェックするメソッド
    /// </summary>
    private bool IsPlayerVisible()
    {
        if (playerTarget == null) return false;

        Vector3 direction = playerTarget.position - transform.position;
        // 敵とプレイヤーの間に障害物があるかチェック
        // LayerMask を使用して、Playerレイヤーは無視し、障害物レイヤーのみ検出対象にすることが望ましい
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, direction.magnitude))
        {
            // 衝突したオブジェクトのタグがPlayerであればOK
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
            return false; // 間にPlayer以外の障害物がある
        }
        // 間に何も障害物がなければ視界が通っていると見なす
        return true;
    }

    // -------------------------------------------------------------------
    //          ダメージと自爆処理
    // -------------------------------------------------------------------

    /// <summary>
    /// 外部（プレイヤーの攻撃など）からダメージを受け付けるメソッド。
    /// </summary>
    /// <param name="damageAmount">受けるダメージ量。</param>
    public void TakeDamage(float damageAmount)
    {
        // 自爆準備中は追加のダメージを無視 (自爆アニメーションなどに移行する場合)
        if (isSuiciding) return;

        currentHP -= damageAmount;
        Debug.Log($"SuicideEnemyが {damageAmount} ダメージを受けた。残りHP: {currentHP}");

        if (currentHP <= 0)
        {
            // HPがゼロになったら自爆攻撃を実行
            SuicideAttack();
        }
    }

    void SuicideAttack()
    {
        if (isSuiciding) return;
        isSuiciding = true; // フラグを立てて、UpdateやTakeDamageを停止させる

        // 敵の動きを止める
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false; // NavMeshAgentを無効化
        }

        // 爆発エフェクトの生成
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        // --- 爆発のダメージ処理 ---
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            // プレイヤーかどうかの判定
            if (hitCollider.CompareTag("Player"))
            {
                bool damageApplied = false;

                // 1. PlayerControllerコンポーネントを探す
                PlayerController player = hitCollider.GetComponent<PlayerController>();
                if (player != null)
                {
                    // ⭐ 修正: PlayerControllerのTakeDamageを呼び出す
                    player.TakeDamage(explosionDamage);
                    Debug.Log("PlayerControllerに " + explosionDamage + " ダメージを与えました！");
                    damageApplied = true;
                }

                // 2. TutorialPlayerControllerコンポーネントを探す
                if (!damageApplied)
                {
                    TutorialPlayerController tutorialPlayer = hitCollider.GetComponent<TutorialPlayerController>();
                    if (tutorialPlayer != null)
                    {
                        // ⭐ 修正: TutorialPlayerControllerのTakeDamageを呼び出す
                        tutorialPlayer.TakeDamage(explosionDamage);
                        Debug.Log("TutorialPlayerControllerに " + explosionDamage + " ダメージを与えました！");
                        damageApplied = true;
                    }
                }

                if (!damageApplied)
                {
                    Debug.LogWarning("Playerタグのオブジェクトに TakeDamage を持つコントローラーが見つかりませんでした。", hitCollider.gameObject);
                }
            }
        }

        // 自爆完了。自身を消滅させる
        Destroy(gameObject);
    }

    // 爆発範囲をSceneビューで可視化するためのギズモ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // 自爆開始距離をワイヤーフレームで表示
        Gizmos.DrawWireSphere(transform.position, suicideDistance);

        Gizmos.color = Color.yellow;
        // 爆発ダメージ範囲をワイヤーフレームで表示
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.cyan;
        // ランダム移動範囲を水色で表示
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // 🚧 新規追加: 移動中の壁回避Raycastの可視化
        if (Application.isEditor && agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 movementDirection = agent.velocity.normalized;

            // 壁検出Rayをマゼンタ色で表示
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, movementDirection * wallAvoidanceDistance);
        }
    }
}