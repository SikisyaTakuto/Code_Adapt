using UnityEngine;
using System.Collections;
using System;

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
    public enum EnemyState { Landing, Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing;

    // --- AI 設定 ---
    [SerializeField] private Transform player;
    public float sightRange = 15f;
    public float attackRange = 5f;
    public float rotationSpeed = 10f;
    public float moveSpeed = 6.0f;

    // 💡 追加: 着地設定
    [Header("着地設定")]
    public float initialWaitTime = 1.0f;  // 浮遊してから落下を開始するまでの待機時間
    public float landingSpeed = 2.0f;    // ゆっくり落下する速度
    public string groundTag = "Ground"; // 地面と判定するオブジェクトのタグ

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
        currentHealth = maxHealth;

        // --- コンポーネント取得 ---
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // 💡 変更点: 最初は空中待機のため、物理演算を無効にする
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
        }

        // --- 外部AI参照取得 (Die()用) ---
        aiA = GetComponent<EnemyAI>();
        aiB = GetComponent<ChaserAI>();
        aiOld = GetComponent<JuggernautStaticAI>();

        FindPlayerWithTag();

        // --- AI初期設定 ---
        currentAmmo = maxAmmo;
        TransitionToLanding();
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

    void FixedUpdate()
    {
        if (isDead) return;
        if (player == null || rb == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 💡 修正: Landing状態では、AI遷移や移動速度チェックをスキップする (LandingLogicのみ実行)
        if (currentState == EnemyState.Landing)
        {
            LandingLogic();
            // Landing状態では、以下のIdle/Chase/Attack判定をすべてスキップする。
            // 状態遷移は FinishLandingCoroutine の中で排他的に行われるべき。
            return;
        }

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
        // 💡 修正: Landing中はダメージを受けても状態遷移させない
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
            // 💡 修正: Floatingアニメーションもオフにする
            animator.SetBool("IsFloating", false);
            animator.SetTrigger("Die");
        }

        // 3. 全てのAI、ナビゲーション、発砲ロジックを強制停止
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
        if (rb != null) rb.velocity = Vector3.zero;
        if (distance <= sightRange)
        {
            TransitionToChase();
        }
    }

    void ChaseLogic(float distance)
    {
        Vector3 direction = (player.position - transform.position);
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        if (distance > attackRange)
        {
            if (rb != null)
            {
                rb.velocity = transform.forward * moveSpeed;
            }
        }
        else
        {
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
        if (rb != null) rb.velocity = Vector3.zero;

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
        if (rb != null) rb.velocity = Vector3.zero;
    }

    void LandingLogic()
    {
        if (rb == null) return;
        // 落下速度を制御 (ゆっくり)
        rb.velocity = Vector3.down * landingSpeed;
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 ---
    // ----------------------------------------------------

    void TransitionToLanding()
    {
        if (isDead) return;
        currentState = EnemyState.Landing;

        CancelInvoke();
        StopAllCoroutines();

        Invoke("StartFalling", initialWaitTime);

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsFloating", true);
        }
    }

    void StartFalling()
    {
        if (isDead) return;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }
    }

    // 💡 修正: 物理挙動安定化のためのコルーチン。コライダーの無効化/有効化を追加。
    IEnumerator FinishLandingCoroutine()
    {
        if (isDead) yield break;

        // 💡 物理エンジンがコライダー無効状態で位置調整を反映するのを待つ
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // 物理演算設定を通常AI動作に戻す
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // 💡 修正: コライダーを有効化して、AI通常動作に戻す
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsFloating", false);
        }

        // プレイヤーが近くにいればChase、いなければIdleへ
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= sightRange)
            {
                TransitionToChase();
                yield break;
            }
        }

        TransitionToIdle();
    }

    void TransitionToIdle()
    {
        if (isDead) return;
        currentState = EnemyState.Idle;

        if (rb != null) rb.velocity = Vector3.zero;

        CancelInvoke();
        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
        }
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
    // --- 確実な着地判定 (衝突判定) ---
    // ----------------------------------------------------

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == EnemyState.Landing && collision.gameObject.CompareTag(groundTag))
        {
            // 💡 衝突したら既存の落下処理を停止
            StopCoroutine("FinishLandingCoroutine");
            CancelInvoke("StartFalling");

            // 落下速度をリセットし、地面にめり込まないように位置調整
            if (rb != null)
            {
                rb.velocity = Vector3.zero;

                float contactY = collision.contacts[0].point.y;

                if (enemyCollider != null)
                {
                    // 💡 修正: 衝突判定を無効化して、位置調整中の物理干渉を防ぐ
                    enemyCollider.enabled = false;
                    // 衝突した地面のY座標 + 自分のコライダー半分の高さ
                    transform.position = new Vector3(transform.position.x, contactY + enemyCollider.bounds.extents.y, transform.position.z);
                }
            }

            // 着地完了処理をコルーチンで呼び出す
            StartCoroutine(FinishLandingCoroutine());
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
    