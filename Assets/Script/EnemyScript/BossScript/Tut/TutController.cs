using UnityEngine;
using UnityEngine.AI;

public class TutController : MonoBehaviour
{
    [Header("Settings")]
    public float attackRange = 3f;        // 攻撃範囲
    public float prepareTime = 2f;        // 攻撃前に停止する時間
    public float cooldownTime = 1f;       // ダメージ後待機時間
    public int damage = 10;               // 与えるダメージ
    public float stopDistance = 2f;       // この距離以内なら止まる

    private Transform player;
    private NavMeshAgent agent;

    private enum State
    {
        Chase,      // 追跡
        Preparing,  // 攻撃準備
        Cooldown    // 攻撃後の待機
    }

    private State state = State.Chase;
    private float timer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        switch (state)
        {
            case State.Chase:
                ChasePlayer();      // 追跡処理
                break;

            case State.Preparing:
                PreparingAttack();  // 攻撃準備
                break;

            case State.Cooldown:
                Cooldown();         // 攻撃後待機
                break;
        }
    }

    // プレイヤー追跡処理
    private void ChasePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // プレイヤーが停止距離以上なら追跡を続ける
        if (distance > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // 停止距離以内ならその場で止まる
            agent.isStopped = true;

            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0; // 水平方向のみで回転

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * 3f   // 回転スピード
            );
        }

        // 前方判定
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dirToPlayer);
        bool isInFront = dot > 0.7f;

        // 攻撃準備に移行する条件
        if (distance <= attackRange && isInFront)
        {
            state = State.Preparing;
            timer = 0f;
            agent.isStopped = true;
        }
    }

    // 攻撃準備
    private void PreparingAttack()
    {
        timer += Time.deltaTime;

        // 準備時間が経過したら攻撃してクールダウンへ
        if (timer >= prepareTime)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPlayer);

            if (dot > 0.7f)
            {
                Debug.Log("ダメージ: " + damage);
            }
            else
            {
                Debug.Log("前方にいないのでミス");
            }

            timer = 0f;
            state = State.Cooldown;
        }
    }

    // 攻撃後の待機
    private void Cooldown()
    {
        timer += Time.deltaTime;

        // クールダウン終了
        if (timer >= cooldownTime)
        {
            timer = 0f;
            state = State.Chase;  // 再追跡
        }
    }
}