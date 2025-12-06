using UnityEngine;

public class SoldierEnemy : MonoBehaviour
{
    // ===================================
    // 1. 設定 & 状態
    // ===================================
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;
    private bool isDead = false;

    public enum EnemyState { Idle, Idle_Shoot, Attack }
    public EnemyState currentState = EnemyState.Idle;

    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;
    private bool isReloading = false;

    public float sightRange = 15f;
    public float viewAngle = 90f;
    public float rotationSpeed = 3f;
    public float shootDuration = 1.0f;
    public int bulletsPerBurst = 1;
    public float timeBetweenShots = 0.1f;

    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // ===================================
    // 2. コンポーネント
    // ===================================
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    private AudioSource audioSource;
    private Transform player;
    private float nextRotationTime;

    // AI制御スクリプトへの参照
    private EnemyAI aiA;
    private ChaserAI aiB;
    private JuggernautStaticAI aiOld;


    void Start()
    {
        currentHealth = maxHealth;

        // コンポーネント取得
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // 外部AI参照取得
        aiA = GetComponent<EnemyAI>();
        aiB = GetComponent<ChaserAI>();
        aiOld = GetComponent<JuggernautStaticAI>();

        if (animator == null) Debug.LogError("Animatorがありません。");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Playerタグのオブジェクトが見つかりません。");
        }

        currentState = EnemyState.Idle;
        currentAmmo = maxAmmo;
        nextRotationTime = Time.time + Random.Range(3f, 6f);
    }

    // ===================================
    // 3. メインループ
    // ===================================

    void FixedUpdate()
    {
        if (player == null || animator == null || isReloading || isDead) return;

        if (currentState == EnemyState.Idle_Shoot)
        {
            Idle_ShootLogic(CheckForPlayer());
        }
    }

    void Update()
    {
        if (isDead) return;

        if (player == null || animator == null || isReloading) return;
        animator.SetFloat("Speed", 0f);

        bool playerFound = CheckForPlayer();

        if (currentState == EnemyState.Idle)
        {
            IdleLogic(playerFound);
        }
    }

    // ===================================
    // 4. ダメージ・死亡処理
    // ===================================

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + "ダメージ: " + currentHealth);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        Debug.Log(gameObject.name + "が停止しました。");

        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // アニメーション
        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Die");
        }

        // AIの強制停止
        CancelInvoke();
        StopAllCoroutines();

        if (aiA != null) aiA.enabled = false;
        if (aiB != null) aiB.enabled = false;
        if (aiOld != null) aiOld.enabled = false;
        this.enabled = false;

        // 物理的な固定
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // 衝突判定の無効化
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // if (audioSource != null && audioSource.isPlaying) audioSource.Stop(); 
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

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > viewAngle / 2f) return false;

        RaycastHit hit;
        Vector3 eyePosition = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(eyePosition, directionToPlayer.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.DrawLine(eyePosition, hit.point, Color.red);
                return true;
            }
        }
        return false;
    }

    void IdleLogic(bool playerFound)
    {
        if (playerFound)
        {
            TransitionToIdle_Shoot();
            return;
        }

        if (Time.time > nextRotationTime)
        {
            nextRotationTime = Time.time + Random.Range(3f, 6f);
            Quaternion targetRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void Idle_ShootLogic(bool playerFound)
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float maxDegreesPerFrame = rotationSpeed * 30f * Time.fixedDeltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, maxDegreesPerFrame);

        if (Quaternion.Angle(transform.rotation, lookRotation) < 15f)
        {
            TransitionToAttack();
        }

        if (!playerFound)
        {
            TransitionToIdle();
        }
    }

    // ===================================
    // 6. 状態遷移と発砲
    // ===================================

    void TransitionToIdle()
    {
        if (isDead) return;

        CancelInvoke();
        currentState = EnemyState.Idle;
        animator.SetBool("IsAiming", false);
        nextRotationTime = Time.time + Random.Range(3f, 6f);
    }

    void TransitionToIdle_Shoot()
    {
        if (isDead) return;

        currentState = EnemyState.Idle_Shoot;
        animator.SetBool("IsAiming", true);
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
        CancelInvoke("TransitionToIdle_Shoot");

        for (int i = 0; i < bulletsPerBurst; i++)
        {
            Invoke("ShootBullet", i * timeBetweenShots);
        }

        float totalBurstTime = (bulletsPerBurst - 1) * timeBetweenShots;
        Invoke("TransitionToIdle_Shoot", totalBurstTime + shootDuration);
    }

    void StartReload()
    {
        if (isDead) return;

        isReloading = true;
        Debug.Log("リロード開始...");

        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToIdle_Shoot");

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
        Debug.Log("リロード完了！");

        if (CheckForPlayer())
        {
            TransitionToIdle_Shoot();
        }
        else
        {
            TransitionToIdle();
        }
    }

    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0) return;

        currentAmmo--;

        if (bulletPrefab == null || muzzlePoint == null)
        {
            Debug.LogError("弾丸プレハブまたは銃口が未設定です！");
            return;
        }

        Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation).transform.parent = null;
        Debug.Log("弾が生成されました！");
    }
}