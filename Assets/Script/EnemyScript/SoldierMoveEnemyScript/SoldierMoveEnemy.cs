using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;

public class SoldierMoveEnemy : MonoBehaviour
{
    // ====================================================================
    // --- 1. ヘルスと死亡設定 ---
    // ====================================================================
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;

    private bool isDead = false;
    private bool isJumping = false;
    private const float ANIMATION_DEATH_DELAY = 2.0f;

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

    // 💡 ジャンプ設定
    [Header("ジャンプ設定")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.5f;

    // 💡 新規追加: ダッシュジャンプ設定
    [Header("ダッシュジャンプ設定")]
    public float dashJumpDistanceMultiplier = 1.5f; // ジャンプの水平速度の倍率 (大きいほど速くリンクを渡る)
    public float dashJumpHeightMultiplier = 1.3f;    // ジャンプの高さの倍率 (大きいほど高く飛ぶ)


    // 💡 追加: 着地設定
    [Header("着地設定")]
    public float initialWaitTime = 1.0f;
    public float landingSpeed = 2.0f;
    public string groundTag = "Ground";

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
    private NavMeshAgent agent;

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
        agent = GetComponent<NavMeshAgent>();

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
        if (isDead || isJumping) return;
        if (player == null || rb == null) return;

        // NavMeshAgent が OffMeshLink 上にいる場合、ジャンプ処理へ移行
        if (agent != null && agent.isOnOffMeshLink)
        {
            // 💡 (1) 追跡/攻撃状態でのみジャンプするか？
            if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
            {
                agent.isStopped = true;
                return;
            }

            // 💡 (2) コルーチンが起動
            Debug.Log("Jumping Started!");
            StartCoroutine(ProcessOffMeshLink());
            return; // AIロジックを無視
        }

        // NavMeshAgent による移動制御
        if (agent != null && (currentState == EnemyState.Chase || currentState == EnemyState.Idle))
        {
            agent.isStopped = (currentState == EnemyState.Idle);
            if (currentState == EnemyState.Chase)
            {
                agent.SetDestination(player.position);
            }
        }


        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (currentState == EnemyState.Landing)
        {
            LandingLogic();
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

        // アニメーション制御
        if (animator != null && rb != null)
        {
            bool isMoving = (agent != null) ? agent.velocity.magnitude > 0.1f : rb.velocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
    }


    // ----------------------------------------------------
    // --- ヘルスとダメージ処理 (省略) ---
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

        Debug.Log(gameObject.name + "が倒れ、アニメーション後に破棄されます。");

        if (animator != null)
        {
            if (!animator.enabled) animator.enabled = true;
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsFloating", false);
            animator.SetTrigger("Die");
        }

        // AIロジック、Agent、物理挙動を強制停止
        CancelInvoke();
        StopAllCoroutines();

        if (aiA != null) aiA.enabled = false;
        if (aiB != null) aiB.enabled = false;
        if (aiOld != null) aiOld.enabled = false;
        this.enabled = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        StartCoroutine(DestroyAfterDelay(ANIMATION_DEATH_DELAY, animator));
    }

    /// <summary>
    /// 遅延後にAnimatorを停止し、エフェクトを生成し、オブジェクトを削除するコルーチン
    /// </summary>
    IEnumerator DestroyAfterDelay(float delay, Animator anim)
    {
        yield return new WaitForSeconds(delay);

        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);

            explosion.transform.localScale = Vector3.one * 2f;

            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play(true);
            }
        }

        if (anim != null)
        {
            anim.enabled = false;
        }

        Destroy(gameObject);
    }

    // ----------------------------------------------------
    // --- ダッシュジャンプ実行コルーチン ---
    // ----------------------------------------------------

    IEnumerator ProcessOffMeshLink()
    {
        if (isJumping) yield break;
        isJumping = true;

        // NavMesh Agentを停止し、物理挙動をリセット
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updatePosition = false; // 💡 追加
            agent.updateRotation = false; // 💡 追加
        }
        if (rb != null) rb.velocity = Vector3.zero;

        // 1. ジャンプアニメーションのトリガー
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        // 2. オフメッシュリンクの始点と終点を取得
        Vector3 startPos = agent.currentOffMeshLinkData.startPos;
        Vector3 endPos = agent.currentOffMeshLinkData.endPos;

        // 💡 3. ダッシュジャンプの速度/高さパラメータを適用
        float actualJumpHeight = jumpHeight * dashJumpHeightMultiplier;
        float actualJumpDuration = jumpDuration / dashJumpDistanceMultiplier; // 短縮 = 水平速度アップ

        float timer = 0f;

        // 4. リンクに沿ってダッシュジャンプ移動 (放物線)
        while (timer < actualJumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / actualJumpDuration;

            float height = Mathf.Sin(progress * Mathf.PI) * actualJumpHeight;

            // 💡 ログを追加 (数フレームに一度でOK)
            // if (Time.frameCount % 5 == 0) Debug.Log("Jump Progress: " + progress.ToString("F2"));

            transform.position = Vector3.Lerp(startPos, endPos, progress) + Vector3.up * height;
            yield return null;
        }

        // 💡 ジャンプが完了したことを確認
        Debug.Log("Jump Finished. Completing Link.");

        // 5. ジャンプ完了後、NavMesh Agentの移動を完了させる
        if (agent != null)
        {
            agent.CompleteOffMeshLink();

            // 💡 再びNavMesh Agentに制御を戻す
            agent.updatePosition = true; // 💡 戻す
            agent.updateRotation = true; // 💡 戻す
            agent.isStopped = false;
        }

        isJumping = false; // ジャンプ終了
    }


    // ----------------------------------------------------
    // --- ロジック関数 (ChaseLogic, TransitionToChase など) (省略) ---
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
        if (agent != null)
        {
            Vector3 direction = (player.position - transform.position);
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);
        }
        else
        {
            Vector3 direction = (player.position - transform.position);
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);
            if (distance > attackRange)
            {
                if (rb != null) rb.velocity = transform.forward * moveSpeed;
            }
        }

        if (distance <= attackRange)
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
        rb.velocity = Vector3.down * landingSpeed;
    }

    void TransitionToLanding()
    {
        if (isDead) return;
        currentState = EnemyState.Landing;

        CancelInvoke();
        StopAllCoroutines();

        if (agent != null) agent.enabled = false;

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

    IEnumerator FinishLandingCoroutine()
    {
        if (isDead) yield break;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (agent != null)
        {
            agent.enabled = true;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsFloating", false);
        }

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
        if (agent != null) agent.isStopped = true;

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

        if (agent != null) agent.isStopped = false;
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
        if (agent != null) agent.isStopped = true;

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
        if (agent != null) agent.isStopped = true;

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

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == EnemyState.Landing && collision.gameObject.CompareTag(groundTag))
        {
            StopCoroutine("FinishLandingCoroutine");
            CancelInvoke("StartFalling");

            if (rb != null)
            {
                rb.velocity = Vector3.zero;

                float contactY = collision.contacts[0].point.y;

                if (enemyCollider != null)
                {
                    enemyCollider.enabled = false;
                    transform.position = new Vector3(transform.position.x, contactY + enemyCollider.bounds.extents.y, transform.position.z);
                }
            }

            StartCoroutine(FinishLandingCoroutine());
        }
    }
    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0 || player == null || muzzlePoint == null) return;

        currentAmmo--;

        if (bulletPrefab == null)
        {
            return;
        }

        // 1. 🎯 プレイヤーへの正確な方向ベクトルを取得 (Y軸を含む)
        //    プレイヤーがどこにいても、その中心点を狙います。
        Vector3 targetPosition = player.position;
        Vector3 directionToPlayer = (targetPosition - muzzlePoint.position).normalized;

        // 2. プレイヤーを直接向くための基準回転を取得
        //    LookRotation(directionToPlayer) は、Y軸を含むプレイヤーへの正確な回転を求めます。
        Quaternion baseRotation = Quaternion.LookRotation(directionToPlayer);

        // 3. 垂直方向の角度調整 (下向きの放物線オフセット) を加える
        //    プレイヤーを狙った回転に対して、さらにX軸周りに -5度回転させ、弾が放物線を描くようにする。
        //    これにより、プレイヤーがジャンプしても、狙いは外れず、放物線効果が維持されます。
        float verticalAngleOffset = -5f;
        Quaternion adjustedRotation = baseRotation * Quaternion.Euler(verticalAngleOffset, 0, 0);

        // 4. 調整された回転で弾を生成
        GameObject bulletInstance = Instantiate(bulletPrefab, muzzlePoint.position, adjustedRotation);
        bulletInstance.transform.parent = null;

        // 弾が重力と初速で放物線を描くのは、Bullet.csのRigidbodyへの速度設定に依存します。
    }
}