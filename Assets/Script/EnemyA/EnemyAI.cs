using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // --- 状態定義 ---
    public enum EnemyState { Idle, Idle_Shoot, Attack }
    public EnemyState currentState = EnemyState.Idle;

    // --- 弾薬とリロード設定 ---
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;
    private bool isReloading = false;

    // --- AI 設定 ---
    public float sightRange = 15f;
    public float viewAngle = 90f;
    public float rotationSpeed = 3f;
    public float shootDuration = 1.0f;

    // 💡 1回の攻撃で発射する弾数
    public int bulletsPerBurst = 1;
    // 💡 バースト内の弾と弾の間の時間（連射速度）
    public float timeBetweenShots = 0.1f;

    // ★★★ 銃弾の発射に必要な参照 ★★★
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // --- コンポーネント ---
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private float nextRotationTime;

    // 💡 EnemyHealthへの参照を追加
    private EnemyHealth health;

    // ★★★ Start() 関数 ★★★
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>(); // 💡 EnemyHealthの参照を取得

        if (agent == null) Debug.LogError("NavMeshAgentがありません。");
        if (animator == null) Debug.LogError("Animatorがありません。");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) { player = playerObj.transform; }
        else { Debug.LogError("Playerタグのオブジェクトが見つかりません。"); }

        currentState = EnemyState.Idle;

        if (agent != null)
        {
            // 💡 NavMeshAgentが有効な場合のみ操作
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
                agent.updateRotation = false;
            }
        }

        nextRotationTime = Time.time + Random.Range(3f, 6f);
        currentAmmo = maxAmmo;
    }

    // --- リロード処理 ---

    void StartReload()
    {
        // 💡 死亡時はリロードしない
        if (health != null && health.currentHealth <= 0) return;

        isReloading = true;
        Debug.Log("静止型ロボット：リロード開始...");

        // 攻撃中のInvokeを全てキャンセル
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
        // 💡 死亡時はリロード完了しても何もしない
        if (health != null && health.currentHealth <= 0) return;

        isReloading = false;
        currentAmmo = maxAmmo;
        Debug.Log("静止型ロボット：リロード完了！");

        if (CheckForPlayer())
        {
            TransitionToIdle_Shoot();
        }
        else
        {
            TransitionToIdle();
        }
    }

    // ★★★ FixedUpdate() 関数 ★★★
    void FixedUpdate()
    {
        // 💡 死亡またはリロード中は処理をスキップ
        if (player == null || agent == null || animator == null || isReloading || (health != null && health.currentHealth <= 0)) return;

        if (currentState == EnemyState.Idle_Shoot)
        {
            bool playerFoundInFixed = CheckForPlayer();
            Idle_ShootLogic(playerFoundInFixed);
        }
    }

    // ★★★ Update() 関数 ★★★
    void Update()
    {
        // 1. 🚨 死亡チェック: HPがゼロ以下なら即座に処理を終了 (最重要修正箇所)
        if (health != null && health.currentHealth <= 0)
        {
            // 💡 死亡した瞬間、全ての予約された攻撃/リロード処理をキャンセルする
            CancelInvoke("ShootBullet");
            CancelInvoke("TransitionToIdle_Shoot");
            CancelInvoke("FinishReload");

            // アニメーションを静止状態に移行
            if (animator != null)
            {
                animator.SetBool("IsAiming", false);
                animator.SetFloat("Speed", 0f);
            }

            return; // これ以降のAIロジックは全てスキップ
        }

        // 💡 リロード中は処理をスキップ
        if (player == null || agent == null || animator == null || isReloading) return;
        animator.SetFloat("Speed", 0f);

        bool playerFound = CheckForPlayer();

        if (currentState == EnemyState.Idle)
        {
            IdleLogic(playerFound);
        }
    }

    // ... (IdleLogic, Idle_ShootLogic, CheckForPlayer は変更なし) ...
    void IdleLogic(bool playerFound)
    {
        // ... (省略) ...
        if (playerFound)
        {
            TransitionToIdle_Shoot();
            return;
        }

        if (Time.time > nextRotationTime)
        {
            nextRotationTime = Time.time + Random.Range(3f, 6f);
            float randomAngle = Random.Range(0f, 360f);
            Quaternion targetRotation = Quaternion.Euler(0, randomAngle, 0);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void Idle_ShootLogic(bool playerFound)
    {
        // ... (省略) ...
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

    bool CheckForPlayer()
    {
        if (player == null || agent == null) return false;

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


    // ----------------------------------------------------
    // --- 状態遷移関数 ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        // 💡 死亡時は遷移しない
        if (health != null && health.currentHealth <= 0) return;

        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToIdle_Shoot");
        CancelInvoke("FinishReload"); // 💡 リロードのInvokeもキャンセル

        currentState = EnemyState.Idle;
        animator.SetBool("IsAiming", false);
        nextRotationTime = Time.time + Random.Range(3f, 6f);
    }

    void TransitionToIdle_Shoot()
    {
        // 💡 死亡時は遷移しない
        if (health != null && health.currentHealth <= 0) return;

        currentState = EnemyState.Idle_Shoot;
        animator.SetBool("IsAiming", true);
    }

    // ★★★ TransitionToAttack ★★★
    void TransitionToAttack()
    {
        // 💡 死亡時は遷移しない
        if (health != null && health.currentHealth <= 0) return;

        // 🚨 攻撃開始前に弾薬チェック
        if (currentAmmo <= 0)
        {
            StartReload();
            return; // リロードに切り替えるため、これ以降の攻撃処理はスキップ
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
        float totalAttackTime = totalBurstTime + shootDuration;

        Invoke("TransitionToIdle_Shoot", totalAttackTime);
    }

    // ----------------------------------------------------
    // --- 弾丸生成処理 ---
    // ----------------------------------------------------

    public void ShootBullet()
    {
        // 💡 死亡時は発砲しない
        if (health != null && health.currentHealth <= 0) return;

        // 💡 弾切れの場合は即座に処理を終了
        if (currentAmmo <= 0) return;

        currentAmmo--; // 💡 弾薬を消費

        if (gameObject == null || !gameObject.activeInHierarchy) return;

        if (bulletPrefab == null || muzzlePoint == null)
        {
            Debug.LogError("弾丸プレハブまたは銃口が未設定です！");
            return;
        }

        GameObject newBullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        newBullet.transform.parent = null;

        Debug.Log("弾が生成されました！");
    }
}