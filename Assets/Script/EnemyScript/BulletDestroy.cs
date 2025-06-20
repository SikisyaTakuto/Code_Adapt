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
            // Player‚É“–‚½‚Á‚½‚ç’e‚ğÁ‚·
            Destroy(gameObject);
            Debug.Log("’e‚ğíœ");
        }
    }
}
