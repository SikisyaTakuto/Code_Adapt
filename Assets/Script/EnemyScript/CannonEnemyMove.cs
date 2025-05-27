using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class CannonEnemyMove : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();
    }

    // Player���߂Â����ꍇ
    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player")
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
