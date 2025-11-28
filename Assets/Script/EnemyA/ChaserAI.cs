using UnityEngine;
using UnityEngine.AI;

public class ChaserAI : MonoBehaviour
{
    // --- 状態定義 ---
    // 💡 Reload ステートを削除
    public enum EnemyState { Idle, Chase, Attack, Reload }
    public EnemyState currentState = EnemyState.Idle;

    // --- AI 設定 ---
    public Transform player;
    public float sightRange = 15f;
    public float attackRange = 5f;
    public float rotationSpeed = 10f;

    // --- 攻撃設定 ---
    public int bulletsPerBurst = 3;
    public float timeBetweenShots = 0.1f;
    public float shootDuration = 0.5f;

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

    // ----------------------------------------------------------------------

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

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
        currentAmmo = maxAmmo; // 💡 弾薬を満タンに初期化
    }

    // ----------------------------------------------------------------------

    void Update()
    {
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
            case EnemyState.Reload: // 💡 Reload ステートの追加
                ReloadLogic();
                break;
        }

        // 💡 アニメーション制御（移動速度連動）
        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
    }

    // ----------------------------------------------------
    // --- ロジック関数 ---
    // ----------------------------------------------------
    // IdleLogic, ChaseLogic, AttackLogic は変更なし

    void IdleLogic(float distance)
    {
        if (distance <= sightRange)
        {
            TransitionToChase();
        }
    }

    void ChaseLogic(float distance)
    {
        agent.SetDestination(player.position);

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
    // --- 状態遷移関数 ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        currentState = EnemyState.Idle;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = true;
        }
        CancelInvoke("ShootBullet");
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    void TransitionToChase()
    {
        currentState = EnemyState.Chase;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
        if (animator != null) animator.SetBool("IsAiming", false);
        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAttackComplete");
    }

    void TransitionToAttack()
    {
        // 💡 弾切れの場合、AttackではなくReloadへ遷移
        if (currentAmmo <= 0)
        {
            TransitionToReload();
            return;
        }

        // 💡 弾薬チェックを削除
        currentState = EnemyState.Attack;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        if (animator != null)
        {
            // 🚨 修正: IsRunningをオフにして、Run/Idleステートへの影響を完全に断つ
            animator.SetBool("IsRunning", false);

            // 💡 Shootset アニメーションを起動・維持
            animator.SetBool("IsAiming", true);

            // 💡 射撃トリガーを即座に起動し、モーションに入らせる
            animator.SetTrigger("Shoot");
        }

        CancelInvoke("ShootBullet");

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
        // プレイヤーがまだ射程内にいればAttackを続行、いなければChaseへ戻る
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack(); // 攻撃を繰り返す
        }
        else
        {
            TransitionToChase(); // 追跡に戻る
        }
    }

    // ----------------------------------------------------
    // --- 弾丸生成処理 ---
    // ----------------------------------------------------

    public void ShootBullet()
    {
        if (currentAmmo <= 0) return; // 弾切れなら撃たない

        currentAmmo--; // 💡 弾薬を消費

        // 💡 弾薬消費ロジックを削除
        if (gameObject == null || !gameObject.activeInHierarchy || bulletPrefab == null || muzzlePoint == null)
        {
            return;
        }

        GameObject newBullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        newBullet.transform.parent = null;

        Debug.Log("弾が発射されました！");
    }

    void ReloadLogic()
    {
        // リロード中はアニメーションと時間待ちがメインなので、ここでは特に何もしません。
    }

    void TransitionToReload()
    {
        currentState = EnemyState.Reload;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false; // リロード中は停止
        }

        // 攻撃中のInvokeを全てキャンセル
        CancelInvoke("ShootBullet");
        CancelInvoke("TransitionToAttackComplete");

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            // 💡 Animator のリロードトリガーを発動！
            animator.SetTrigger("Reload");
        }

        Debug.Log("リロード開始... (" + reloadTime + "秒)");
        // リロード時間が経過したら FinishReload を呼び出す
        Invoke("FinishReload", reloadTime);
    }

    void FinishReload()
    {
        currentAmmo = maxAmmo;
        Debug.Log("リロード完了！");

        // 💡 リロードアニメーションが終了していることを確認する（安全のため）
        if (animator != null)
        {
            // Has Exit Timeで次のステートに戻るため、ここではTriggerのクリアは不要ですが、
            // 念のためアニメーションの状態を確実にリセットします。
            animator.SetBool("IsRunning", false);
        }

        // 完了後、次の行動を決定する
        float distance = Vector3.Distance(transform.position, player.position);

        // 以前のデバッグで確認した、即座にIdleに戻るのを防ぐためのロジック
        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack(); // 攻撃再開
        }
        else if (distance <= sightRange)
        {
            TransitionToChase(); // 追跡に戻る
        }
        else
        {
            TransitionToIdle(); // プレイヤーが視界外なら待機
        }
    }
}