using UnityEngine;
using System.Collections; // コルーチンのためにSystem.Collectionsを追加

public class EnemyAI : MonoBehaviour
{
    private Animator anim;

    [Header("ターゲット設定")]
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("移動・後退設定")]
    [SerializeField] private float moveSpeed = 3f;      // 通常速度
    [SerializeField] private float dashSpeed = 6f;      // ダッシュ速度
    [SerializeField] private float retreatRange = 5f;
    [SerializeField] private float dashStartRange = 20f; // この距離を超えるとダッシュ開始


    [Header("射撃設定")]
    public float fireRate = 2.0f;
    private float nextFireTime;

    [Tooltip("射撃アニメーション開始から弾が出るまでの時間(秒)")]
    [SerializeField] private float shootingDelay = 0.2f;

    [Header("弾設定")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    [SerializeField] private float bulletSpeed = 100f;
    [SerializeField] private float bulletLifetime = 5f;

    // ★★★ リロード関連の追加フィールド ★★★
    [Header("リロード設定")]
    [SerializeField] private int maxAmmo = 10;        // 最大弾数
    private int currentAmmo;                          // 現在の弾数
    [SerializeField] private float reloadTime = 3.0f; // リロード時間
    private bool isReloading = false;                 // リロード中フラグ


    // Animatorのパラメーター名
    private const string IsAimingParam = "IsAiming";
    private const string FireTriggerParam = "FireTrigger";

    // Boolパラメーター名
    private const string MoveForwardParam = "MoveForward";
    private const string MoveBackwardParam = "MoveBackward";
    private const string MoveLeftParam = "MoveLeft";
    private const string MoveRightParam = "MoveRight";
    private const string IsDashingParam = "IsDashing"; // ダッシュ判定用

    void Start()
    {
        anim = GetComponent<Animator>();
        nextFireTime = Time.time;
        currentAmmo = maxAmmo; // ★ 追加: 初期弾数を最大にする

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

            // ★ 修正: リロード中でないときだけ構えアニメーションをONにする
            if (!isReloading)
            {
                anim.SetBool(IsAimingParam, true);
            }

            if (distanceToTarget <= retreatRange)
            {
                // プレイヤーが近すぎる場合: 後退
                currentSpeedZ = -moveSpeed;
                transform.Translate(Vector3.forward * currentSpeedZ * Time.deltaTime);
            }
            else
            {
                // 適切な距離（定点射撃範囲）の場合: 左右に移動 (ストレイフ)
                currentSpeedZ = 0f;

                // 左右ストレイフのロジック (省略)
                Vector3 directionToTarget = target.position - transform.position;
                float dotProductX = Vector3.Dot(directionToTarget.normalized, transform.right);

                if (dotProductX > 0.1f)
                {
                    currentSpeedX = moveSpeed;
                }
                else if (dotProductX < -0.1f)
                {
                    currentSpeedX = -moveSpeed;
                }
                else
                {
                    currentSpeedX = 0f;
                }

                transform.Translate(Vector3.right * currentSpeedX * Time.deltaTime);
            }

            // 2. 射撃タイミングのチェック
            // ★ 修正: リロード中でなく、弾があるときだけ射撃を許可
            if (!isReloading && currentAmmo > 0 && Time.time >= nextFireTime)
            {
                // 射撃トリガーをセットし、コルーチンを開始する
                anim.SetTrigger(FireTriggerParam);
                StartCoroutine(ShootWithDelay());

                // 次の発射時間を設定
                nextFireTime = Time.time + fireRate;
            }
        }
        // 3. 攻撃範囲外の行動 (省略)
        else
        {
            // 攻撃範囲外に出たら、射撃コルーチンを停止する
            StopAllCoroutines();
            // ... (その他の追跡ロジックは省略) ...

            if (distanceToTarget > dashStartRange)
            {
                currentSpeedZ = 0f;
                currentSpeedX = 0f;
                anim.SetBool(IsAimingParam, false);
            }
            else
            {
                currentSpeedX = 0f;
                anim.SetBool(IsAimingParam, false);

                if (distanceToTarget > (dashStartRange / 2f))
                {
                    currentSpeedZ = dashSpeed;
                }
                else
                {
                    currentSpeedZ = moveSpeed;
                }

                transform.Translate(Vector3.forward * currentSpeedZ * Time.deltaTime);
                LookAtTarget();
            }
        }

        // ★★★ リロード開始チェック ★★★
        // 攻撃範囲内にいて、弾がなく、リロード中でない場合
        if (distanceToTarget <= attackRange && currentAmmo <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
        }


        // --- アニメーションのBoolパラメーター設定（省略） ---
        anim.SetBool(MoveForwardParam, false);
        anim.SetBool(MoveBackwardParam, false);
        anim.SetBool(MoveLeftParam, false);
        anim.SetBool(MoveRightParam, false);
        anim.SetBool(IsDashingParam, false);

        if (currentSpeedZ > 0f)
        {
            if (currentSpeedZ > moveSpeed + 0.1f)
            {
                anim.SetBool(IsDashingParam, true);
            }
            anim.SetBool(MoveForwardParam, true);
        }
        else if (currentSpeedZ < 0f)
        {
            anim.SetBool(MoveBackwardParam, true);
        }
        else if (currentSpeedZ == 0f)
        {
            if (currentSpeedX > 0.1f)
            {
                anim.SetBool(MoveRightParam, true);
            }
            else if (currentSpeedX < -0.1f)
            {
                anim.SetBool(MoveLeftParam, true);
            }
        }
    }

    // ★★★ リロード処理コルーチン ★★★
    private IEnumerator Reload()
    {
        isReloading = true;
        // リロード中は構えを解除
        anim.SetBool(IsAimingParam, false);

        // TODO: リロードアニメーションの再生やSEの再生をここに入れる
        // anim.SetTrigger("ReloadTrigger"); 

        Debug.Log("リロード開始...");
        yield return new WaitForSeconds(reloadTime);

        // 弾数を回復
        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("リロード完了。弾数: " + currentAmmo);

        // 射撃モードに戻る
        anim.SetBool(IsAimingParam, true);
    }

    /// <summary>
    /// 射撃アニメーション開始からディレイ後に弾を生成
    /// </summary>
    private IEnumerator ShootWithDelay()
    {
        // shootingDelay秒待機する
        yield return new WaitForSeconds(shootingDelay);

        // ディレイ後、弾を生成する関数を呼び出す
        ShootBullet();
    }

    /// <summary>
    /// 弾を生成し、発射する (BulletControllerの機能と衝突回避を含む)
    /// </summary>
    public void ShootBullet()
    {
        // ★ 修正: 弾を発射するたびに弾数を減らす
        if (bulletPrefab != null && firePoint != null && currentAmmo > 0)
        {
            currentAmmo--; // ★ 弾数減少

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // 1. 【消滅ロジック】時間経過による自動消滅を設定
            Destroy(bullet, bulletLifetime);

            // --- 2. 【最重要】衝突回避ロジック ---
            Collider bulletCollider = bullet.GetComponent<Collider>();

            // 敵のコライダーを、ルート or 子オブジェクトから取得を試みる
            Collider enemyCollider = GetComponent<Collider>();
            if (enemyCollider == null)
            {
                enemyCollider = GetComponentInChildren<Collider>();
            }

            if (bulletCollider != null && enemyCollider != null)
            {
                // 弾と敵自身の衝突判定を一時的に無視 (0.3秒間)
                Physics.IgnoreCollision(bulletCollider, enemyCollider, true);
                StartCoroutine(StopIgnoringCollision(bulletCollider, enemyCollider, 0.3f));
            }
            else
            {
                Debug.LogError("FATAL ERROR: Enemy or Bullet Collider is missing. Cannot ignore collision!");
            }
            // ------------------------------------

            // 3. 弾丸に速度を与える（Rigidbodyの場合）
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = bullet.transform.forward * bulletSpeed;
            }
        }
    }

    // 衝突無視解除のコルーチン
    private IEnumerator StopIgnoringCollision(Collider bulletCollider, Collider enemyCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bulletCollider != null && enemyCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, enemyCollider, false);
        }
    }

    /// <summary>
    /// ターゲットの方をゆっくりと向く
    /// </summary>
    void LookAtTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }
}