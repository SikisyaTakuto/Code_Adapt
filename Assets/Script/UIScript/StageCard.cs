using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // �}�E�X�C�x���g���������߂ɕK�v
using System.Collections;       // Coroutine�̂��߂ɕK�v

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
    [SerializeField, Tooltip("SE�Đ�����������܂ł̑ҋ@���ԁi�b�j")]
    private float sePlayDuration = 0.3f; // SE�̒����ɉ����Ē���

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
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
    }

    // �J�[�h���N���b�N���ꂽ��
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} ���N���b�N���ꂽ�I");

        if (!string.IsNullOrEmpty(stageSceneName))
        {
            // �܂��{�^���N���b�NSE���Đ�
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSE();
                // SE�Đ����I���̂�҂��Ă���V�[�������[�h����R���[�`�����J�n
                StartCoroutine(LoadSceneAfterSE(stageSceneName));
            }
            else
            {
                Debug.LogError("AudioManager.Instance ��������܂���BSE���Đ������ɃV�[�������[�h���܂��B");
                // AudioManager���Ȃ��ꍇ�ł��V�[���̓��[�h����
                if (StageSelectManager.Instance != null)
                {
                    StageSelectManager.Instance.SelectStage(stageSceneName);
                }
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} �� Stage Scene Name ���ݒ肳��Ă��܂���B");
        }
    }

    /// <summary>
    /// SE�Đ���҂��Ă���V�[�������[�h����R���[�`��
    /// </summary>
    private IEnumerator LoadSceneAfterSE(string sceneToLoad)
    {
        // SE���Đ����I���܂őҋ@
        yield return new WaitForSeconds(sePlayDuration);

        // StageSelectManager��ʂ��đI�����ꂽ�X�e�[�W����ۑ����A���̃V�[���֑J��
        if (StageSelectManager.Instance != null)
        {
            StageSelectManager.Instance.SelectStage(sceneToLoad);
        }
        else
        {
            Debug.LogError("StageSelectManager ��������܂���B�V�[�����[�h�𒼐ڎ��s���܂��B");
            // �ً}����Ƃ��Ē��ڃV�[�����[�h
            // SceneManager.LoadScene(sceneToLoad); // �K�v�ɉ����ăR�����g����
        }
    }
}