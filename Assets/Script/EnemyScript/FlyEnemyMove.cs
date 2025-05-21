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

    // Player‚ª‹ß‚Ã‚¢‚½ê‡
    public void OnDetectObject(Collider collider)
    {
        // Player‚ª”ÍˆÍ“à‚É“ü‚Á‚½‚Æ‚«
        if (collider.gameObject.tag == "Player")
        {
            // UŒ‚‚·‚é
            Debug.Log("a");
            speed = 0;
        }
    }
}
