using UnityEngine;
using System.Collections;

public class SoldierMoveEnemy : MonoBehaviour
{
    // ====================================================================
    // --- 1. ヘルスと死亡設定 ---
    // ====================================================================
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;
    private bool isDead = false;

    // ====================================================================
    // --- 2. AI 状態定義と設定 ---
    // ====================================================================
    public enum EnemyState { Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Idle;

    // --- AI 設定 ---
    // プレイヤー参照は "Player" タグで取得されます
    [SerializeField] private Transform player; // private + [SerializeField]
    public float sightRange = 15f;
    public float attackRange = 5f;
    public float rotationSpeed = 10f;

    // Rigidbody/Transform移動用の速度パラメータ
    // ?? プレイヤーへの接近速度をデフォルトで速くしました。
    public float moveSpeed = 6.0f;

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
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private AudioSource audioSource;

    // 死亡時に無効化する外部AIスクリプトの参照
    private EnemyAI aiA;
    private ChaserAI aiB;
    private JuggernautStaticAI aiOld;


    void Start()
    {
        // --- ヘルス初期化 ---
        currentHealth = maxHealth;

        // --- コンポーネント取得 ---
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // Rigidbody設定確認: 
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.freezeRotation = true; // 追跡AIが倒れないように回転を固定
        }

        // --- 外部AI参照取得 (Die()用) ---
        aiA = GetComponent<EnemyAI>();
        aiB = GetComponent<ChaserAI>();
        aiOld = GetComponent<JuggernautStaticAI>();

        // プレイヤー参照取得 (Tagを使用)
        FindPlayerWithTag();

        // --- AI初期設定 ---
        currentAmmo = maxAmmo;
        TransitionToIdle();
    }

    /// <summary>
    /// Tag "Player" を持つオブジェクトを検索し、参照を設定します。
    /// </summary>
    void FindPlayerWithTag()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
            else Debug.LogError("Playerタグのオブジェクトが見つかりません。AIは動作しません。");
        }
    }


    // ====================================================================
    // --- 4. メインループ (AIロジック) ---
    // ====================================================================

    // 物理演算による移動のため、FixedUpdate()を使用
    void FixedUpdate()
    {
        // 死亡チェック: 死亡状態なら即座に処理を終了
        if (isDead) return;

        // Rigidbodyがないかプレイヤーがない場合はロジックをスキップ
        if (player == null || rb == null) return;

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

        // アニメーション制御（移動速度連動）
        if (animator != null && rb != null)
        {
            // Rigidbodyの速度で移動を判定
            bool isMoving = rb.velocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
    }


    // ----------------------------------------------------
    // --- ヘルスとダメージ処理 ---
    // ----------------------------------------------------

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + "がダメージを受けました。残り体力: " + currentHealth);

        if (player == null) FindPlayerWithTag();

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (currentState == EnemyState.Idle)
        {
            TransitionToChase();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        Debug.Log(gameObject.name + "が倒れ、完全に停止します。");

        // 1. 爆発エフェクトの生成
        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // 2. アニメーションのトリガー
        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Die");
        }

        // 3. 全てのAI、ナビゲーション、発砲ロジックを強制停止

        // Invokeとコルーチンを全て停止
        CancelInvoke();
        StopAllCoroutines();

        // AI制御スクリプトを無効化
        if (aiA != null) aiA.enabled = false;
        if (aiB != null) aiB.enabled = false;
        if (aiOld != null) aiOld.enabled = false;
        this.enabled = false;

        // 5. 物理的な固定と衝突判定の無効化
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
    }

    // ----------------------------------------------------
    // --- ロジック関数 ---
    // ----------------------------------------------------

    void IdleLogic(float distance)
    {
        if (rb != null) rb.velocity = Vector3.zero; // アイドル中は移動停止
        if (distance <= sightRange)
        {
            TransitionToChase();
        }
    }

    void ChaseLogic(float distance)
    {
        // プレイヤーへの方向を計算
        Vector3 direction = (player.position - transform.position);

        // プレイヤーのY座標を無視して水平に回転
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        if (distance > attackRange)
        {
            // プレイヤーに向かって移動 (Rigidbody制御)
            if (rb != null)
            {
                // ここでrb.velocityのY成分は物理エンジンによって制御され続ける
                rb.velocity = transform.forward * moveSpeed;
            }
        }
        else
        {
            // 攻撃範囲内に入ったら停止
            if (rb != null) rb.velocity = Vector3.zero;
            TransitionToAttack();
        }

        if (distance > sightRange)
        {
            TransitionToIdle();
        }
    }

    void AttackLogic(float distance)
    {
        if (rb != null) rb.velocity = Vector3.zero; // 攻撃中は停止

        // プレイヤーのY座標を無視して水平に回転
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        if (distance > attackRange * 1.2f)
        {
            TransitionToChase();
        }
    }

    void ReloadLogic()
    {
        if (rb != null) rb.velocity = Vector3.zero; // リロード中は停止
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        if (isDead) return;
        currentState = EnemyState.Idle;

        if (rb != null) rb.velocity = Vector3.zero;

        CancelInvoke();
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    void TransitionToChase()
    {
        if (isDead) return;
        currentState = EnemyState.Chase;

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

        if (rb != null) rb.velocity = Vector3.zero;

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

        if (currentAmmo <= 0)
        {
            TransitionToReload();
        }
        else if (distance <= attackRange * 1.2f)
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

        if (rb != null) rb.velocity = Vector3.zero;

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
    // --- 弾丸生成処理 ---
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