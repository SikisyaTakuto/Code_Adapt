using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // ��������
    public float BombTime;
    // ���S�����ꍇ�̃X�N���v�g
    public EnemyDaed enemyDaed;
    // �����U��
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
}