using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaserAI : MonoBehaviour
{
    // ★ 宣言済み変数を使用
    private Animator anim;
    private NavMeshAgent agent;

    [SerializeField] private Transform target;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("移動設定")]
    [SerializeField] private float dashSpeed = 6f;

    // Animator Parameters
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsAimingParam = Animator.StringToHash("IsAiming");
    private static readonly int IsBackpedalingParam = Animator.StringToHash("IsBackpedaling");

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>(); // ★ ここで一度だけ代入する

        // Agentが取得できなかったり、無効な場合は処理を中断
        if (agent == null) return;

        agent.updateRotation = false;

        // ★★★ NavMeshAgent 初期化失敗対策 (修正済み) ★★★
        if (agent.isActiveAndEnabled)
        {
            NavMeshHit hit;
            // エージェントがNavMeshの有効な位置にあるか確認し、補正
            if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }
        // ★★★ 対策コードはここまで ★★★

        if (target == null && GameObject.FindWithTag("Player") != null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
    }

    void Update()
    {
        if (target == null) return;
        // Start()でnullチェックをしているため、ここで冗長なagentのnullチェックは不要だが、念のため残す
        if (agent == null || !agent.isActiveAndEnabled) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        LookAtTarget();
        HandleChase(distanceToTarget);

        agent.stoppingDistance = 0.1f;

        UpdateAnimatorParameters();
    }

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
    // AI行動ロジック
    // --------------------------------------------------------------------------------

    void HandleChase(float distanceToTarget)
    {
        // 常に全力で走る (Run)
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
        anim.SetFloat(SpeedParam, currentVelocity, 0.1f, Time.deltaTime);
    }
}