using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float currentHealth = 100f; // 敵の現在の体力

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        // 破壊エフェクトやサウンドなどを再生
        Destroy(gameObject); // 敵を破壊
    }
}