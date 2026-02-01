using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;
using UnityEngine.UI;

public class SoldierMoveEnemy : MonoBehaviour
{
    // ====================================================================
    // --- 1. ヘルスと死亡設定 ---
    // ====================================================================
    public float maxHealth = 1000f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;

    [Header("UI Settings")]
    public Slider healthSlider;        // Slider本体
    public GameObject healthBarCanvas; // HPバーのCanvas
    public Image healthBarFillImage;   // SliderのFill(中身)のImage
    public Gradient healthGradient;    // HPに応じた色の変化設定

    private bool isDead = false;
    private bool isJumping = false;
    private const float ANIMATION_DEATH_DELAY = 2.0f;

    // ====================================================================
    // --- 2. AI 状態定義と設定 ---
    // ====================================================================
    public enum EnemyState { Landing, Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing;

    // --- AI 設定 ---
    private Transform player; // 💡 Tagで自動取得するためprivateに変更
    public float sightRange = 30f;
    public float attackRange = 15f;
    public float rotationSpeed = 10f;
    public float moveSpeed = 6.0f;

    [Header("ジャンプ設定")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.5f;

    [Header("ダッシュジャンプ設定")]
    public float dashJumpDistanceMultiplier = 1.5f;
    public float dashJumpHeightMultiplier = 1.3f;

    [Header("着地設定")]
    public float initialWaitTime = 1.0f;
    public float landingSpeed =50.0f;
    public string groundTag = "Ground";

    // --- 攻撃設定 ---
    public int bulletsPerBurst = 3;
    public float timeBetweenShots = 0.1f;
    public float shootDuration = 0.5f;

    // --- 弾薬とリロード設定 ---
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;

    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    //[Header("オーディオ設定")]
    //public AudioClip shootSound; // ★サブマシンガンの音をアサインしてください
    // ====================================================================
    // --- 3. コンポーネントと初期化 ---
    // ====================================================================
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private AudioSource audioSource;
    private NavMeshAgent agent;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
            UpdateHealthBarColor();
        }

        // --- コンポーネント取得 ---
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();

        if (rb != null)
        {
            rb.isKinematic = false; 
            rb.useGravity = true;
            rb.freezeRotation = true;
        }

        // 💡 起動時にプレイヤーを検索
        FindPlayerWithTag();

        currentAmmo = maxAmmo;
        TransitionToLanding();
    }

