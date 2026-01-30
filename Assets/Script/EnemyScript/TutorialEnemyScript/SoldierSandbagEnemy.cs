using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.UI;

public class SoldierSandbagEnemy : MonoBehaviour
{
    // ====================================================================
    // --- 1. サンドバック設定 (修正) ---
    // ====================================================================
    [Header("Sandbag Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;
    private float totalDamageTaken = 0f;

    [Header("UI Settings")]
    public Slider healthSlider;
    public GameObject healthBarCanvas;
    public Image healthBarFillImage;
    public Gradient healthGradient;

    // サンドバックなので死亡フラグは常にfalse
    private bool isDead = false;
    private bool isJumping = false;

    // ====================================================================
    // --- 2. AI 状態定義 ---
    // ====================================================================
    public enum EnemyState { Landing, Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing;

    private Transform player;
    public float sightRange = 30f;
    public float attackRange = 15f;
    public float rotationSpeed = 10f;
    public float moveSpeed = 6.0f;

    [Header("Movement Settings")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.5f;
    public float dashJumpDistanceMultiplier = 1.5f;
    public float dashJumpHeightMultiplier = 1.3f;
    public float initialWaitTime = 1.0f;
    public string groundTag = "Ground";

    [Header("Attack Settings")]
    public int bulletsPerBurst = 3;
    public float timeBetweenShots = 0.1f;
    public float shootDuration = 0.5f;
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;

    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    [Header("Visual Feedback")]
    [SerializeField] private Color damageFlashColor = Color.red;
    private Color originalColor;
    private Renderer enemyRenderer;
    private Coroutine flashCoroutine;
    private Renderer[] enemyRenderers; // 配列に変更
    private Color[] originalColors;    // 各パーツの元の色を保存

    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private NavMeshAgent agent;

    void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        agent = GetComponent<NavMeshAgent>();
        enemyRenderers = GetComponentsInChildren<Renderer>();

        // 元の色をすべて保存しておく
        originalColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            // マテリアルが存在する場合のみ色を保存
            if (enemyRenderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = enemyRenderers[i].material.color;
            }
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
            UpdateHealthBarColor();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.freezeRotation = true;
        }

        FindPlayerWithTag();
        TransitionToLanding();
    }

    private void FindPlayerWithTag()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    void Update()
    {
        if (healthBarCanvas != null && Camera.main != null)
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
    }

    void FixedUpdate()
    {
        if (isJumping || rb == null) return;
        if (player == null) { FindPlayerWithTag(); return; }
        if (currentState == EnemyState.Landing) return;

        // NavMesh Jump Logic
        if (agent != null && agent.enabled && agent.isOnOffMeshLink)
        {
            StartCoroutine(ProcessOffMeshLink());
            return;
        }

        // Agent Move Logic
        if (agent != null && agent.enabled && (currentState == EnemyState.Chase || currentState == EnemyState.Idle))
        {
            agent.isStopped = (currentState == EnemyState.Idle);
            if (currentState == EnemyState.Chase) agent.SetDestination(player.position);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle: IdleLogic(distanceToPlayer); break;
            case EnemyState.Chase: ChaseLogic(distanceToPlayer); break;
            case EnemyState.Attack: AttackLogic(distanceToPlayer); break;
            case EnemyState.Reload: ReloadLogic(); break;
        }

        if (animator != null)
        {
            bool isMoving = (agent != null && agent.enabled) ? agent.velocity.magnitude > 0.1f : rb.linearVelocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
    }

    // ====================================================================
    // --- 3. ダメージ処理 (サンドバック化の核) ---
    // ====================================================================

    public void TakeDamage(float damage)
    {
        totalDamageTaken += damage;
        currentHealth = maxHealth; // サンドバッグなので即回復

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            UpdateHealthBarColor();
        }

        // 全身を点滅させる
        if (enemyRenderers != null && enemyRenderers.Length > 0)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashEffect());
        }

        if (currentState == EnemyState.Idle) TransitionToChase();
    }

