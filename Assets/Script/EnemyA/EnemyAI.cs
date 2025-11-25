using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // --- 状態定義 ---
    public enum EnemyState { Idle, Idle_Shoot, Attack }
    public EnemyState currentState = EnemyState.Idle;

    // --- AI 設定 ---
    public float sightRange = 15f;
    public float viewAngle = 90f;
    public float rotationSpeed = 3f;
    public float shootDuration = 1.0f;

    // ★★★ 銃弾の発射に必要な参照 ★★★
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;

    // --- コンポーネント ---
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private float nextRotationTime;

    // 💡 視界判定の結果を保持する変数 (Updateで計算され、LateUpdateで参照される)
    private bool isPlayerInSight = false;

    // 💡 ステートに入った時刻を記録
    private float stateEnterTime;

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
            agent.updateRotation = false; // 回転無効化
            agent.isStopped = true;       // 移動停止
        }

        nextRotationTime = Time.time + Random.Range(3f, 6f);
    }

    // ★★★ Update() 関数 ★★★
    void Update()
    {
        if (player == null || agent == null || animator == null) return;

        animator.SetFloat("Speed", 0f);

        // 💡 視界判定を Update で一回だけ実行し、メンバー変数に保存
        isPlayerInSight = CheckForPlayer();

        // Idle Logic のみ Update で実行
        if (currentState == EnemyState.Idle)
        {
            // 💡 メンバー変数を渡すように変更
            IdleLogic(isPlayerInSight);
        }
    }

    // ★★★ LateUpdate() 関数 ★★★
    void LateUpdate()
    {
        if (player == null || agent == null || animator == null) return;

        // Idle_Shoot ステートでのみ実行
        if (currentState == EnemyState.Idle_Shoot)
        {
            // 💡 引数なしで呼び出し
            Idle_ShootLogic();
        }
    }


    // ----------------------------------------------------
    // --- ロジック関数 ---
    // ----------------------------------------------------

    // 💡 引数ありのまま維持（既存のロジックを変更しないため）
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

    // 💡 引数なしに変更し、isPlayerInSight を直接参照
    void Idle_ShootLogic()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        // プレイヤーの方向へ滑らかに向きを変える（追尾回転）
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed * 2f);

        // 💡 3秒以上このステートに留まったら、何らかのブロックと見なし強制リセット
        if (Time.time > stateEnterTime + 3.0f)
        {
            Debug.LogError("Idle_Shoot滞在時間超過！強制的にIDLEへ戻ります。");
            TransitionToIdle();
            return;
        }

        // 回転が完了したと見なす角度差でAttackへ移行
        if (Quaternion.Angle(transform.rotation, lookRotation) < 25f)
        {
            TransitionToAttack();
        }

        // プレイヤーを見失ったらIdleに戻る
        // 💡 メンバー変数 isPlayerInSight を参照
        if (!isPlayerInSight)
        {
            TransitionToIdle();
        }
    }

    // CheckForPlayer() は変更なし

    bool CheckForPlayer()
    {
        if (player == null || agent == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 1. 距離と角度チェック
        if (distanceToPlayer > sightRange) return false;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > viewAngle / 2f) return false;

        RaycastHit hit;
        Vector3 eyePosition = transform.position + Vector3.up * (agent.height / 2f);

        // 2. 遮蔽物チェック (Raycast)
        if (Physics.Raycast(eyePosition, directionToPlayer.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.DrawLine(eyePosition, hit.point, Color.red);
                return true;
            }
            else
            {
                Debug.DrawLine(eyePosition, hit.point, Color.yellow);
                return false;
            }
        }

        return false;
    }


    // ----------------------------------------------------
    // --- 状態遷移関数 (変更なし) ---
    // ----------------------------------------------------

    void TransitionToIdle()
    {
        currentState = EnemyState.Idle;
        animator.SetBool("IsAiming", false);
        nextRotationTime = Time.time + Random.Range(3f, 6f);
    }

    void TransitionToIdle_Shoot()
    {
        currentState = EnemyState.Idle_Shoot;
        animator.SetBool("IsAiming", true);
        animator.ResetTrigger("Shoot");

        // 💡 ステートに入った時刻を記録
        stateEnterTime = Time.time;
        Debug.Log("==> IDLE_SHOOT: 構え、追尾開始");
    }

    void TransitionToAttack()
    {
        currentState = EnemyState.Attack;
        animator.SetTrigger("Shoot");
    }

    public void ShootBullet()
    {
        if (bulletPrefab == null || muzzlePoint == null)
        {
            Debug.LogError("弾丸プレハブまたは銃口が未設定です！");
            return;
        }

        Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        Debug.Log("弾が生成されました！");
    }
}