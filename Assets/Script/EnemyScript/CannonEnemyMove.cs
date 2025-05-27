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

    // Player‚ª‹ß‚Ã‚¢‚½ê‡
    public void OnDetectObject(Collider collider)
    {
        // Player‚ª”ÍˆÍ“à‚É“ü‚Á‚½‚Æ‚«
        if (collider.gameObject.tag == "Player")
        {
            // Player‚ğ’Ç‚¢‚©‚¯‚é
            navMeshAgent.destination = collider.gameObject.transform.position;
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
