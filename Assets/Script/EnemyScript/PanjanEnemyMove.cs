using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // 死亡した場合のスクリプト
    public EnemyDaed enemyDaed;
    // 自爆攻撃
    public PanjanExplosion explosion;

    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();

    }

    void Update()
    {
        if (enemyDaed.Dead || explosion.Explos)
        {
            navMeshAgent.destination = this.gameObject.transform.position;
            StartCoroutine(PanDeath());
        }
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

    private IEnumerator PanDeath()
    {
        yield return new WaitForSeconds(2);
        Debug.Log("mimimim");
        Destroy(gameObject);
    }
}