using UnityEngine;

public class Cannonball : MonoBehaviour
{
    public float damageAmount = 50f;
    public string targetTag = "Player";
    public float lifeTime = 5.0f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                Debug.Log($"�C�e�� {other.name} �� {damageAmount} �_���[�W��^���܂����B");
            }
            Destroy(gameObject); // �Փ˂��������
        }
    }
}