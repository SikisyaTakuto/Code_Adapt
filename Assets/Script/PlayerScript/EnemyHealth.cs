using UnityEngine;
using System; // Action���g�����߂ɒǉ�

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public Action onDeath; // �G���|���ꂽ�Ƃ��ɔ��΂���C�x���g

    void Awake()
    {
        currentHealth = maxHealth;
        // �G���������ꂽ��ATag��"Enemy"�ɐݒ肵�Ă���
        gameObject.tag = "Enemy";
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} �͓|��܂����I");
        onDeath?.Invoke(); // �G���|�ꂽ���Ƃ�ʒm
        Destroy(gameObject); // �I�u�W�F�N�g��j��
    }
}