using UnityEngine;
using UnityEngine.AI;
using System.Collections; // コルーチンのために必要

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;

    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private NavMeshAgent agent;
    private AudioSource audioSource;

    // AIスクリプトを直接取得するための参照 (即時停止用)
    private EnemyAI aiA;
    private ChaserAI aiB;
    private JuggernautStaticAI aiOld;


    void Start()
    {
        currentHealth = maxHealth;

        // コンポーネントを取得
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        // 外部AIスクリプトの参照を取得
        aiA = GetComponent<EnemyAI>();
        aiB = GetComponent<ChaserAI>();
        aiOld = GetComponent<JuggernautStaticAI>();
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
    /// 死亡処理を行う関数 (アニメーション再生後に遅延破棄)
    /// </summary>
    void Die()
    {
        Debug.Log(gameObject.name + "が倒れ、アニメーション後に破棄されます。");

        currentHealth = 0f;

        // 1. 🚶 アニメーションのトリガー
        if (animator != null)
        {
            // 💡 修正: アニメーションを再生させるためにAnimatorは有効なままにしておく
            if (!animator.enabled) animator.enabled = true;
            animator.SetTrigger("Die");
        }

        // ===============================================
        // 💥 最重要: AIロジックのみを即座に強制停止する
        // ===============================================

        // 💡 実行中の発砲コルーチンやInvokeを全て停止
        StopAllCoroutines();
        CancelInvoke();

        // 💡 全てのAI制御スクリプトを無効化 (Update/FixedUpdateロジックを停止)
        if (aiA != null) aiA.enabled = false;
        if (aiB != null) aiB.enabled = false;
        if (aiOld != null) aiOld.enabled = false;

        // 💡 NavMeshAgentの完全停止
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 💡 AudioSourceの停止
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 💡 EnemyHealthスクリプト自身のUpdate/FixedUpdateを停止
        // このスクリプトは停止しても、コルーチンは動作し続ける
        this.enabled = false;

        // 💡 物理的な固定とコライダーの無効化 (即座に実行)
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // ===============================================

        // 2. 💣 爆発エフェクトの生成
        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // 3. 🗑️ 最終手段: 遅延破棄
        float animationDuration = 1.5f; // 💡 1.5秒間アニメーションを再生させます
        // 💡 修正: Animatorを渡してコルーチンを開始
        StartCoroutine(DestroyAfterDelay(animationDuration, animator));
    }

    /// <summary>
    /// 遅延後にAnimatorを停止し、オブジェクトを削除するコルーチン
    /// </summary>
    IEnumerator DestroyAfterDelay(float delay, Animator anim)
    {
        // 倒れるアニメーションが再生し終わるまで待機
        yield return new WaitForSeconds(delay);

        // 💥 破棄直前にAnimatorを強制無効化 (アニメーションイベントの発生を完全に防ぐ)
        if (anim != null)
        {
            anim.enabled = false;
        }

        // ゲームオブジェクトをシーンから完全に削除
        Destroy(gameObject);
    }
}