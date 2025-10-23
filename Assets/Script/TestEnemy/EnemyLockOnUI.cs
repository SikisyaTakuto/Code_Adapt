using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ���b�N�I���Ώۂ̓G�̃��[���h���W��ǐՂ��AUI Image��G�̉�ʈʒu�ɕ\���E�Ǐ]������B
/// </summary>
public class EnemyLockOnUI : MonoBehaviour
{
    // ���b�N�I��UI�Ƃ��ċ@�\����Image�R���|�[�l���g
    private Image lockOnImage;

    // �ǐՂ���^�[�Q�b�g (�G��Transform)
    private Transform target;

    // �G�̃��[���h���W����ʂɕϊ����邽�߂�Camera
    private Camera mainCamera;

    // ��ʊO�ɏo�Ă��Ȃ����𔻒肷��}�[�W��
    private const float ScreenMargin = 50f;

    void Awake()
    {
        lockOnImage = GetComponent<Image>();
        mainCamera = Camera.main;

        if (lockOnImage == null || mainCamera == null)
        {
            Debug.LogError("EnemyLockOnUI: �K�v�ȃR���|�[�l���g��������܂���B");
            enabled = false;
        }

        lockOnImage.enabled = false;
    }

    /// <summary>
    /// �ǐՂ���^�[�Q�b�g��ݒ肵�AUI���A�N�e�B�u�ɂ���B
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        lockOnImage.enabled = true; // UI��\���J�n
    }

    void Update()
    {
        if (target == null)
        {
            // �^�[�Q�b�g�����Ȃ��Ȃ�����UI���\���ɂ��Ĕj��
            Destroy(gameObject);
            return;
        }

        if (mainCamera == null) return;

        // �^�[�Q�b�g�̃��[���h���W���X�N���[�����W�ɕϊ�
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

        // Z���W�i�J��������̋����j�����̏ꍇ�A�^�[�Q�b�g�̓J�����̌��ɂ���
        if (screenPos.z < 0)
        {
            lockOnImage.enabled = false;
            return;
        }

        // �^�[�Q�b�g����ʊO�ɂ��邩�`�F�b�N
        Rect screenRect = new Rect(ScreenMargin, ScreenMargin, Screen.width - ScreenMargin * 2, Screen.height - ScreenMargin * 2);

        if (!screenRect.Contains(screenPos))
        {
            lockOnImage.enabled = false;
            return;
        }

        // ��ʓ��ɂ���ꍇ��UI��\�����A�ʒu���X�V
        lockOnImage.enabled = true;

        // UI�̈ʒu���X�N���[�����W�ɐݒ�
        transform.position = screenPos;
    }
}