using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使うために必要

public class SuicideEnemy : MonoBehaviour
{
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

    void Start()
    {
        // NavMeshAgentコンポーネントを取得
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // プレイヤーオブジェクトを検索して設定 (タグが"Player"の場合)
        // ※より確実な方法で設定することをお勧めします
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    void Update()
    {
        // プレイヤーが設定されているか確認
        if (playerTarget == null) return;

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
            if (agent != null)
            {
                agent.SetDestination(playerTarget.position);
            }
        }
    }

    void SuicideAttack()
    {
        // 敵の動きを止める
        if (agent != null)
        {
            agent.isStopped = true;
        }

        // 爆発エフェクトの生成
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // --- 爆発のダメージ処理 ---
        // 爆発範囲内のオブジェクトを探す (Physics.OverlapSphereを使う)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            // プレイヤーかどうかの判定 (タグやコンポーネントで確認)
            if (hitCollider.CompareTag("Player"))
            {
                // プレイヤーのHealthコンポーネントなどを取得し、ダメージを与える処理
                // 例: hitCollider.GetComponent<PlayerHealth>().TakeDamage(explosionDamage);
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