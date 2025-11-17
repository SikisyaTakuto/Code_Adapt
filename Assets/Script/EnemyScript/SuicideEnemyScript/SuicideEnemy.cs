using UnityEngine;
using UnityEngine.AI;

public class SuicideEnemy : MonoBehaviour
{
    // ?? 新規追加: HP設定
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

    // ?? 攻撃中/自爆準備中かどうかを判別するフラグ
    private bool isSuiciding = false;

    void Start()
    {
        // ?? HPを最大値で初期化
        currentHP = maxHP;

        // NavMeshAgentコンポーネントを取得
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // プレイヤーオブジェクトを検索して設定 (タグが"Player"の場合)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    void Update()
    {
        // ?? 自爆準備中またはプレイヤーがいなければ処理を停止
        if (playerTarget == null || isSuiciding) return;

        // プレイヤーまでの距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // プレイヤーが自爆距離内にいるか
        if (distanceToPlayer <= suicideDistance)
        {
            // --- 自爆処理の開始 ---
            SuicideAttack();
        }
        else
        {
            // プレイヤーを追いかける
            if (agent != null && agent.enabled)
            {
                agent.SetDestination(playerTarget.position);
            }
        }
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
                // PlayerControllerコンポーネントを取得
                PlayerController player = hitCollider.GetComponent<PlayerController>();

                if (player != null)
                {
                    // PlayerControllerのTakeDamageメソッドを呼び出す
                    player.TakeDamage(explosionDamage);
                    Debug.Log("Playerに " + explosionDamage + " ダメージを与えました！");
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
    }
}