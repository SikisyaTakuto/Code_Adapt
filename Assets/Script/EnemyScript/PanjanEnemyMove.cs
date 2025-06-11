using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // 自爆時間
    private float BombTime = 10.0f;
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // 死亡した場合のスクリプト
    public EnemyDaed enemyDaed;

    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();

        enemyDaed = GetComponent<EnemyDaed>();
    }

    void Update()
    {
        
    }

    // Playerが近づいた場合
    public void OnDetectObject(Collider collider)
    {
        // Playerが範囲内に入ったとき
        if (collider.gameObject.tag == "Player")
        {
            // Playerを追いかける
            navMeshAgent.destination = collider.gameObject.transform.position;

            Destroy(gameObject, BombTime);
        }
    }

    // Playerが離れた場合
    public void OnLoseObject(Collider collider)
    {
        // Playerが範囲外に出たとき
        if (collider.gameObject.tag == "Player")
        {
            // その場で止まる
            navMeshAgent.destination = this.gameObject.transform.position;
        }
    }
}