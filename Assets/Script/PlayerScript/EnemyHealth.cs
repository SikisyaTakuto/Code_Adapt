using UnityEngine;
using System; // Actionを使うために追加

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public Action onDeath; // 敵が倒されたときに発火するイベント

    void Awake()
    {
        currentHealth = maxHealth;
        // 敵が生成されたら、Tagを"Enemy"に設定しておく
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
        Debug.Log($"{gameObject.name} は倒れました！");
        onDeath?.Invoke(); // 敵が倒れたことを通知
        Destroy(gameObject); // オブジェクトを破棄
    }
}