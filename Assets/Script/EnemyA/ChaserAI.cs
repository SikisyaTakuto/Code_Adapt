using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaserAI_Stateful : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;

    [SerializeField] private Transform target;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("移動設定")]
    [SerializeField] private float dashSpeed = 6f;      // 追跡時の走行速度
    [SerializeField] private float stopDistance = 0.5f; // この距離で停止・待機

    [Header("追跡開始設定")]
    [SerializeField] private float chaseStartDistance = 3f; // この距離以上離れたら追跡開始

    // AIの現在の状態を保持するフラグ
    private bool isChasing = false;

    // Animator Parameters (変更なし)
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsAimingParam = Animator.StringToHash("IsAiming");
    private static readonly int IsBackpedalingParam = Animator.StringToHash("IsBackpedaling");

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (agent == null || anim == null) return;

        agent.updateRotation = false;
        agent.speed = dashSpeed;
        agent.stoppingDistance = stopDistance; // NavMeshAgentの停止距離を設定しておく

        // 省略: NavMesh配置とターゲット検索の初期化処理はそのまま
        if (agent.isActiveAndEnabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }

        if (target == null && GameObject.FindWithTag("Player") != null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
    }

    void Update()
    {
        if (target == null || agent == null || !agent.isActiveAndEnabled) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        LookAtTarget();
        HandleState(distanceToTarget);
        UpdateAnimatorParameters();
    }

    // ターゲットを見る処理（変更なし）
    void LookAtTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // --------------------------------------------------------------------------------
    // AI行動ロジック (ステートベース制御)
    // --------------------------------------------------------------------------------

    void HandleState(float distanceToTarget)
    {
        // アニメーションパラメータのリセット（現在の実装方針に合わせる）
        anim.SetBool(IsAimingParam, false);
        anim.SetBool(IsBackpedalingParam, false);

        if (!agent.isOnNavMesh) return;

        // 【ステート遷移判定】

        // 追跡中 かつ 停止距離以下になったら -> 停止ステートへ
        if (isChasing && distanceToTarget <= stopDistance)
        {
            isChasing = false;
        }
        // 停止中 かつ 追跡開始距離以上離れたら -> 追跡ステートへ
        else if (!isChasing && distanceToTarget > chaseStartDistance)
        {
            isChasing = true;
        }

        // 【現在のステートに基づく行動実行】

        if (isChasing)
        {
            // 追跡ステート: 目的地を設定し、速度をフルスピードにする
            agent.speed = dashSpeed;
            // 目的地を毎フレーム更新することで追跡を継続
            agent.SetDestination(target.position);
        }
        else
        {
            // 停止ステート: 現在地を目的地に設定し、移動を完全に停止させる
            agent.speed = 0f;
            agent.SetDestination(transform.position);
        }
    }

    // --------------------------------------------------------------------------------
    // アニメーターパラメーター更新
    // --------------------------------------------------------------------------------

    void UpdateAnimatorParameters()
    {
        // 実際の移動速度を取得
        // agent.velocity は NavMesh Agentの実際の移動速度ベクトル
        float currentVelocityMagnitude = agent.velocity.magnitude;

        float normalizedSpeed = 0f;
        if (dashSpeed > 0.01f)
        {
            normalizedSpeed = Mathf.Clamp01(currentVelocityMagnitude / dashSpeed);
        }

        // アニメーターに速度をセット (DampTime 0.1f でスムーズに)
        anim.SetFloat(SpeedParam, normalizedSpeed, 0.1f, Time.deltaTime);
    }
}