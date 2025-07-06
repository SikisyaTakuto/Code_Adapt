using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float currentHealth = 100f; // �G�̌��݂̗̑�

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
        // �j��G�t�F�N�g��T�E���h�Ȃǂ��Đ�
        Destroy(gameObject); // �G��j��
    }
}