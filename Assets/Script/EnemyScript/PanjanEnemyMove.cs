using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class PanjanEnemyMove : MonoBehaviour
{
    // ©”šŠÔ
    private float BombTime = 10.0f;
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;

    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        
    }

    // Player‚ª‹ß‚Ã‚¢‚½ê‡
    public void OnDetectObject(Collider collider)
    {
        // Player‚ª”ÍˆÍ“à‚É“ü‚Á‚½‚Æ‚«
        if (collider.gameObject.tag == "Player")
        {
            // Player‚ğ’Ç‚¢‚©‚¯‚é
            navMeshAgent.destination = collider.gameObject.transform.position;

            Destroy(gameObject, BombTime);
        }
    }

    // Player‚ª—£‚ê‚½ê‡
    public void OnLoseObject(Collider collider)
    {
        // Player‚ª”ÍˆÍŠO‚Éo‚½‚Æ‚«
        if (collider.gameObject.tag == "Player")
        {
            // ‚»‚Ìê‚Å~‚Ü‚é
            navMeshAgent.destination = this.gameObject.transform.position;
        }
    }
}