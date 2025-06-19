using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // 自爆時間
    public float BombTime;
    // 死亡した場合のスクリプト
    public EnemyDaed enemyDaed;
    // 自爆攻撃
    public PanjanExplosion explosion;

    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();

        enemyDaed = GetComponent<EnemyDaed>();

        explosion = GetComponent<PanjanExplosion>();
    }

    void Update()
    {
        
    }

    // Playerが近づいた場合
    public void OnDetectObject(Collider collider)
    {
        // Playerが範囲内に入ったとき
        if (collider.gameObject.tag == "Player" && !enemyDaed.Dead)
        {
            // Playerを追いかける
            navMeshAgent.destination = collider.gameObject.transform.position;
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