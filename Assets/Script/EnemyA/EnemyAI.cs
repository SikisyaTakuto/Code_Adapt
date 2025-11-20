using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Queueのために必要
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;
    private Collider myCollider; // 自分のコライダーをキャッシュ

    [Header("ターゲット設定")]
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("移動・後退設定")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float dashSpeed = 6f;
    [SerializeField] private float retreatRange = 5f;

    // 攻撃範囲内でも、この距離まではターゲットに近づこうとします
    [Tooltip("攻撃範囲内でも、この距離まではターゲットに近づこうとします")]
    [SerializeField] private float approachDistance = 8f;

    // [SerializeField] private float dashStartRange = 20f; // 未使用のため今回は削除

    [Header("射撃設定")]
    public float fireRate = 2.0f;
    private float nextFireTime;
    [SerializeField] private float shootingDelay = 0.2f;

    [Header("弾設定")]
    // 複数の種類の弾を管理する配列
    [SerializeField] public Bullet[] bulletPrefabs;

    public Transform firePoint;
    [SerializeField] private float bulletSpeed = 100f;
    [SerializeField] private float bulletLifetime = 5f;

    // プーリング用のキュー (簡易的に全種類の弾を共有)
    private Queue<Bullet> bulletPool = new Queue<Bullet>();

    [Header("リロード設定")]
    [SerializeField] private int maxAmmo = 10;
    private int currentAmmo;
    [SerializeField] private float reloadTime = 3.0f;
    private bool isReloading = false;

    // Animator Parameters
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsAimingParam = Animator.StringToHash("IsAiming");
    private static readonly int IsBackpedalingParam = Animator.StringToHash("IsBackpedaling");
    private static readonly int FireTriggerParam = Animator.StringToHash("Shoot");
    private static readonly int ReloadTriggerParam = Animator.StringToHash("Reload");

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        myCollider = GetComponent<Collider>();

        if (agent != null) agent.updateRotation = false;

        // ★★★ NavMeshAgent 初期化失敗対策 ★★★
        if (agent != null && agent.isActiveAndEnabled)
        {
            NavMeshHit hit;
            // 現在地から半径 1.0f 以内で最も近いNavMesh上の点を探索
            if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                // 有効な位置が見つかったら、そこに座標を修正する
                transform.position = hit.position;
            }
        }
        // ★★★ 対策コードはここまで ★★★

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
        if (agent == null || !agent.isActiveAndEnabled) return;

        if (isReloading)
        {
            anim.SetFloat(SpeedParam, 0f);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        LookAtTarget();

        if (distanceToTarget <= attackRange)
            HandleCombat(distanceToTarget);
        else
            HandleChase(distanceToTarget);

        // ★ Agent の StoppingDistance を動的に調整 (振動対策)
        if (distanceToTarget <= approachDistance && distanceToTarget >= retreatRange)
        {
            // 理想距離（停止したい）
            agent.stoppingDistance = 1.0f;
        }
        else
        {
            // 移動中
            agent.stoppingDistance = 0.1f;
        }

        UpdateAnimatorParameters();
    }

    void LookAtTarget()
    {
        // ターゲットの位置ではなく、firePointからターゲットの中心への方向を使う
        Vector3 direction = target.position - firePoint.position;

        // Y軸の回転は維持する
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // --------------------------------------------------------------------------------
    // AI行動ロジック
    // --------------------------------------------------------------------------------

    void HandleChase(float distanceToTarget)
    {
        anim.SetBool(IsAimingParam, false);
        anim.SetBool(IsBackpedalingParam, false);

        if (agent.isOnNavMesh)
        {
            agent.speed = dashSpeed;
            agent.SetDestination(target.position);
        }
    }

    void UpdateAnimatorParameters()
    {
        float currentVelocity = agent.velocity.magnitude;

        if (anim.GetBool(IsBackpedalingParam))
        {
            // 後退中は Speed = 0f (専用アニメーションを使用しない場合)
            anim.SetFloat(SpeedParam, 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            // 移動/Aim中/Run時は実測速度を使用
            anim.SetFloat(SpeedParam, currentVelocity, 0.1f, Time.deltaTime);
        }
    }

    void HandleCombat(float distanceToTarget)
    {
        anim.SetBool(IsAimingParam, true);

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (agent.isOnNavMesh)
        {
            // 1. 近すぎる: 後退 (Retreat)
            if (distanceToTarget <= retreatRange)
            {
                anim.SetBool(IsBackpedalingParam, true);

                Vector3 retreatDir = (transform.position - target.position).normalized;
                Vector3 theoreticalRetreatPos = transform.position + retreatDir * retreatRange * 2f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(theoreticalRetreatPos, out hit, retreatRange * 3f, NavMesh.AllAreas))
                {
                    agent.speed = moveSpeed;
                    agent.SetDestination(hit.position);
                }
                else
                {
                    // 後退先がない場合
                    agent.speed = 0f;
                    agent.SetDestination(transform.position);
                }
            }
            // 2. 遠すぎる (射程内だが遠い): 前進して詰める (Approach -> WalkFront_Shoot)
            else if (distanceToTarget > approachDistance)
            {
                anim.SetBool(IsBackpedalingParam, false);
                agent.speed = moveSpeed;
                agent.SetDestination(target.position);
            }
            // 3. 理想距離: 停止 (Idle_Shoot)
            else
            {
                anim.SetBool(IsBackpedalingParam, false);
                // StoppingDistanceの調整により、この範囲内で停止する
                agent.speed = 0f;
                agent.SetDestination(transform.position);
            }
        }

        // 射撃処理
        if (Time.time >= nextFireTime)
        {
            anim.SetTrigger(FireTriggerParam);
            StartCoroutine(ShootWithDelay());
            nextFireTime = Time.time + fireRate;
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        anim.SetBool(IsAimingParam, false);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        anim.SetTrigger(ReloadTriggerParam);
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        if (agent.isOnNavMesh) agent.isStopped = false;
    }

    private IEnumerator ShootWithDelay()
    {
        yield return new WaitForSeconds(shootingDelay);
        ShootBullet();
    }

    // --------------------------------------------------------------------------------
    // ★★★ プーリングと射撃ロジック（弾種切り替え対応） ★★★
    // --------------------------------------------------------------------------------

    public void ShootBullet()
    {
        // 1. ターゲットまでの距離を取得 (弾種切り替えに使用)
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // 2. 発射するプレハブを決定
        Bullet selectedPrefab = GetSelectedBulletPrefab(distanceToTarget);

        // プレハブが設定されていない、発射位置がない、弾がない場合は終了
        if (selectedPrefab == null || firePoint == null || currentAmmo <= 0) return;

        // 3. プールから弾を取得
        Bullet bullet = GetBulletFromPool(selectedPrefab);

        if (bullet == null) return;

        // 4. 位置と回転を設定
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;
        bullet.gameObject.SetActive(true);

        // 5. レイヤー設定
        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null && myCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, myCollider, true);
        }

        // 6. Bulletの初期化
        bullet.Initialize(ReturnBulletToPool, bulletSpeed, bulletLifetime);

        // 7. 速度を与える
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = firePoint.forward * bulletSpeed;
        }

        currentAmmo--;
    }

    /// <summary>
    /// ターゲットまでの距離に応じて発射する弾のプレハブを決定します。
    /// </summary>
    private Bullet GetSelectedBulletPrefab(float distanceToTarget)
    {
        if (bulletPrefabs == null || bulletPrefabs.Length < 2)
        {
            if (bulletPrefabs != null && bulletPrefabs.Length > 0)
            {
                return bulletPrefabs[0];
            }
            // Debug.LogError("Bullet Prefabsが設定されていません。または2種類以上必要です。");
            return null;
        }

        // 距離が遠い場合 (approachDistance より外側)
        if (distanceToTarget > approachDistance)
        {
            return bulletPrefabs[0];
        }
        else
        {
            // 近距離または理想距離の場合
            return bulletPrefabs[1];
        }
    }

    /// <summary>
    /// プールから弾を取得します。プールが空の場合は、指定されたプレハブを新規生成します。
    /// </summary>
    private Bullet GetBulletFromPool(Bullet prefabToInstantiate)
    {
        if (bulletPool.Count > 0)
        {
            // 簡易プーリング: 種類に関わらず、キュー内の既存の弾をデキューして再利用
            return bulletPool.Dequeue();
        }
        else
        {
            // プールが空の場合のみ、指定されたプレハブを使用してInstantiate
            if (prefabToInstantiate == null)
            {
                return null;
            }
            Bullet newBullet = Instantiate(prefabToInstantiate);
            return newBullet;
        }
    }

    private void ReturnBulletToPool(Bullet bullet)
    {
        if (bullet != null)
        {
            // 弾のRigidbody速度をリセット
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;

            bullet.gameObject.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }
}