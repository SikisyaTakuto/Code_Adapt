using UnityEngine;
using System.Collections;
using UnityEngine.AI;

// NavMeshAgentコンポーネントが必須であることを保証
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;

    [Header("ターゲット設定")]
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float rotationSpeed = 10f; // LookAtTargetでの回転速度

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


    // Animatorのパラメーター名 (ブレンドツリー用)
    private const string SpeedParam = "Speed";
    private const string IsAimingParam = "IsAiming";
    private const string IsBackpedalingParam = "IsBackpedaling";
    private const string FireTriggerParam = "Shoot";
    private const string ReloadTriggerParam = "Reload";


    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Agentの回転を無効にし、LookAtTarget()で自分で回転を制御する
        if (agent != null)
        {
            agent.updateRotation = false;
        }

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

        // 必須チェック: Agentが存在しない、または無効な場合は処理を中断
        if (agent == null || !agent.isActiveAndEnabled)
        {
            return;
        }

        // リロード中はアニメーションの速度を0に固定し、他の行動を停止
        if (isReloading)
        {
            anim.SetFloat(SpeedParam, 0f);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        LookAtTarget(); // 常にターゲットの方を向く

        if (distanceToTarget <= attackRange)
        {
            HandleCombat(distanceToTarget);
        }
        else
        {
            HandleChase(distanceToTarget);
        }

        UpdateAnimatorParameters();
    }


    /// <summary>
    /// ターゲットの方を滑らかに向く
    /// </summary>
    void LookAtTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    // --------------------------------------------------------------------------------
    // ⭐️ AI行動ロジック（NavMeshAgent使用）
    // --------------------------------------------------------------------------------

    void HandleChase(float distanceToTarget)
    {
        // 構え解除 (Movement Blend Treeへ移行)
        anim.SetBool(IsAimingParam, false);
        anim.SetBool(IsBackpedalingParam, false);

        float currentSpeed;

        // ダッシュロジック
        currentSpeed = (distanceToTarget > dashStartRange) ? dashSpeed : moveSpeed;

        // ⭐️ NavMesh Agent操作の防御的チェック ⭐️
        if (agent.isOnNavMesh)
        {
            agent.speed = currentSpeed;
            // 目的地を設定し、Agentに移動させる
            agent.SetDestination(target.position);
        }
    }

    void HandleCombat(float distanceToTarget)
    {
        // 戦闘モードに入る
        anim.SetBool(IsAimingParam, true);

        // 射撃・リロードチェック
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // 距離による移動行動決定
        if (distanceToTarget <= retreatRange)
        {
            // プレイヤーが近すぎる場合: 後退
            anim.SetBool(IsBackpedalingParam, true);

            // ⭐️ NavMesh Agent操作の防御的チェック ⭐️
            if (agent.isOnNavMesh)
            {
                // 後退方向への目的地を設定
                Vector3 retreatDir = (transform.position - target.position).normalized;
                Vector3 retreatPos = transform.position + retreatDir * retreatRange;

                agent.speed = moveSpeed;
                agent.SetDestination(retreatPos);
            }
        }
        else
        {
            // 適切な距離: 停止 (定点射撃)
            anim.SetBool(IsBackpedalingParam, false);

            // ⭐️ NavMesh Agent操作の防御的チェック ⭐️
            if (agent.isOnNavMesh)
            {
                agent.speed = 0f;
                // 現在位置を目的地に設定して停止を維持
                agent.SetDestination(transform.position);
            }

            // 射撃アクションをトリガー
            if (Time.time >= nextFireTime)
            {
                anim.SetTrigger(FireTriggerParam);
                StartCoroutine(ShootWithDelay());
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    // --------------------------------------------------------------------------------
    // ⭐️ アニメーションパラメータの更新ロジック
    // --------------------------------------------------------------------------------

    void UpdateAnimatorParameters()
    {
        // NavMeshAgentの現在の水平速度の大きさ
        float speedForAnim = agent.velocity.magnitude;

        // 後退中は移動アニメーションを停止し、Idle_Shootに留める
        if (anim.GetBool(IsBackpedalingParam))
        {
            anim.SetFloat(SpeedParam, 0f);
        }
        else
        {
            // 通常の移動/追跡時は、実際の速度を渡す
            anim.SetFloat(SpeedParam, speedForAnim);
        }
    }


    // --- リロード/射撃関連のコルーチン ---

    private IEnumerator Reload()
    {
        isReloading = true;
        anim.SetBool(IsAimingParam, false);

        // ⭐️ NavMesh Agent操作の防御的チェック (isStoppedエラー回避) ⭐️
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // 完全に停止
        }

        anim.SetTrigger(ReloadTriggerParam);
        Debug.Log("リロード開始...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("リロード完了。弾数: " + currentAmmo);

        // ⭐️ NavMesh Agent操作の防御的チェック (isStoppedエラー回避) ⭐️
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private IEnumerator ShootWithDelay()
    {
        yield return new WaitForSeconds(shootingDelay);
        ShootBullet();
    }

    // --- 弾の生成と物理処理 ---

    /// <summary>
    /// 弾をInstantiateで生成し、Rigidbodyに速度を与え、Destroyで消滅させる
    /// </summary>
    public void ShootBullet()
    {
        if (bulletPrefab == null || firePoint == null || currentAmmo <= 0)
        {
            return;
        }

        // 1. Instantiate (生成)
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        currentAmmo--; // 弾数減少

        // 2. Destroy (消滅)
        Destroy(bullet, bulletLifetime);

        // 3. 衝突回避ロジック
        Collider bulletCollider = bullet.GetComponent<Collider>();
        Collider enemyCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        if (bulletCollider != null && enemyCollider != null)
        {
            // 弾と敵自身の衝突判定を一時的に無視
            Physics.IgnoreCollision(bulletCollider, enemyCollider, true);
            StartCoroutine(StopIgnoringCollision(bulletCollider, enemyCollider, 0.3f));
        }
        else
        {
            Debug.LogError("FATAL ERROR: Enemy or Bullet Collider is missing.");
        }

        // 4. 弾丸に速度を与える（Rigidbodyの場合）
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bullet.transform.forward * bulletSpeed;
        }
    }

    /// <summary>
    /// 弾が敵をすり抜けた後、衝突無視を解除するコルーチン
    /// </summary>
    private IEnumerator StopIgnoringCollision(Collider bulletCollider, Collider enemyCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        // 弾が破棄されていないか確認してから無視を解除
        if (bulletCollider != null && enemyCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, enemyCollider, false);
        }
    }
}