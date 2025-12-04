using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class MachineGunChaserAI : MonoBehaviour
{
    // --- 状態定義 ---
    public enum EnemyState { Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Idle;

    // --- AI 設定 ---
    public Transform player;
    public float sightRange = 15f;
    public float attackRange = 5f;
    public float rotationSpeed = 10f;

    // --- 攻撃設定 (マシンガン連射仕様) ---
    // 💡 修正点 1: 連射数と連射間隔をマシンガン向けに調整
    public int bulletsPerBurst = 25;        // 1回のバーストで発射する弾数
    public float timeBetweenShots = 0.05f;  // 連射間隔 (高速化)
    public float shootDuration = 0.5f;

    // 💡 修正点 2: 弾速と拡散の設定を追加
    public float bulletLaunchForce = 150f;  // 弾速 (高速化)
    public float fireSpread = 0.08f;        // 弾の拡散量（マシンガンらしいブレ）

    // --- 弾薬とリロード設定 ---
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;

    // ★★★ 銃弾の発射に必要な参照 ★★★
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // --- コンポーネント ---
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth health;

    // ----------------------------------------------------------------------

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            TransitionToIdle();
        }
        currentAmmo = maxAmmo;
    }

    // ----------------------------------------------------------------------

    void Update()
    {
        // 🚨 死亡チェック: HPがゼロ以下なら即座に処理を終了
        if (health != null && health.currentHealth <= 0)
        {
            CancelInvoke(); // 全ての予約処理をキャンセル
            if (animator != null)
            {
                animator.SetBool("IsAiming", false);
                animator.SetBool("IsRunning", false);
            }
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
            }
            return;
        }


        if (player == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

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
                // Reload中はロジックなし
                break;
        }

        // 💡 アニメーション制御（移動速度連動）
        if (animator != null && agent.isActiveAndEnabled)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
        else if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }
    }

    // ----------------------------------------------------
    // --- ロジック関数 (変更なし) ---
    // ----------------------------------------------------

    void IdleLogic(float distance)
    {
        if (distance <= sightRange)
        {
            TransitionToChase();
        }
    }

    void ChaseLogic(float distance)
    {
        if (agent.isActiveAndEnabled)
        {
            agent.SetDestination(player.position);
        }

        if (distance <= attackRange)
        {
            TransitionToAttack();
        }
        else if (distance > sightRange)
        {
            TransitionToIdle();
        }
    }

    void AttackLogic(float distance)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        if (distance > attackRange * 1.2f)
        {
            TransitionToChase();
        }
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 (Attackのみ修正) ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        currentState = EnemyState.Idle;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = true;
        }
        CancelInvoke();
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    void TransitionToChase()
    {
        currentState = EnemyState.Chase;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
        if (animator != null) animator.SetBool("IsAiming", false);
        CancelInvoke();
    }

    void TransitionToAttack()
    {
        // 弾切れの場合、AttackではなくReloadへ遷移
        if (currentAmmo <= 0)
        {
            TransitionToReload();
            return;
        }

        currentState = EnemyState.Attack;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsAiming", true);
            animator.SetTrigger("Shoot");
        }

        CancelInvoke("ShootBullet");

        // 💡 修正: 高速・多弾数での発射を予約
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            Invoke("ShootBullet", i * timeBetweenShots);
        }

        float totalBurstTime = (bulletsPerBurst - 1) * timeBetweenShots;
        float totalAttackTime = totalBurstTime + shootDuration;

        Invoke("TransitionToAttackComplete", totalAttackTime);
    }

    void TransitionToAttackComplete()
    {
        if (health != null && health.currentHealth <= 0) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack();
        }
        else
        {
            TransitionToChase();
        }
    }

    // ----------------------------------------------------
    // --- 弾丸生成処理 (最重要修正箇所) ---
    // ----------------------------------------------------

    public void ShootBullet()
    {
        if (health != null && health.currentHealth <= 0) return;
        if (currentAmmo <= 0) return;

        currentAmmo--;

        if (gameObject == null || !gameObject.activeInHierarchy || bulletPrefab == null || muzzlePoint == null)
        {
            return;
        }

        // 💡 修正点 3-1: 拡散用のランダムなオフセットを計算
        Vector3 randomOffset = new Vector3(
            Random.Range(-fireSpread, fireSpread),
            Random.Range(-fireSpread, fireSpread),
            Random.Range(-fireSpread, fireSpread)
        );

        GameObject newBullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        newBullet.transform.parent = null;

        // 💡 修正点 3-2: Rigidbodyを取得し、推進力を与える
        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 前方ベクトルにランダムオフセットを加えて発射
            Vector3 fireDirection = muzzlePoint.forward + randomOffset;
            // 推進力を与える (弾速の適用)
            rb.AddForce(fireDirection.normalized * bulletLaunchForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("弾丸PrefabにRigidbodyがありません。弾が飛びません。");
        }

        Debug.Log("弾が発射されました！ 残り弾薬: " + currentAmmo);
    }

    // ----------------------------------------------------
    // --- リロード処理 (変更なし) ---
    // ----------------------------------------------------

    void ReloadLogic()
    {
        if (health != null && health.currentHealth <= 0) return;
        // リロード中はアニメーションと時間待ちがメイン
    }

    void TransitionToReload()
    {
        currentState = EnemyState.Reload;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAttackComplete");

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetTrigger("Reload");
        }

        Debug.Log("リロード開始... (" + reloadTime + "秒)");
        Invoke("FinishReload", reloadTime);
    }

    void FinishReload()
    {
        if (health != null && health.currentHealth <= 0) return;

        currentAmmo = maxAmmo;
        Debug.Log("リロード完了！");

        if (player == null)
        {
            TransitionToIdle();
            return;
        }

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack();
        }
        else if (distance <= sightRange)
        {
            TransitionToChase();
        }
        else
        {
            TransitionToIdle();
        }
    }
}