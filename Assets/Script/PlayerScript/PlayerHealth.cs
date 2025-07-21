using UnityEngine;
using UnityEngine.UI; // Slider �� Text ���g������
using System; // Action ���g������

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("�v���C���[�̍ő�HP�B")]
    public float maxHealth = 1000f;
    [Tooltip("�v���C���[�̌��݂�HP�B")]
    public float currentHealth;

    [Header("UI References")]
    [Tooltip("UI���HP�Q�[�W�iSlider�j�ւ̎Q�ƁB")]
    public Slider hpSlider; // HP UI Slider�ւ̎Q��
    [Tooltip("UI���HP�e�L�X�g�\���iText�j�ւ̎Q�ƁB")]
    public Text hpText; // ���ǉ�: HP�e�L�X�g�\���i��: 1000/1000�j�ւ̎Q��

    public event Action onHealthDamaged; // HP���_���[�W���󂯂��Ƃ��ɔ��΂���C�x���g

    void Awake()
    {
        currentHealth = maxHealth;
        // ����������UI���X�V
        UpdateHealthUI();
    }

    /// <summary>
    /// �v���C���[���_���[�W���󂯂鏈���B
    /// </summary>
    /// <param name="amount">�󂯂�_���[�W�ʁB</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return; // �_���[�W�ʂ�0�ȉ��̏ꍇ�͏������Ȃ�

        float previousHealth = currentHealth; // �_���[�W�O��HP���L�^
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // HP��0����ő�HP�͈̔͂ɐ���

        // UI���X�V
        UpdateHealthUI();

        // ���ۂ�HP���������ꍇ�ɂ̂݃C�x���g�𔭉�
        if (currentHealth < previousHealth)
        {
            onHealthDamaged?.Invoke();
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Player defeated!"); // �v���C���[���|���ꂽ���Ƃ����O�ɏo��
            // �����ŃQ�[���I�[�o�[�����Ȃǂ��Ăяo��
            // ��: SceneManager.LoadScene("GameOverScene");
            // �܂��́A�v���C���[�I�u�W�F�N�g���A�N�e�B�u�ɂ���Ȃ�
            // gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// �v���C���[��HP���񕜂��鏈���B
    /// </summary>
    /// <param name="amount">�񕜂���HP�ʁB</param>
    public void Heal(float amount)
    {
        if (amount <= 0) return; // �񕜗ʂ�0�ȉ��̏ꍇ�͏������Ȃ�

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // HP��0����ő�HP�͈̔͂ɐ���

        // UI���X�V
        UpdateHealthUI();
    }

    /// <summary>
    /// HP�o�[��HP�e�L�X�g��UI���X�V����B
    /// </summary>
    void UpdateHealthUI()
    {
        // Slider�����蓖�Ă��Ă���΍X�V
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth; // �X���C�_�[�̍ő�l��ݒ�
            hpSlider.value = currentHealth; // �X���C�_�[�̌��ݒl��ݒ�
        }

        // Text�����蓖�Ă��Ă���΍X�V
        if (hpText != null)
        {
            // ���݂�HP�ƍő�HP���u���݂�HP/�ő�HP�v�̌`���ŕ\��
            // ":F0" �͏����_�ȉ���\�����Ȃ��t�H�[�}�b�g�w��q
            hpText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
}
