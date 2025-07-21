using UnityEngine;
using UnityEngine.UI; // Slider を使うため
using System; // Action を使うため

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Tooltip("UI上のHPゲージ（Slider）への参照。")]
    public Slider hpSlider; // Reference to the HP UI Slider

    public event Action onHealthDamaged; // ★追加: HPがダメージを受けたときに発火するイベント

    void Awake()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;

        float previousHealth = currentHealth;
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        // 実際にHPが減った場合にのみイベントを発火
        if (currentHealth < previousHealth)
        {
            onHealthDamaged?.Invoke();
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Player defeated!");
            // ここでゲームオーバー処理などを呼び出す
            // 例: SceneManager.LoadScene("GameOverScene");
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHealth / maxHealth;
        }
    }
}