using UnityEngine;
using UnityEngine.AI;

public class FlyEnemyMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Player‚ª‹ß‚Ã‚¢‚½ê‡
    public void OnDetectObject(Collider collider)
    {
        // Player‚ª”ÍˆÍ“à‚É“ü‚Á‚½‚Æ‚«
        if (collider.gameObject.tag == "Player")
        {
            // UŒ‚‚·‚é

        }
    }
}
