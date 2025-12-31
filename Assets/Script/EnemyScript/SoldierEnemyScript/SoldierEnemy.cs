using UnityEngine;
using System.Collections; // コルーチンのために必要
using UnityEngine.UI;

public class SoliderEnemy : MonoBehaviour
{
    // ===================================
    // 1. 設定 & 状態
    // ===================================
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;
    private bool isDead = false;

    [Header("UI Settings")]
    public Slider healthSlider;        // Slider本体
    public GameObject healthBarCanvas; // HPバーのCanvas
    public Image healthBarFillImage;   // SliderのFill(中身)のImage
    public Gradient healthGradient;    // HPに応じた色の変化設定

    public enum EnemyState { Landing, Idle, Aiming, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing;

    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;
    private bool isReloading = false;

    public float sightRange = 40f;
    public float viewAngle = 120f;
    public float rotationSpeed = 3f;
    public float shootDuration = 1.0f;
    public int bulletsPerBurst = 1;
    public float timeBetweenShots = 0.1f;

    [Header("着地設定")]
    public float initialWaitTime = 1.0f;
    public float landingSpeed = 2.0f;
    public string groundTag = "Ground";

    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // ===================================
    // 2. コンポーネントと内部変数
    // ===================================
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private AudioSource audioSource;
    private Transform player; // 内部で自動取得

    private float nextRotationTime;
    private Quaternion targetIdleRotation;
    private bool isRotatingInIdle = false;

    // AI制御スクリプトへの参照 (Die()用)
    private EnemyAI aiA;
    private ChaserAI aiB;
    private JuggernautStaticAI aiOld;

    void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
            UpdateHealthBarColor();
        }

