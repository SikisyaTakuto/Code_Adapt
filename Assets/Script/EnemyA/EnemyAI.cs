using UnityEngine;

public class EnemyAI: MonoBehaviour
{
    private Animator anim;
    private EnemyShooter enemyShooter; // 弾の発射を担うコンポーネント

    public Transform playerTarget;
    public float detectionRange = 10f;

    // 🔥 射撃間隔の設定
    public float fireRate = 2.0f;
    private float nextFireTime;

    private const string IsAimingParam = "IsAiming";
    private const string FireTriggerParam = "FireTrigger";

    void Start()
    {
        anim = GetComponent<Animator>();
        enemyShooter = GetComponent<EnemyShooter>();
        // プレイヤーのTransformを取得 (省略)

        nextFireTime = Time.time; // すぐに射撃可能にする
    }

    void Update()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 1. 索敵ロジック
        if (distanceToPlayer <= detectionRange)
        {
            // プレイヤーの方を向く
            LookAtPlayer();

            // 構えるアニメーションを開始 (待機→構える)
            anim.SetBool(IsAimingParam, true);

            // 2. 射撃タイミングのチェック
            if (Time.time >= nextFireTime)
            {
                // 撃つモーションへ移行
                anim.SetTrigger(FireTriggerParam);

                // 次の射撃可能時間を更新
                nextFireTime = Time.time + fireRate;

                // 🔥 注意: 実際の弾の発射は、アニメーションイベントで行う！
            }
        }
        else
        {
            // プレイヤーが範囲外に出たら、構えるアニメーションを終了 (構える→待機)
            anim.SetBool(IsAimingParam, false);
        }
    }

    // LookAtPlayer() 関数は以前の内容と同じ
    void LookAtPlayer()
    {
        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
}