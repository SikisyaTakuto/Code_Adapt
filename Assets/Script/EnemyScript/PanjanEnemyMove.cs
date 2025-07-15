using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // ���S�����ꍇ�̃X�N���v�g
    public EnemyDaed enemyDaed;
    // �����U��
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

    // Player���߂Â����ꍇ
    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player" && !enemyDaed.Dead)
        {
            // Player��ǂ�������
            navMeshAgent.destination = collider.gameObject.transform.position;
        }
    }

    // Player�����ꂽ�ꍇ
    public void OnLoseObject(Collider collider)
    {
        // Player���͈͊O�ɏo���Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // ���̏�Ŏ~�܂�
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