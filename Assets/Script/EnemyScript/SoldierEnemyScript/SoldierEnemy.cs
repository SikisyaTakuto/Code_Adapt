using UnityEngine;
using System.Collections; // コルーチンのために必要

public class SoliderEnemy : MonoBehaviour
{
    // ===================================
    // 1. 設定 & 状態
    // ===================================
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;
    private bool isDead = false;

    public enum EnemyState { Landing, Idle, Aiming, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing;

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
    private Transform player;

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

        // コンポーネント取得
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // 物理初期設定: Landing処理のため、最初は物理演算を無効化
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
        }

        // 外部AI参照取得 (現状のコードに合わせて残す)
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

        // 初期設定
        targetIdleRotation = transform.rotation;
        nextRotationTime = Time.time + Random.Range(3f, 6f);

        // 初期状態をLandingにし、着地処理を開始
        TransitionToLanding();
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
        if (isDead || player == null || animator == null || isReloading) return;

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
        Debug.Log(gameObject.name + "ダメージ: " + currentHealth);

        if (currentHealth <= 0)
        {
            // 💡 修正: 死亡フラグとHPを即座に設定
            isDead = true;
            currentHealth = 0;

            // ===============================================
            // 💥 最重要: 全ロジックの即時強制停止
            // ===============================================

            // 1. タイマー・コルーチンの停止
            CancelInvoke();
            StopAllCoroutines();

            // 2. スクリプト駆動の停止
            this.enabled = false;

            // 3. アニメーター駆動の停止 (アニメーションイベントもブロック)
            if (animator != null) animator.enabled = false;

            // 4. 外部AIスクリプトの停止 (念のため)
            if (aiA != null) aiA.enabled = false;
            if (aiB != null) aiB.enabled = false;

            // 5. 物理とコライダーの無効化
            if (rb != null) rb.isKinematic = true;
            if (enemyCollider != null) enemyCollider.enabled = false;

            // ===============================================

            // アニメーション再生とオブジェクト無効化の処理に移る
            Die();
        }
    }

    void Die()
    {
        // 💡 修正: isDeadフラグがtrueであることのみ確認 (TakeDamageで設定済み)
        if (!isDead) return;

        Debug.Log(gameObject.name + "が停止しました。");

        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // アニメーションを再生 (TakeDamageで無効化したAnimatorを一時的に有効化)
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsAiming", false);
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsFloating", false);
            animator.SetTrigger("Die");
        }

        // 死亡アニメーションの完了を待ってからオブジェクトを無効化
        float animationDuration = 2.0f; // ★ 死亡アニメーションの再生時間に合わせる
        StartCoroutine(DisableObjectAfterDie(animationDuration));
    }

    // 💡 修正: オブジェクトをシーンから完全に削除するコルーチン
    IEnumerator DisableObjectAfterDie(float delay)
    {
        // アニメーションが再生し終わるまで待機
        yield return new WaitForSeconds(delay);

        // 死亡アニメーション終了後、再起動を防ぐためにオブジェクトをシーンから完全に削除する
        Destroy(gameObject); // 💥 これでいかなるロジックも再開しない
    }

    // ===================================
    // 5. AIロジック関数 (変更なし)
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
        Vector3 eyePosition = transform.position + Vector3.up * 0.1f;

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

    void AimingLogic(bool playerFound)
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float maxDegreesPerFrame = rotationSpeed * 60f * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, maxDegreesPerFrame);

        if (Quaternion.Angle(transform.rotation, lookRotation) < 5f)
        {
            TransitionToAttack();
            return;
        }

        if (!playerFound)
        {
            TransitionToIdle();
        }
    }

    void LandingLogic()
    {
        if (rb == null) return;
        rb.velocity = Vector3.down * landingSpeed;
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
            rb.velocity = Vector3.zero;
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

        Debug.Log(gameObject.name + ": 攻撃開始シークエンス！");

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
        Debug.Log("リロード開始...");

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
        Debug.Log("リロード完了！");

        if (CheckForPlayer())
        {
            TransitionToAiming();
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
        Debug.Log("弾が生成されました！ (残り弾数: " + currentAmmo + ")");
    }

    // ----------------------------------------------------
    // --- 確実な着地判定 (衝突判定) ---
    // ----------------------------------------------------

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
}