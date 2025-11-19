using UnityEngine;
using UnityEngine.AI;

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

    // ?? 新規追加: ランダム移動（パトロール）の設定
    [Header("Wander/Patrol Settings")]
    [Tooltip("ランダムな目的地を設定する最大範囲")]
    public float wanderRadius = 10f;
    [Tooltip("次の移動目標を設定するまでのクールタイム")]
    public float wanderTimer = 5f;
    private float timer;


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

        // ?? 初期タイマーを設定
        timer = wanderTimer;

        // プレイヤーオブジェクトを検索して設定 (タグが"Player"の場合)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    void Update()
    {
        // 自爆準備中またはNavMeshAgentが無効であれば処理を停止
        if (isSuiciding || agent == null || !agent.enabled) return;

        // プレイヤーがいない場合、または距離が遠い場合の移動処理を統合
        if (playerTarget == null)
        {
            // プレイヤーがいない場合はランダム移動
            Wander();
            return;
        }

        // プレイヤーまでの距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // プレイヤーが自爆距離内にいるか
        if (distanceToPlayer <= suicideDistance)
        {
            // --- 自爆処理の開始 ---
            agent.isStopped = true; // 接近距離に入ったら追跡を停止
            SuicideAttack();
        }
        else
        {
            // プレイヤーを追いかける (追跡モード)
            // ?? Playerが遠くにいるか、Playerを失った場合、ランダム移動に切り替える
            if (distanceToPlayer > 20f || !IsPlayerVisible()) // 20fは一例として遠い距離を設定
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

    // ?? 新規追加: ランダムな目的地へ移動させるメソッド
    private void Wander()
    {
        timer += Time.deltaTime;

        // タイマーがクールタイムを超えたら新しい目的地を設定
        if (timer >= wanderTimer)
        {
            // 現在地からwanderRadius内のランダムな位置を取得
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);

            // 新しい目的地を設定
            agent.SetDestination(newPos);
            agent.isStopped = false;

            timer = 0f; // タイマーリセット
        }
    }

    // ?? 新規追加: 現在地周辺のNavMesh上のランダムな点を取得
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit hit;
        // NavMesh.SamplePositionで最も近いNavMesh上の点を取得
        NavMesh.SamplePosition(randDirection, out hit, dist, NavMesh.AllAreas);

        return hit.position;
    }

    // ?? 新規追加: プレイヤーが見えているかを簡易チェックするメソッド
    private bool IsPlayerVisible()
    {
        // ここでは単純に距離で判断するか、またはRaycastを使って視線が通っているかチェック
        // Raycastで視線チェックを行う方がより正確
        if (playerTarget == null) return false;

        Vector3 direction = playerTarget.position - transform.position;
        // 敵とプレイヤーの間に障害物があるかチェック
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, direction.magnitude))
        {
            // 衝突したオブジェクトのタグがPlayerであればOK
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
            return false; // 間に障害物がある
        }
        // 間に何も障害物がなければ視界が通っていると見なす
        return true;
    }

    /// <summary>
    /// 外部（プレイヤーの攻撃など）からダメージを受け付けるメソッド。
    /// </summary>
    /// <param name="damageAmount">受けるダメージ量。</param>
    public void TakeDamage(float damageAmount)
    {
        if (isSuiciding) return; // 自爆準備中は追加のダメージを無視

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

        // 敵の動きを止める (Updateで既に停止しているはずだが念のため)
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

        // --- 爆発のダメージ処理 (PlayerControllerとTutorialPlayerControllerに対応) ---
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
                    player.TakeDamage(explosionDamage);
                    Debug.Log("PlayerControllerに " + explosionDamage + " ダメージを与えました！");
                    damageApplied = true;
                }

                // 2. PlayerControllerが見つからなかった場合、TutorialPlayerControllerコンポーネントを探す
                if (!damageApplied)
                {
                    TutorialPlayerController tutorialPlayer = hitCollider.GetComponent<TutorialPlayerController>();
                    if (tutorialPlayer != null)
                    {
                        tutorialPlayer.TakeDamage(explosionDamage);
                        Debug.Log("TutorialPlayerControllerに " + explosionDamage + " ダメージを与えました！");
                        damageApplied = true;
                    }
                }

                if (!damageApplied)
                {
                    Debug.LogWarning("Playerタグのオブジェクトに TakeDamage を持つ PlayerController または TutorialPlayerController が見つかりませんでした。", hitCollider.gameObject);
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

        // ?? 新規追加: ランダム移動範囲を水色で表示
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}