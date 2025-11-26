using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    public ElsController boss;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            boss.isActive = true;
            Debug.Log("Boss Activated!");
        }
    }
}
