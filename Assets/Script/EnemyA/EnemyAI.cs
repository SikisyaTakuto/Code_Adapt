using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // --- 状態定義 ---
    public enum EnemyState { Idle, Idle_Shoot, Attack }
    public EnemyState currentState = EnemyState.Idle;

    // EnemyAI.cs クラスの最初に追加

    // --- 弾薬とリロード設定 ---
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;
    private bool isReloading = false; // 💡 リロード中かどうかを示すフラグ

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

    // 💡 弾薬とリロード関連の変数を全て削除

    // ★★★ Start() 関数 ★★★
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null) Debug.LogError("NavMeshAgentがありません。");
        if (animator == null) Debug.LogError("Animatorがありません。");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) { player = playerObj.transform; }
        else { Debug.LogError("Playerタグのオブジェクトが見つかりません。"); }

        currentState = EnemyState.Idle;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        nextRotationTime = Time.time + Random.Range(3f, 6f);

        // 💡 弾薬を満タンに初期化
        currentAmmo = maxAmmo;
       
    }

    // EnemyAI.cs に追加

    // --- リロード処理 ---

    void StartReload()
    {
        isReloading = true; // 💡 リロード中フラグをON
        Debug.Log("静止型ロボット：リロード開始...");

        // 攻撃中のInvokeを全てキャンセルし、アニメーションを発動
        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToIdle_Shoot");

        if (animator != null)
        {
            // 💡 攻撃態勢を解除し、リロードアニメーションを発動
            animator.SetBool("IsAiming", false);
            animator.SetTrigger("Reload");
        }

        // リロード時間が経過したら FinishReload を呼び出す
        Invoke("FinishReload", reloadTime);
    }

    void FinishReload()
    {
        isReloading = false; // 💡 リロード中フラグをOFF
        currentAmmo = maxAmmo;
        Debug.Log("静止型ロボット：リロード完了！");

        // プレイヤーがまだ視界内にいるか再チェック
        if (CheckForPlayer())
        {
            // 視界内なら、すぐに攻撃態勢 (Idle_Shoot) に戻る
            TransitionToIdle_Shoot();
        }
        else
        {
            // 視界外なら、通常の待機 (Idle) に戻る
            TransitionToIdle();
        }
    }

    // ★★★ FixedUpdate() 関数 ★★★
    void FixedUpdate()
    {
        if (player == null || agent == null || animator == null || isReloading) return; // 💡 リロード中は処理をスキップ

        if (currentState == EnemyState.Idle_Shoot)
        {
            bool playerFoundInFixed = CheckForPlayer();
            Idle_ShootLogic(playerFoundInFixed);
        }
    }

    // ★★★ Update() 関数 ★★★
    void Update()
    {
        if (player == null || agent == null || animator == null || isReloading) return; // 💡 リロード中は処理をスキップ
        animator.SetFloat("Speed", 0f);

        bool playerFound = CheckForPlayer();

        if (currentState == EnemyState.Idle)
        {
            IdleLogic(playerFound);
        }
    }

    // ★★★ Idle (待機・見回し) ロジック ★★★
    void IdleLogic(bool playerFound)
    {
        if (playerFound)
        {
            TransitionToIdle_Shoot();
            return;
        }

        // ランダムな見回し処理
        if (Time.time > nextRotationTime)
        {
            nextRotationTime = Time.time + Random.Range(3f, 6f);
            float randomAngle = Random.Range(0f, 360f);
            Quaternion targetRotation = Quaternion.Euler(0, randomAngle, 0);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // ★★★ Idle_Shoot (照準合わせ) ロジック ★★★
    void Idle_ShootLogic(bool playerFound)
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float maxDegreesPerFrame = rotationSpeed * 30f * Time.fixedDeltaTime;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, maxDegreesPerFrame);

        // 💡 向きがほぼ完了したらAttackへ移行
        if (Quaternion.Angle(transform.rotation, lookRotation) < 15f)
        {
            TransitionToAttack();
        }

        // プレイヤーを見失ったらIdleに戻る
        if (!playerFound)
        {
            TransitionToIdle();
        }
    }

    // ★★★ プレイヤー視界判定 ★★★
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

        // Raycastで障害物チェック
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
        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToIdle_Shoot");
        // 💡 StartReload/FinishReload 関連のInvokeキャンセルを削除

        currentState = EnemyState.Idle;
        animator.SetBool("IsAiming", false);
        nextRotationTime = Time.time + Random.Range(3f, 6f);
    }

    void TransitionToIdle_Shoot()
    {
        currentState = EnemyState.Idle_Shoot;
        animator.SetBool("IsAiming", true);
    }

    // ★★★ TransitionToAttack ★★★
    void TransitionToAttack()
    {
        // 💡 弾薬チェックと StartReload() の呼び出しを削除

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
        // 💡 弾切れの場合は即座に処理を終了
        if (currentAmmo <= 0) return;

        currentAmmo--; // 💡 弾薬を消費

        // 💡 弾薬消費のロジックを削除
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

    // 💡 StartReload/FinishReload 関数を削除
    // ... (OnDisable, OnDestroy は変更なし) ...
}