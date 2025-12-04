using TMPro;
using UnityEngine;
// 💡 修正点 1: NavMeshAgentを使用しないため、using UnityEngine.AI; を削除

public class MachineGunStaticAI : MonoBehaviour
{
    // --- 状態定義 ---
    // 💡 修正点 2: Chase状態を削除
    public enum EnemyState { Idle, Attack, Reload }
    public EnemyState currentState = EnemyState.Idle;

    // --- AI 設定 ---
    public Transform player;
    // 💡 修正点 3: ChaseStateがなくなったため、視界範囲と攻撃範囲を統合
    public float sightRange = 15f;
    public float attackRange = 10f; // 攻撃継続距離
    public float rotationSpeed = 10f;

    // --- 攻撃設定 (マシンガン連射仕様) ---
    public int bulletsPerBurst = 25;
    public float timeBetweenShots = 0.05f;
    public float shootDuration = 0.5f;

    public float bulletLaunchForce = 150f;
    public float fireSpread = 0.08f;

    // --- 弾薬とリロード設定 ---
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 3.0f;

    // ★★★ 銃弾の発射に必要な参照 ★★★
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // --- コンポーネント ---
    // 💡 修正点 4: NavMeshAgent agent; を削除
    private Animator animator;
    private EnemyHealth health;

    // ----------------------------------------------------------------------

    void Start()
    {
        // 💡 修正点 5: agent = GetComponent<NavMeshAgent>(); を削除
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        currentAmmo = maxAmmo;
        TransitionToIdle();
    }

    // ----------------------------------------------------------------------

    void Update()
    {
        // 🚨 死亡チェック: HPがゼロ以下なら即座に処理を終了
        if (health != null && health.currentHealth <= 0)
        {
            CancelInvoke();
            if (animator != null)
            {
                animator.SetBool("IsAiming", false);
                // 💡 修正: IsRunningの制御を削除
            }
            return;
        }

        if (player == null || animator == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- 状態遷移判定 ---
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleLogic(distanceToPlayer);
                break;
            // 💡 修正: ChaseLogicを削除
            case EnemyState.Attack:
                AttackLogic(distanceToPlayer);
                break;
            case EnemyState.Reload:
                // ReloadLogic は何もしない
                break;
        }

        // 💡 修正: アニメーション制御（移動速度連動）を削除
    }

    // ----------------------------------------------------
    // --- ロジック関数 ---
    // ----------------------------------------------------

    void IdleLogic(float distance)
    {
        // プレイヤーが視界に入ったら即攻撃へ
        if (distance <= sightRange)
        {
            TransitionToAttack();
        }
    }

    // 💡 修正: ChaseLogicを削除

    void AttackLogic(float distance)
    {
        // 攻撃中もプレイヤーの方向を追従
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        // プレイヤーが射程外に出たら待機に戻る
        if (distance > attackRange * 1.2f)
        {
            TransitionToIdle();
        }
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        currentState = EnemyState.Idle;
        // 💡 修正: NavMeshAgentの制御を全て削除
        CancelInvoke();
        if (animator != null) animator.SetBool("IsAiming", false);
    }

    // 💡 修正: TransitionToChase を削除

    void TransitionToAttack()
    {
        if (currentAmmo <= 0)
        {
            TransitionToReload();
            return;
        }

        currentState = EnemyState.Attack;

        // 💡 修正: NavMeshAgentの制御を全て削除

        if (animator != null)
        {
            // 💡 修正: IsRunningの制御を削除
            animator.SetBool("IsAiming", true);
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
        if (health != null && health.currentHealth <= 0) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // プレイヤーが射程内に留まっていれば、攻撃を繰り返す
        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack();
        }
        else
        {
            // 射程外に出たら待機に戻る
            TransitionToIdle();
        }
    }

    // ----------------------------------------------------
    // --- 弾丸生成処理 (変更なし) ---
    // ----------------------------------------------------

    public void ShootBullet()
    {
        if (health != null && health.currentHealth <= 0) return;
        if (currentAmmo <= 0) return;

        currentAmmo--;

        if (bulletPrefab == null || muzzlePoint == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-fireSpread, fireSpread),
            Random.Range(-fireSpread, fireSpread),
            Random.Range(-fireSpread, fireSpread)
        );

        GameObject newBullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        newBullet.transform.parent = null;

        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 fireDirection = muzzlePoint.forward + randomOffset;
            rb.AddForce(fireDirection.normalized * bulletLaunchForce, ForceMode.Impulse);
        }

        Debug.Log("弾が発射されました！ 残り弾薬: " + currentAmmo);
    }

    // ----------------------------------------------------
    // --- リロード処理 (NavMeshAgent制御を削除) ---
    // ----------------------------------------------------

    void ReloadLogic()
    {
        if (health != null && health.currentHealth <= 0) return;
    }

    void TransitionToReload()
    {
        currentState = EnemyState.Reload;

        // 💡 修正: NavMeshAgentの制御を全て削除

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

        // 💡 修正: IsRunningの制御を削除

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange * 1.2f)
        {
            TransitionToAttack();
        }
        else
        {
            TransitionToIdle();
        }
    }
}