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

    // 💡 変更点: Landing状態を追加
    public enum EnemyState { Landing, Idle, Aiming, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing; // 💡 初期状態をLandingに変更

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

    // 💡 追加: 着地設定
    [Header("着地設定")]
    public float initialWaitTime = 1.0f;  // 浮遊してから落下を開始するまでの待機時間
    public float landingSpeed = 2.0f;    // ゆっくり落下する速度
    public string groundTag = "Ground"; // 地面と判定するオブジェクトのタグ

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

        // 💡 物理初期設定: Landing処理のため、最初は物理演算を無効化
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

        // 💡 修正: 初期状態をLandingにし、着地処理を開始
        TransitionToLanding();
    }

    // ===================================
    // 3. メインループ
    // ===================================

    // 💡 FixedUpdateは物理処理とLandingLogicのみに使用
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

        // 💡 修正: Landing状態では、他の全てのロジックをスキップ
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
                // Attack状態はInvokeで制御されるため、Updateでは何もしない
                break;
            case EnemyState.Reload:
                // Reload状態はInvokeで制御されるため、Updateでは何もしない
                break;
        }
    }

    // ===================================
    // 4. ダメージ・死亡処理 (変更なし)
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
            // 💡 追加: 浮遊アニメーションもオフに
            animator.SetBool("IsFloating", false);
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
    }

    // ===================================
    // 5. AIロジック関数 (Idle, Aimingは変更なし)
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

    // 💡 追加: ゆっくり落下させるロジック
    void LandingLogic()
    {
        if (rb == null) return;
        rb.velocity = Vector3.down * landingSpeed;
    }

    // ===================================
    // 6. 状態遷移と発砲
    // ===================================

    // 💡 追加: Landing遷移の開始
    void TransitionToLanding()
    {
        if (isDead) return;
        currentState = EnemyState.Landing;

        CancelInvoke();
        StopAllCoroutines();

        // 浮遊待機後に落下を開始
        Invoke("StartFalling", initialWaitTime);

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsFloating", true); // 💡 浮遊アニメーションON
        }
    }

    // 💡 追加: 落下開始（物理を有効化）
    void StartFalling()
    {
        if (isDead) return;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false; // LandingLogicで速度制御するため、重力は一旦OFF
        }
    }

    // 💡 追加: 着地完了コルーチン（物理安定化）
    IEnumerator FinishLandingCoroutine()
    {
        if (isDead) yield break;

        // 衝突判定と位置調整が確定するまで待機
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // 物理演算設定を通常AI動作に戻す
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true; // 重力ONに戻す
        }

        // コライダーを有効化
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsFloating", false); // 浮遊アニメーションOFF
        }

        // プレイヤーが近くにいればAiming、いなければIdleへ
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
            // 💡 衝突したら既存の落下処理を停止
            StopCoroutine("FinishLandingCoroutine");
            CancelInvoke("StartFalling");

            if (rb != null)
            {
                rb.velocity = Vector3.zero;

                float contactY = collision.contacts[0].point.y;

                if (enemyCollider != null)
                {
                    // 衝突中の物理干渉を防ぐため、コライダーを一時的に無効化
                    enemyCollider.enabled = false;
                    // 位置調整
                    transform.position = new Vector3(transform.position.x, contactY + enemyCollider.bounds.extents.y, transform.position.z);
                }
            }

            // 着地完了処理をコルーチンで呼び出す
            StartCoroutine(FinishLandingCoroutine());
        }
    }
}