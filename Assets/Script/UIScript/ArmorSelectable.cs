using UnityEngine;
using UnityEngine.EventSystems; // UI�C�x���g���������߂ɕK�v

public class ArmorSelectable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ArmorData armorData; // ���̃��f�����\���A�[�}�[�̃f�[�^
    public ArmorSelectUI armorSelectUI; // ArmorSelectUI�ւ̎Q��

    private Vector3 originalScale;
    private Color originalColor;
    private Renderer _renderer; // ���f����Renderer

    private const float HOVER_SCALE_FACTOR = 1.1f; // �z�o�[���̊g�嗦
    private const float ANIMATION_DURATION = 0.1f; // �A�j���[�V��������

    void Awake()
    {
        originalScale = transform.localScale;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            originalColor = _renderer.material.color; // ���f���̃}�e���A���̐F��ۑ�
        }
    }

    // ���f�����N���b�N���ꂽ��
    public void OnPointerClick(PointerEventData eventData)
    {
        if (armorSelectUI != null)
        {
            armorSelectUI.OnArmorClicked(armorData);
        }
    }

    // �}�E�X�J�[�\�������f���ɓ�������
    public void OnPointerEnter(PointerEventData eventData)
    {
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
        if (_renderer != null)
        {
            // ���f���̐F���������邭����ȂǁA���o�I�ȃt�B�[�h�o�b�N
            _renderer.material.color = originalColor + new Color(0.1f, 0.1f, 0.1f, 0);
        }
        // �����p�l����\��
        if (armorSelectUI != null)
        {
            armorSelectUI.ShowDescription(armorData);
        }
    }

    // �}�E�X�J�[�\�������f������o����
    public void OnPointerExit(PointerEventData eventData)
    {
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
        if (_renderer != null)
        {
            // ���̐F�ɖ߂�
            _renderer.material.color = originalColor;
        }
        // �����p�l�����\���� (�z�o�[���ɕʂ̃A�[�}�[�Ɉړ������ꍇ�͂����ɔ�\����)
        // �������A�N���b�N���Đ�����\�������ꍇ�͕��Ȃ��悤�Ƀ��W�b�N�𒲐�����K�v�����邩��
        // ����̓V���v���Ƀ}�E�X�A�E�g�ŏ���
        if (armorSelectUI != null)
        {
            armorSelectUI.HideDescription();
        }
    }
}