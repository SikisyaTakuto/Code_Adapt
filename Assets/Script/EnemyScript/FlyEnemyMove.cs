using UnityEngine;
using UnityEngine.AI;

public class FlyEnemyMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Player���߂Â����ꍇ
    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // �U������

        }
    }
}
