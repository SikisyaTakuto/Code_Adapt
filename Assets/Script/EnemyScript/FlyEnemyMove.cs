using UnityEngine;
using UnityEngine.AI;

public class FlyEnemyMove : MonoBehaviour
{
    public float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = 0;
    }

    // Player���߂Â����ꍇ
    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // �U������
            Debug.Log("a");
            speed = 0;
        }
    }
}
