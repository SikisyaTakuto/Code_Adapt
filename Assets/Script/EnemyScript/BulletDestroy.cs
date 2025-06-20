using UnityEngine;
using UnityEngine.AI;

public class BulletDestroy : MonoBehaviour
{
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            // Player�ɓ���������e������
            Destroy(gameObject);
            Debug.Log("�e���폜");
        }
    }
}