    /// <summary>
    /// Tag "Player" を持つオブジェクトを検索し、参照を設定します。
    /// </summary>
    private void FindPlayerWithTag()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            // デバッグログは頻出しすぎないよう注意
            Debug.LogWarning(gameObject.name + ": Playerタグが見つかりません。");
        }
    }

    void Update()
    {
        // --- ビルボード処理: HPバーを常にカメラに向ける (追加) ---
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        // 死亡時は以下のロジックをスキップ
        if (isDead) return;
    }

    // ====================================================================
    // --- 4. メインループ (AIロジック) ---
    // ====================================================================

    void FixedUpdate()
    {
        if (isDead || isJumping || rb == null) return;

        // 💡 プレイヤーがいない場合は検索し、見つからなければロジックをスキップ
        if (player == null)
        {
            FindPlayerWithTag();
            if (player == null) return;
        }

        if (currentState == EnemyState.Landing)
        {
            return; // LandingLogic() は呼び出さない
        }

        // NavMeshAgent が OffMeshLink 上にいる場合、ジャンプ処理へ移行
        if (agent != null && agent.enabled && agent.isOnOffMeshLink)
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                Debug.Log("Jumping Started!");
                StartCoroutine(ProcessOffMeshLink());
                return;
            }
            else
            {
                agent.isStopped = true;
                return;
            }
        }

        // NavMeshAgent による移動制御
        if (agent != null && agent.enabled && (currentState == EnemyState.Chase || currentState == EnemyState.Idle))
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
        if (animator != null)
        {
            bool isMoving = (agent != null && agent.enabled) ? agent.velocity.magnitude > 0.1f : rb.linearVelocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
    }

    // ====================================================================
    // --- 5. ダメージ・死亡処理 ---
    // ====================================================================

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            UpdateHealthBarColor();
        }
        // ダメージを受けた際にプレイヤーがいなければ即座に検索
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

    private void UpdateHealthBarColor()
    {
        if (healthBarFillImage != null && healthSlider != null)
        {
            float healthRatio = currentHealth / maxHealth;
            healthBarFillImage.color = healthGradient.Evaluate(healthRatio);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);

        if (animator != null)
        {
            if (!animator.enabled) animator.enabled = true;
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsFloating", false);
            animator.SetTrigger("Die");
        }

        if (animator != null)
        {
            if (!animator.enabled) animator.enabled = true;
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsFloating", false);
            animator.SetTrigger("Die");
        }

        CancelInvoke();
        StopAllCoroutines();

        if (agent != null)
        {
            if (agent.enabled) agent.isStopped = true;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (enemyCollider != null) enemyCollider.enabled = false;

        StartCoroutine(DestroyAfterDelay(ANIMATION_DEATH_DELAY, animator));
        this.enabled = false;
    }

    IEnumerator DestroyAfterDelay(float delay, Animator anim)
    {
        yield return new WaitForSeconds(delay);

        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * 2f;
        }

        if (anim != null) anim.enabled = false;
        Destroy(gameObject);
    }

    // ====================================================================
    // --- 6. 移動・ジャンプロジック ---
    // ====================================================================

    IEnumerator ProcessOffMeshLink()
    {
        if (isJumping) yield break;
        isJumping = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (animator != null) animator.SetTrigger("Jump");

        Vector3 startPos = agent.currentOffMeshLinkData.startPos;
        Vector3 endPos = agent.currentOffMeshLinkData.endPos;

        float actualJumpHeight = jumpHeight * dashJumpHeightMultiplier;
        float actualJumpDuration = jumpDuration / dashJumpDistanceMultiplier;

        float timer = 0f;
        while (timer < actualJumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / actualJumpDuration;
            float height = Mathf.Sin(progress * Mathf.PI) * actualJumpHeight;

            transform.position = Vector3.Lerp(startPos, endPos, progress) + Vector3.up * height;
            yield return null;
        }

        if (agent != null && agent.enabled)
        {
            agent.CompleteOffMeshLink();
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        isJumping = false;
    }

    // 視線が通っているかチェックするメソッド
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        // 敵の目線の高さ（適宜調整）からプレイヤーの腰あたりへ線を飛ばす
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 targetPosition = player.position + Vector3.up * 1.0f;

        RaycastHit hit;
        // Linecastは2点間に障害物があるか判定する
        if (Physics.Linecast(eyePosition, targetPosition, out hit))
        {
            // 最初に当たったのがPlayerタグなら「見える」と判断
            if (hit.transform.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    void IdleLogic(float distance)
    {
        if (rb != null) rb.linearVelocity = Vector3.zero;

        // 距離が近く、かつ「目で見える」場合のみ追いかける
        if (distance <= sightRange && CanSeePlayer())
        {
            TransitionToChase();
        }
    }

    void ChaseLogic(float distance)
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position);
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        if (agent == null || !agent.enabled)
        {
            if (distance > attackRange)
            {
                if (rb != null) rb.linearVelocity = transform.forward * moveSpeed;
            }
        }

        // 攻撃範囲内かつ「目で見える」なら攻撃へ
        if (distance <= attackRange && CanSeePlayer())
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            TransitionToAttack();
        }
        // 視界から消えた、もしくは離れすぎた場合はIdleへ（または数秒後に見失う処理など）
        else if (distance > sightRange || !CanSeePlayer())
        {
            TransitionToIdle();
        }
    }

    void AttackLogic(float distance)
    {
        if (player == null) return;
        if (rb != null) rb.linearVelocity = Vector3.zero;

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
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    void LandingLogic()
    {
        // 何もしない。RigidbodyのuseGravity = true によって自然に落下します。
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
            rb.useGravity = true; // ★重力を有効にする
            // rb.linearVelocity = Vector3.zero; // 必要に応じて初期速度をリセット
        }
    }

    IEnumerator FinishLandingCoroutine()
    {
        if (isDead) yield break;

        // すでに StartFalling で true になっていますが、念のため
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(0.05f);

        if (agent != null) agent.enabled = true;
        if (animator != null) animator.SetBool("IsFloating", false);

        // 状態遷移
        if (player != null && Vector3.Distance(transform.position, player.position) <= sightRange)
            TransitionToChase();
        else
            TransitionToIdle();
    }

    // ====================================================================
    // --- 7. 状態遷移と攻撃 ---
    // ====================================================================

    void TransitionToIdle()
    {
        if (isDead) return;
        currentState = EnemyState.Idle;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        if (agent != null && agent.enabled) agent.isStopped = true;
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    void TransitionToChase()
    {
        if (isDead) return;
        currentState = EnemyState.Chase;
        CancelInvoke("ShootBullet");
        if (animator != null) animator.SetBool("IsAiming", false);
        if (agent != null && agent.enabled) agent.isStopped = false;
    }

    void TransitionToAttack()
    {
        if (isDead) return;
        if (currentAmmo <= 0) { TransitionToReload(); return; }

        currentState = EnemyState.Attack;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        if (agent != null && agent.enabled) agent.isStopped = true;

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

        float totalAttackTime = (bulletsPerBurst - 1) * timeBetweenShots + shootDuration;
        Invoke("TransitionToAttackComplete", totalAttackTime);
    }

    void TransitionToAttackComplete()
    {
        if (isDead || player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);

        // 距離だけでなく、視線が通っているか (CanSeePlayer) を条件に追加
        if (currentAmmo <= 0)
        {
            TransitionToReload();
        }
        else if (distance <= attackRange * 1.2f && CanSeePlayer())
        {
            TransitionToAttack();
        }
        else
        {
            // 見えない、または遠すぎるなら追いかける
            TransitionToChase();
        }
    }

    void TransitionToReload()
    {
        if (isDead) return;
        currentState = EnemyState.Reload;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        if (agent != null && agent.enabled) agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetTrigger("Reload");
        }
        Invoke("FinishReload", reloadTime);
    }

    void FinishReload()
    {
        if (isDead) return;
        currentAmmo = maxAmmo;
        if (player == null) { TransitionToIdle(); return; }

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange * 1.2f) TransitionToAttack();
        else if (distance <= sightRange) TransitionToChase();
        else TransitionToIdle();
    }

    public void ShootBullet()
    {
        // CanSeePlayer() のチェックを追加
        if (isDead || currentAmmo <= 0 || player == null || muzzlePoint == null || bulletPrefab == null || !CanSeePlayer())
            return;

        currentAmmo--;

        //// --- 音の再生を追加 ---
        //if (audioSource != null && shootSound != null)
        //{
        //    // PlayOneShotを使うことで、連射しても音が途切れず重なって再生されます
        //    audioSource.PlayOneShot(shootSound);
        //}

        Vector3 targetPosition = player.position;
        Vector3 directionToPlayer = (targetPosition - muzzlePoint.position).normalized;
        Quaternion baseRotation = Quaternion.LookRotation(directionToPlayer);
        Quaternion adjustedRotation = baseRotation * Quaternion.Euler(-5f, 0, 0);

        Instantiate(bulletPrefab, muzzlePoint.position, adjustedRotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Groundタグに触れたら「敵の状態」をLandingからIdle/Chaseに変えるだけ
        if (currentState == EnemyState.Landing && collision.gameObject.CompareTag(groundTag))
        {
            // rb.isKinematic = true; ← これを消すことで、重力が効き続けます
            StartCoroutine(FinishLandingCoroutine());
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (player != null)
        {
            // 視線が通っているなら緑、通っていないなら赤で線を表示
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up * 1.0f);
        }
    }
}