        // コンポーネント取得
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // 物理初期設定
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
        }

        // 外部AI参照取得
        aiA = GetComponent<EnemyAI>();
        aiB = GetComponent<ChaserAI>();
        aiOld = GetComponent<JuggernautStaticAI>();

        if (animator == null) Debug.LogError("Animatorがありません。");

        // --- PlayerをTagで取得 ---
        FindTargetPlayer();

        // 初期設定
        targetIdleRotation = transform.rotation;
        nextRotationTime = Time.time + Random.Range(3f, 6f);

        // 初期状態をLandingにし、着地処理を開始
        TransitionToLanding();
    }

    // プレイヤーを探す処理を独立（Updateでも再利用可能にする）
    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    // ===================================
    // 3. メインループ
    // ===================================

    void FixedUpdate()
    {
        if (isDead || player == null || rb == null) return;

        if (currentState == EnemyState.Landing)
        {
            LandingLogic();
        }
    }

    void Update()
    {
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        if (player == null)
        {
            FindTargetPlayer();
            return;
        }

        if (currentState == EnemyState.Landing) return;

        animator.SetFloat("Speed", 0f);

        bool playerFound = CheckForPlayer();

        // --- 状態ごとのロジック実行 ---
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleLogic(playerFound);
                break;
            case EnemyState.Aiming:
                AimingLogic(playerFound);
                break;
            case EnemyState.Attack:
                break;
            case EnemyState.Reload:
                break;
        }
    }

    // ===================================
    // 4. ダメージ・死亡処理 
    // ===================================

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            UpdateHealthBarColor();
        }

        if (currentHealth <= 0)
        {
            isDead = true;
            currentHealth = 0;

            // 全ロジックの即時強制停止
            CancelInvoke();
            StopAllCoroutines();
            this.enabled = false;

            if (animator != null) animator.enabled = false;
            if (aiA != null) aiA.enabled = false;
            if (aiB != null) aiB.enabled = false;
            if (aiOld != null) aiOld.enabled = false;

            if (rb != null) rb.isKinematic = true;
            if (enemyCollider != null) enemyCollider.enabled = false;

            Die();
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
        if (!isDead) return;

        Debug.Log(gameObject.name + "が停止しました。");

        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsAiming", false);
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsFloating", false);
            animator.SetTrigger("Die");
        }

        float animationDuration = 2.0f;
        StartCoroutine(DisableObjectAfterDie(animationDuration));
    }

    IEnumerator DisableObjectAfterDie(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // ===================================
    // 5. AIロジック関数
    // ===================================

    bool CheckForPlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > sightRange) return false;

        Vector3 horizontalForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 horizontalDirection = new Vector3(directionToPlayer.x, 0, directionToPlayer.z).normalized;

        float angle = Vector3.Angle(horizontalForward, horizontalDirection);
        if (angle > viewAngle / 2f) return false;

        RaycastHit hit;
        // 少し高い位置（目線）からレイを飛ばす
        Vector3 eyePosition = transform.position + Vector3.up * 1.0f;

        if (Physics.Raycast(eyePosition, directionToPlayer.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.DrawLine(eyePosition, hit.point, Color.red);
                return true;
            }
        }
        Debug.DrawLine(eyePosition, eyePosition + directionToPlayer.normalized * sightRange, Color.gray);
        return false;
    }

    void IdleLogic(bool playerFound)
    {
        if (playerFound)
        {
            TransitionToAiming();
            return;
        }

        if (Time.time > nextRotationTime)
        {
            nextRotationTime = Time.time + Random.Range(3f, 6f);
            targetIdleRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            isRotatingInIdle = true;
        }

        if (isRotatingInIdle)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetIdleRotation, Time.deltaTime * rotationSpeed * 0.5f);

            if (Quaternion.Angle(transform.rotation, targetIdleRotation) < 1.0f)
            {
                isRotatingInIdle = false;
            }
        }
    }

    // --- 5. AIロジック関数の修正 ---
    void AimingLogic(bool playerFound)
    {
        if (player == null) return;

        // 💡 プレイヤーの足元ではなく、胸の高さ（+1.2m程度）をターゲットにする
        Vector3 targetPoint = player.position + Vector3.up * 1.2f;
        Vector3 direction = (targetPoint - transform.position).normalized;

        // 回転は水平方向のみ（Y軸回転）に制限することで、体が傾くのを防ぐ
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float maxDegreesPerFrame = rotationSpeed * 60f * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, maxDegreesPerFrame);

        if (Quaternion.Angle(transform.rotation, lookRotation) < 5f)
        {
            TransitionToAttack();
            return;
        }

        if (!playerFound) TransitionToIdle();
    }

    void LandingLogic()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.down * landingSpeed;
    }

    // ===================================
    // 6. 状態遷移と発砲
    // ===================================

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
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsFloating", false);
        }

        if (player != null && CheckForPlayer())
        {
            TransitionToAiming();
        }
        else
        {
            TransitionToIdle();
        }
    }

    void TransitionToIdle()
    {
        if (isDead) return;

        CancelInvoke();
        currentState = EnemyState.Idle;
        animator.SetBool("IsAiming", false);

        isRotatingInIdle = false;
        nextRotationTime = Time.time + Random.Range(1f, 3f);
    }

    void TransitionToAiming()
    {
        if (isDead) return;

        currentState = EnemyState.Aiming;
        animator.SetBool("IsAiming", true);
        isRotatingInIdle = false;
    }

    void TransitionToAttack()
    {
        if (isDead) return;

        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        currentState = EnemyState.Attack;
        animator.SetTrigger("Shoot");

        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAiming");

        for (int i = 0; i < bulletsPerBurst; i++)
        {
            Invoke("ShootBullet", i * timeBetweenShots);
        }

        float totalBurstTime = (bulletsPerBurst - 1) * timeBetweenShots;
        Invoke("TransitionToAiming", totalBurstTime + shootDuration);
    }

    void StartReload()
    {
        if (isDead) return;

        isReloading = true;
        currentState = EnemyState.Reload;

        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAiming");

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

        isReloading = false;
        currentAmmo = maxAmmo;

        if (CheckForPlayer())
        {
            TransitionToAiming();
        }
        else
        {
            TransitionToIdle();
        }
    }

    // --- 6. 発砲関数の修正 ---
    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0 || player == null || muzzlePoint == null) return;

        currentAmmo--;
        if (bulletPrefab == null) return;

        // 💡 ターゲット位置をプレイヤーの中心（胸の高さ）に設定
        Vector3 targetPosition = player.position + Vector3.up * 1.2f;

        // 銃口からターゲットへの正確な方向を計算
        Vector3 directionToPlayer = (targetPosition - muzzlePoint.position).normalized;

        // 正確な方向を向くクォータニオンを作成
        // verticalAngleOffset (-5f) は削除し、計算された方向をそのまま使います
        Quaternion shootRotation = Quaternion.LookRotation(directionToPlayer);

        GameObject bulletInstance = Instantiate(bulletPrefab, muzzlePoint.position, shootRotation);
        bulletInstance.transform.parent = null;

        // デバッグ用：エディタのSceneビューで射線を確認
        Debug.DrawRay(muzzlePoint.position, directionToPlayer * 10f, Color.yellow, 0.5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == EnemyState.Landing && collision.gameObject.CompareTag(groundTag))
        {
            StopCoroutine("FinishLandingCoroutine");
            CancelInvoke("StartFalling");

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
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
}