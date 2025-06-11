using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // ��������
    private float BombTime = 10.0f;
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // ���S�����ꍇ�̃X�N���v�g
    public EnemyDaed enemyDaed;

    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();

        enemyDaed = GetComponent<EnemyDaed>();
    }

    void Update()
    {
        
    }

    // Player���߂Â����ꍇ
    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // Player��ǂ�������
            navMeshAgent.destination = collider.gameObject.transform.position;

            Destroy(gameObject, BombTime);
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
}