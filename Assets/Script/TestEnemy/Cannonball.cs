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
                Debug.Log($"–C’e‚ª {other.name} ‚É {damageAmount} ƒ_ƒ[ƒW‚ğ—^‚¦‚Ü‚µ‚½B");
            }
            Destroy(gameObject); // Õ“Ë‚µ‚½‚çÁ–Å
        }
    }
}