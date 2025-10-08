using UnityEngine;
using UnityEngine.UI; // Slider���g�����߂ɕK�v

public class EnemyHPBarDisplay : MonoBehaviour
{
    [Header("HP Bar Settings")]
    [Tooltip("�\������HP�o�[��Slider�R���|�[�l���g�B")]
    public Slider hpSlider;
    [Tooltip("�G��GameObject�̏㕔����̃I�t�Z�b�g�B")]
    public Vector3 offset = new Vector3(0, 1.5f, 0); // HP�o�[�̕\���ʒu�����p

    private EnemyHealth enemyHealth; // �G��HP�����擾���邽�߂̎Q��
    private Camera mainCamera;       // HP�o�[����ɃJ�����̕��֌����邽�߂ɕK�v

    void Awake()
    {
        // �e�I�u�W�F�N�g�i�G�{�́j����EnemyHealth�R���|�[�l���g���擾
        enemyHealth = GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("EnemyHealth�R���|�[�l���g���e�I�u�W�F�N�g�Ɍ�����܂���I");
            enabled = false; // �X�N���v�g�𖳌��ɂ���
            return;
        }

        // �V�[�����̃��C���J�������擾
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("���C���J������������܂���I�V�[����Tag: MainCamera���t�^���ꂽ�J���������邱�Ƃ��m�F���Ă��������B");
            enabled = false; // �X�N���v�g�𖳌��ɂ���
            return;
        }

        // HP�o�[�̃X���C�_�[���ݒ肳��Ă��邩�m�F
        if (hpSlider == null)
        {
            Debug.LogError("HP Slider���ݒ肳��Ă��܂���I�C���X�y�N�^�[�Őݒ肵�Ă��������B");
            enabled = false; // �X�N���v�g�𖳌��ɂ���
            return;
        }

        // ����HP��ݒ�
        UpdateHPBar();
    }

    void Update()
    {
        // HP�o�[�̈ʒu��G�̓���ɒǏ]������
        // transform.position��HP�o�[Canvas��RectTransform�̈ʒu�ɂȂ�
        transform.position = transform.parent.position + offset;

        // HP�o�[����ɃJ�����̕��֌����� (LookAt���\�b�h��Z�����J�����̕����������悤�ɉ�]������)
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);

        // HP�o�[�̒l���X�V
        UpdateHPBar();

        // �G��HP��0�ɂȂ�����HP�o�[����\���ɂ���
        if (enemyHealth.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    // HP�o�[�̒l���X�V���郁�\�b�h
    void UpdateHPBar()
    {
        // currentHealth / maxHealth ��HP�̊������v�Z���ASlider��value�ɐݒ�
        hpSlider.value = enemyHealth.currentHealth / enemyHealth.maxHealth;
    }
}