using UnityEngine;
using UnityEngine.UI; // Slider ���g������
using System; // Action ���g������

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Tooltip("UI���HP�Q�[�W�iSlider�j�ւ̎Q�ƁB")]
    public Slider hpSlider; // Reference to the HP UI Slider

    public event Action onHealthDamaged; // ���ǉ�: HP���_���[�W���󂯂��Ƃ��ɔ��΂���C�x���g

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

        // ���ۂ�HP���������ꍇ�ɂ̂݃C�x���g�𔭉�
        if (currentHealth < previousHealth)
        {
            onHealthDamaged?.Invoke();
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Player defeated!");
            // �����ŃQ�[���I�[�o�[�����Ȃǂ��Ăяo��
            // ��: SceneManager.LoadScene("GameOverScene");
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