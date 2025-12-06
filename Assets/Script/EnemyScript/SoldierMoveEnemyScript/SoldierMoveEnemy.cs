using UnityEngine;
using UnityEngine.AI;

public class SoldierMoveEnemy : MonoBehaviour
{
    // ====================================================================
    // --- 1. ヘルスと死亡設定 (旧 EnemyHealth) ---
    // ====================================================================
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;
    private bool isDead = false; // ?? 死亡状態を追跡するフラグ

    // ====================================================================
    // --- 2. AI 状態定義と設定 (旧 ChaserAI) ---
    // ====================================================================
    public enum EnemyState { Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Idle;

    // --- AI 設定 ---
    public Transform player;
    public float sightRange = 15f;
    public float attackRange = 5f;
    public float rotationSpeed = 10f;

    // --- 攻撃設定 ---
    public int bulletsPerBurst = 3;
    public float timeBetweenShots = 0.1f;
    public float shootDuration = 0.5f;

    // --- 弾薬とリロード設定 ---
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;

    // ★★★ 銃弾の発射に必要な参照 ★★★
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // ====================================================================
    // --- 3. コンポーネントと初期化 ---
    // ====================================================================
    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private AudioSource audioSource;

    // ?? 死亡時に無効化する外部AIスクリプトの参照（EnemyHealthから移植）
    private EnemyAI aiA;
    private ChaserAI aiB;
    private JuggernautStaticAI aiOld;


    void Start()
    {
        // --- ヘルス初期化 ---
        currentHealth = maxHealth;

        // --- コンポーネント取得 ---
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // --- 外部AI参照取得 (Die()用) ---
        aiA = GetComponent<EnemyAI>();
        aiB = GetComponent<ChaserAI>();
        aiOld = GetComponent<JuggernautStaticAI>();

        // --- プレイヤー参照取得 ---
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
            else Debug.LogError("Playerタグのオブジェクトが見つかりません。");
        }

        // --- AI初期設定 ---
        currentAmmo = maxAmmo;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }
        TransitionToIdle();
    }

    // ====================================================================
    // --- 4. メインループ (AIロジック) ---
    // ====================================================================

    void Update()
    {
        // ?? 死亡チェック: 死亡状態なら即座に処理を終了
        if (isDead) return;

        if (player == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- 状態遷移判定 ---
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleLogic(distanceToPlayer);
                break;
            case EnemyState.Chase:
                ChaseLogic(distanceToPlayer);
                break;
            case EnemyState.Attack:
                AttackLogic(distanceToPlayer);
                break;
            case EnemyState.Reload:
                ReloadLogic();
                break;
        }

        // ?? アニメーション制御（移動速度連動）
        if (animator != null && agent.isActiveAndEnabled)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
        else if (animator != null)
        {
            // agentが無効な場合は強制的に停止アニメーション
            animator.SetBool("IsRunning", false);
        }
    }

    // ----------------------------------------------------
    // --- ヘルスとダメージ処理 (旧 EnemyHealth.TakeDamage) ---
    // ----------------------------------------------------

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + "がダメージを受けました。残り体力: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ----------------------------------------------------
    // --- 死亡処理 (旧 EnemyHealth.Die) ---
    // ----------------------------------------------------

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        Debug.Log(gameObject.name + "が倒れ、完全に停止します。");

        // 1. ?? 爆発エフェクトの生成
        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // 2. ?? アニメーションのトリガー
        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Die");
        }

        // 3. ?? 全てのAI、ナビゲーション、発砲ロジックを強制停止

        // ?? Invokeとコルーチンを全て停止
        CancelInvoke();
        StopAllCoroutines();

        // ?? NavMeshAgentの完全停止
        if (agent != null && agent.enabled)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        // ?? AI制御スクリプトを無効化 (ChaserAI自体と他の可能性のあるAIスクリプト)
        if (aiA != null) aiA.enabled = false;
        // ?? 統合対象のChaserAIスクリプト自身を無効化
        if (aiB != null) aiB.enabled = false;
        if (aiOld != null) aiOld.enabled = false;
        this.enabled = false; // ?? ChaserEnemyスクリプト自体を無効化

        // 4. ?? オーディオの停止 (オプション)
        // if (audioSource != null && audioSource.isPlaying) audioSource.Stop(); 

        // 5. ??? 物理的な固定と衝突判定の無効化
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
    }

    // ----------------------------------------------------
    // --- ロジック関数 (旧 ChaserAI) ---
    // ----------------------------------------------------

    void IdleLogic(float distance)
    {
        if (distance <= sightRange)
        {
            TransitionToChase();
        }
    }

    void ChaseLogic(float distance)
    {
        if (agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (distance <= attackRange)
        {
            TransitionToAttack();
        }
        else if (distance > sightRange)
        {
            TransitionToIdle();
        }
    }

    void AttackLogic(float distance)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        if (distance > attackRange * 1.2f)
        {
            TransitionToChase();
        }
        // Attack中は TransitionToAttackComplete でループするため、ここでは攻撃命令を出さない
    }

    void ReloadLogic()
    {
        // リロード中はアニメーションと時間待ちがメイン
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 (旧 ChaserAI) ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        if (isDead) return;
        currentState = EnemyState.Idle;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = true;
        }
        CancelInvoke(); // 全てのInvokeをキャンセル
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    void TransitionToChase()
    {
        if (isDead) return;
        currentState = EnemyState.Chase;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAttackComplete");
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    void TransitionToAttack()
    {
        if (isDead) return;

        if (currentAmmo <= 0)
        {
            TransitionToReload();
            return;
        }

        currentState = EnemyState.Attack;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = false; // 攻撃中はAIではなくスクリプトが回転を制御
        }

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsAiming", true);
            animator.SetTrigger("Shoot");
        }

        CancelInvoke("ShootBullet");

        for (int i = 0; i < bulletsPerBurst; i++)
        {
            Invoke("ShootBullet", i * timeBetweenShots);
        }

        float totalBurstTime = (bulletsPerBurst - 1) * timeBetweenShots;
        float totalAttackTime = totalBurstTime + shootDuration;

        Invoke("TransitionToAttackComplete", totalAttackTime);
    }

    void TransitionToAttackComplete()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack();
        }
        else
        {
            TransitionToChase();
        }
    }

    void TransitionToReload()
    {
        if (isDead) return;
        currentState = EnemyState.Reload;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAttackComplete");

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetTrigger("Reload");
        }

        Debug.Log("リロード開始... (" + reloadTime + "秒)");
        Invoke("FinishReload", reloadTime);
    }

    void FinishReload()
    {
        if (isDead) return;

        currentAmmo = maxAmmo;
        Debug.Log("リロード完了！");

        if (player == null)
        {
            TransitionToIdle();
            return;
        }

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack();
        }
        else if (distance <= sightRange)
        {
            TransitionToChase();
        }
        else
        {
            TransitionToIdle();
        }
    }

    // ----------------------------------------------------
    // --- 弾丸生成処理 (旧 ChaserAI) ---
    // ----------------------------------------------------

    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0) return;

        currentAmmo--;

        if (bulletPrefab == null || muzzlePoint == null)
        {
            return;
        }

        Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation).transform.parent = null;
        Debug.Log("弾が発射されました！");
    }
}