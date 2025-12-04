using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    // ✅ 修正後: public (公開アクセス可能)
    public float currentHealth; // 👈 ここを public に変更！
    public GameObject deathExplosionPrefab;

    // 💡 必要なコンポーネント
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private NavMeshAgent agent;
    // 💡 AudioSourceを追加（死亡時の発砲音などを止めるため）
    private AudioSource audioSource;

    void Start()
    {
        currentHealth = maxHealth;

        // 💡 コンポーネントを取得
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>(); // AudioSourceの取得
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + "がダメージを受けました。残り体力: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    /// <summary>
    /// 死亡処理を行う関数 (最終統合版)
    /// </summary>
    void Die()
    {
        Debug.Log(gameObject.name + "が倒れ、完全に停止します。");

        // 1. 💥 爆発エフェクトの生成（オプション）
        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // 2. 🚶 アニメーションのトリガー
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 3. 🛑 全てのAI、ナビゲーション、発砲ロジックを強制停止

        // 💡 NavMeshAgentの完全停止
        if (agent != null && agent.enabled)
        {
            // 🚨 追跡中の場合に安全に停止させる
            if (agent.isActiveAndEnabled)
            {
                // エージェントがアクティブで有効な場合にのみ停止フラグを立てる
                agent.isStopped = true;
            }

            // エージェント自体を無効化し、それ以上の動きを完全に停止
            agent.enabled = false;
        }

        // 💡 AI制御スクリプトを無効化 (全ての可能性のあるAIスクリプト名を網羅)
        EnemyAI aiA = GetComponent<EnemyAI>();
        if (aiA != null) aiA.enabled = false;

        ChaserAI aiB = GetComponent<ChaserAI>();
        if (aiB != null) aiB.enabled = false;

        JuggernautStaticAI aiOld = GetComponent<JuggernautStaticAI>();
        if (aiOld != null) aiOld.enabled = false;

        // 💡 実行中の発砲コルーチンやInvokeを全て停止
        StopAllCoroutines();

        // 4. 🔈 オーディオの停止
        //  if (audioSource != null && audioSource.isPlaying)
        // {
        //   audioSource.Stop();
        // }

        // EnemyHealth.cs の Die() 関数内

        // 5. 🛡️ 物理的な固定
        if (rb != null)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 💡 警告の原因となっていた行を削除
            // rb.velocity = Vector3.zero;        
            // rb.angularVelocity = Vector3.zero; // 👈 この行を削除します

            // 🚨 これが敵を固定する最重要処理。警告もなく、敵は完全に物理的な影響を無視して停止します。
            rb.isKinematic = true;
        }

        // 6. 衝突判定の無効化 (任意)
        //プレイヤーが死体をすり抜けるようにしたい場合はコメントアウトを外す
        if (enemyCollider != null)
         {
             enemyCollider.enabled = false; 
         }
    }
}