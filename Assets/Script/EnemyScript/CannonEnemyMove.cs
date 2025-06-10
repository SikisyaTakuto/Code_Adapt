using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class CannonEnemyMove : MonoBehaviour
{
    // Playerの方向に向く変数
    public Transform target;
    // Playerを追いかける移動速度
    public float moveSpeed;
    // Playerと保つ距離
    public float stopDistance;
    // Playerを索敵する範囲
    public float moveDistance;
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // 目的地の配列
    [SerializeField] private Transform[] waypointArray;
    // 現在の目的地
    private int currentWaypointIndex = 0;
    // 死亡した場合のスクリプト
    public EnemyDaed enemyDaed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();
        // 最初の目的地
        navMeshAgent.SetDestination(waypointArray[currentWaypointIndex].position);

        enemyDaed = GetComponent<EnemyDaed>();
    }

    void Update()
    {
        // 変数 targetPos を作成してターゲットオブジェクトの座標を格納
        Vector3 targetPos = target.position;

        // 生存している場合
        if (!enemyDaed.Dead)
        {
            // 自分自身のY座標を変数 target のY座標に格納
            //（ターゲットオブジェクトのX、Z座標のみ参照）
            targetPos.y = transform.position.y;

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                // 目的地の番号を１更新
                currentWaypointIndex = (currentWaypointIndex + 1) % waypointArray.Length;
                // 目的地を次の場所に設定
                navMeshAgent.SetDestination(waypointArray[currentWaypointIndex].position);
            }

            // 変数 distance を作成してオブジェクトの位置とターゲットオブジェクトの距離を格納
            float distance = Vector3.Distance(transform.position, target.position);
            // オブジェクトとターゲットオブジェクトの距離判定
            // 変数 distance（ターゲットオブジェクトとオブジェクトの距離）が変数 moveDistance の値より小さければ
            // さらに変数 distance が変数 stopDistance の値よりも大きい場合
            if (distance < moveDistance && distance > stopDistance)
            {
                // オブジェクトを変数 targetPos の座標方向に向かせる
                transform.LookAt(targetPos);
                // 変数 moveSpeed を乗算した速度でオブジェクトを前方向に移動する
                transform.position = transform.position + transform.forward * moveSpeed * Time.deltaTime;
            }
        }
        else
        {
            // その場で止まる
            ZeroSpeed();
        }
    }

    // Playerが近づいた場合
    public void OnDetectObject(Collider collider)
    {
        if (!enemyDaed.Dead)
        {
            // Playerが範囲内に入ったとき
            if (collider.gameObject.tag == "Player")
            {
                // Playerを攻撃
                transform.LookAt(target);
                navMeshAgent.speed = 0f;
            }
        }
    }

    // Playerが離れた場合
    public void OnLoseObject(Collider collider)
    {
        // Playerが範囲外に出たとき
        if (collider.gameObject.tag == "Player")
        {
            // その場で止まる
            navMeshAgent.speed = 50f;
        }
    }

    private void ZeroSpeed()
    {
        navMeshAgent.speed = 0f;
        moveSpeed = 0f;
    }
}
