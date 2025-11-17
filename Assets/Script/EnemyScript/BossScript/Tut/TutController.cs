using UnityEngine;
using UnityEngine.AI;

public class TutController : MonoBehaviour
{
    [Header("Settings")]
    public float attackRange = 3f;        // 攻撃範囲
    public float prepareTime = 2f;        // 攻撃前に停止する時間
    public float cooldownTime = 1f;       // ダメージ後待機時間
    public int damage = 10;               // 与えるダメージ

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
                ChasePlayer();   // 追跡処理
                break;

            case State.Preparing:
                PreparingAttack();  // 攻撃準備
                break;

            case State.Cooldown:
                Cooldown();  // 攻撃後待機
                break;
        }
    }

    // プレイヤー追跡処理
    private void ChasePlayer()
    {
        // プレイヤーに向かって移動
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // 距離が攻撃範囲以内 → 攻撃準備へ
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            state = State.Preparing;
            timer = 0f;
            agent.isStopped = true; // その場で停止
        }
    }

    // 攻撃準備
    private void PreparingAttack()
    {
        timer += Time.deltaTime;

        // 準備時間が経過したら攻撃してクールダウンへ
        if (timer >= prepareTime)
        {
            Debug.Log("ダメージ: " + damage);  // ダメージ処理
            timer = 0f;
            state = State.Cooldown;  // 攻撃後待機へ
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