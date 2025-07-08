using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // �}�E�X�C�x���g���������߂ɕK�v

public class StageCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("�X�e�[�W�ݒ�")]
    [Tooltip("���̃J�[�h���\���X�e�[�W�̃V�[����")]
    public string stageSceneName;

    private Vector3 originalScale;
    private int originalSiblingIndex; // ����Hierarchy�ł̕��я�
    private Image cardImage; // �J�[�h��Image�R���|�[�l���g

    private const float HOVER_SCALE_FACTOR = 1.1f; // �}�E�X�I�[�o�[���̊g�嗦
    private const float ANIMATION_DURATION = 0.15f; // �A�j���[�V��������

    private void Awake()
    {
        originalScale = transform.localScale;
        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError("StageCard�X�N���v�g��Image�R���|�[�l���g�ɃA�^�b�`���Ă��������B", this);
            enabled = false; // Image�R���|�[�l���g���Ȃ��ꍇ�̓X�N���v�g�𖳌���
        }
    }

    // �}�E�X�J�[�\�����J�[�h�ɓ�������
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} �Ƀ}�E�X���������I");
        originalSiblingIndex = transform.GetSiblingIndex(); // ���݂̕��я���ۑ�
        transform.SetAsLastSibling(); // �őO�ʂɈړ��iHierarchy�̖����Ɉړ��j
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
    }

    // �}�E�X�J�[�\�����J�[�h����o����
    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} ����}�E�X���o���I");
        // ���̕��я��ɖ߂� (�������A�őO�ʂɂȂ����܂܂̏ꍇ�͒������K�v�ɂȂ邱�Ƃ�)
        // �Ⴆ�΁A���̃J�[�h�Ƀ}�E�X���������ĂȂ��ꍇ�̂݌��̈ʒu�ɖ߂��Ȃ�
        // ����́A�}�E�X�����ꂽ�猳�̃X�P�[���ɖ߂����Ƃɒ���
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();

        // ���̕��я��ɖ߂������́A���̃J�[�h����O�ɂȂ����Ƃ��Ɏ����Ō��ɍs�����߁A
        // �K�������K�v�ł͂Ȃ����A���o�I�Ȉ�ѐ���ۂ��߂ɂ͌�������]�n����
        // transform.SetSiblingIndex(originalSiblingIndex); 
    }

    // �J�[�h���N���b�N���ꂽ��
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} ���N���b�N���ꂽ�I");
        // GameManager��ʂ��đI�����ꂽ�X�e�[�W����ۑ����AArmorSelectScene�֑J��
        if (!string.IsNullOrEmpty(stageSceneName))
        {
            if (StageSelectManager.Instance != null)
            {
                StageSelectManager.Instance.SelectStage(stageSceneName);
            }
            else
            {
                Debug.LogError("StageSelectManager ��������܂���B");
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} �� Stage Scene Name ���ݒ肳��Ă��܂���B");
        }
    }
}