    private IEnumerator FlashEffect()
    {
        // 1. 全パーツを赤くする
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null)
                enemyRenderers[i].material.color = damageFlashColor;
        }

        yield return new WaitForSeconds(0.1f);

        // 2. 全パーツを元の色に戻す
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null)
                enemyRenderers[i].material.color = originalColors[i];
        }
    }

    private void UpdateHealthBarColor()
    {
        if (healthBarFillImage != null && healthSlider != null)
            healthBarFillImage.color = healthGradient.Evaluate(currentHealth / maxHealth);
    }

    // ====================================================================
    // --- 4. AI ロジック (基本は元のコードを継承) ---
    // ====================================================================

    void IdleLogic(float distance) { if (distance <= sightRange) TransitionToChase(); }

    void ChaseLogic(float distance)
    {
        Vector3 direction = (player.position - transform.position);
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        if (distance <= attackRange) TransitionToAttack();
        else if (distance > sightRange) TransitionToIdle();
    }

    void AttackLogic(float distance)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);
        if (distance > attackRange * 1.2f) TransitionToChase();
    }

    void ReloadLogic() { }

    // --- Transitions ---
    void TransitionToIdle() { currentState = EnemyState.Idle; if (agent.enabled) agent.isStopped = true; animator.SetBool("IsAiming", false); }
    void TransitionToChase() { currentState = EnemyState.Chase; animator.SetBool("IsAiming", false); if (agent.enabled) agent.isStopped = false; }
    void TransitionToAttack()
    {
        if (currentAmmo <= 0) { TransitionToReload(); return; }
        currentState = EnemyState.Attack;
        if (agent.enabled) agent.isStopped = true;
        animator.SetBool("IsAiming", true);
        animator.SetTrigger("Shoot");

        for (int i = 0; i < bulletsPerBurst; i++) Invoke("ShootBullet", i * timeBetweenShots);
        Invoke("TransitionToAttackComplete", (bulletsPerBurst - 1) * timeBetweenShots + shootDuration);
    }

    void TransitionToAttackComplete() { if (currentAmmo <= 0) TransitionToReload(); else TransitionToAttack(); }
    void TransitionToReload() { currentState = EnemyState.Reload; animator.SetBool("IsAiming", false); animator.SetTrigger("Reload"); Invoke("FinishReload", reloadTime); }
    void FinishReload() { currentAmmo = maxAmmo; TransitionToChase(); }

    public void ShootBullet()
    {
        if (currentAmmo <= 0 || player == null || muzzlePoint == null || bulletPrefab == null) return;
        currentAmmo--;
        Vector3 directionToPlayer = (player.position - muzzlePoint.position).normalized;
        Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(directionToPlayer));
    }

    // --- Landing & Jumping ---
    void TransitionToLanding() { currentState = EnemyState.Landing; agent.enabled = false; animator.SetBool("IsFloating", true); Invoke("StartFalling", initialWaitTime); }
    void StartFalling() { rb.useGravity = true; }
    private void OnCollisionEnter(Collision collision) { if (currentState == EnemyState.Landing && collision.gameObject.CompareTag(groundTag)) StartCoroutine(FinishLanding()); }
    IEnumerator FinishLanding() { yield return new WaitForSeconds(0.05f); agent.enabled = true; animator.SetBool("IsFloating", false); TransitionToIdle(); }

    IEnumerator ProcessOffMeshLink()
    {
        isJumping = true;
        agent.isStopped = true; agent.updatePosition = false;
        animator.SetTrigger("Jump");
        Vector3 startPos = agent.currentOffMeshLinkData.startPos;
        Vector3 endPos = agent.currentOffMeshLinkData.endPos;
        float timer = 0f;
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;
            transform.position = Vector3.Lerp(startPos, endPos, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            yield return null;
        }
        agent.CompleteOffMeshLink(); agent.updatePosition = true; agent.isStopped = false;
        isJumping = false;
    }
}