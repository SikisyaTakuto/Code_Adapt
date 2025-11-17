using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private Animator anim;

    [Header("ターゲット設定")]
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("移動・後退設定")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float dashSpeed = 6f;
    [SerializeField] private float retreatRange = 5f;
    [SerializeField] private float dashStartRange = 20f;


    [Header("射撃設定")]
    public float fireRate = 2.0f;
    private float nextFireTime;

    [Tooltip("射撃アニメーション開始から弾が出るまでの時間(秒)")]
    [SerializeField] private float shootingDelay = 0.2f;

    [Header("弾設定")]
    public GameObject bulletPrefab; // 弾のPrefab
    public Transform firePoint;
    [SerializeField] private float bulletSpeed = 100f;
    [SerializeField] private float bulletLifetime = 5f; // Destroyで消滅させるまでの時間

    // リロード関連
    [Header("リロード設定")]
    [SerializeField] private int maxAmmo = 10;
    private int currentAmmo;
    [SerializeField] private float reloadTime = 3.0f;
    private bool isReloading = false;


    // Animatorのパラメーター名
    private const string IsAimingParam = "IsAiming";
    private const string FireTriggerParam = "FireTrigger";
    private const string MoveForwardParam = "MoveForward";
    private const string MoveBackwardParam = "MoveBackward";
    private const string MoveLeftParam = "MoveLeft";
    private const string MoveRightParam = "MoveRight";
    private const string IsDashingParam = "IsDashing";


    void Start()
    {
        anim = GetComponent<Animator>();
        nextFireTime = Time.time;
        currentAmmo = maxAmmo;

        if (target == null && GameObject.FindWithTag("Player") != null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float currentSpeedZ = 0f;
        float currentSpeedX = 0f;

        // 1. 攻撃範囲内の行動
        if (distanceToTarget <= attackRange)
        {
            LookAtTarget();
            if (!isReloading)
            {
                anim.SetBool(IsAimingParam, true);
            }

            if (distanceToTarget <= retreatRange)
            {
                currentSpeedZ = -moveSpeed;
                transform.Translate(Vector3.forward * currentSpeedZ * Time.deltaTime);
            }
            else
            {
                currentSpeedZ = 0f;
                // ... (ストレイフの移動ロジックは省略) ...
            }

            // 2. 射撃タイミングのチェック
            if (!isReloading && currentAmmo > 0 && Time.time >= nextFireTime)
            {
                anim.SetTrigger(FireTriggerParam);
                StartCoroutine(ShootWithDelay());
                nextFireTime = Time.time + fireRate;
            }
        }
        // 3. 攻撃範囲外の行動 (省略)
        else
        {
            StopAllCoroutines();
            anim.SetBool(IsAimingParam, false);
            // ... (追跡ロジックは省略) ...
        }

        // ★ リロード開始チェック ★
        if (distanceToTarget <= attackRange && currentAmmo <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
        }

        // --- アニメーションのBoolパラメーター設定（省略） ---
        // (省略)
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        anim.SetBool(IsAimingParam, false);

        Debug.Log("リロード開始...");
        // TODO: リロードアニメーション再生
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("リロード完了。弾数: " + currentAmmo);

        anim.SetBool(IsAimingParam, true);
    }

    private IEnumerator ShootWithDelay()
    {
        yield return new WaitForSeconds(shootingDelay);
        ShootBullet();
    }

    /// <summary>
    /// 弾をInstantiateで生成し、Destroyで消滅させる
    /// </summary>
    public void ShootBullet()
    {
        if (bulletPrefab == null || firePoint == null || currentAmmo <= 0)
        {
            return;
        }

        // ★★★ 1. Instantiate (生成) ★★★
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        currentAmmo--; // 弾数減少

        // ★★★ 2. Destroy (消滅) ★★★
        Destroy(bullet, bulletLifetime);

        // --- 3. 衝突回避ロジック ---
        Collider bulletCollider = bullet.GetComponent<Collider>();
        // 敵のコライダーを取得
        Collider enemyCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        if (bulletCollider != null && enemyCollider != null)
        {
            // 弾と敵自身の衝突判定を一時的に無視 (0.3秒間)
            Physics.IgnoreCollision(bulletCollider, enemyCollider, true);
            StartCoroutine(StopIgnoringCollision(bulletCollider, enemyCollider, 0.3f));
        }
        else
        {
            Debug.LogError("FATAL ERROR: Enemy or Bullet Collider is missing.");
        }
        // ------------------------------------

        // 4. 弾丸に速度を与える（Rigidbodyの場合）
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bullet.transform.forward * bulletSpeed;
        }
    }

    // 衝突無視解除のコルーチン
    private IEnumerator StopIgnoringCollision(Collider bulletCollider, Collider enemyCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        // 弾が破棄されていないか確認してから無視を解除
        if (bulletCollider != null && enemyCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, enemyCollider, false);
        }
    }

    void LookAtTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }
}