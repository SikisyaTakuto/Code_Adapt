using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // �}�E�X�C�x���g���������߂ɕK�v
using System.Collections;        // Coroutine�̂��߂ɕK�v
using UnityEngine.SceneManagement; // SceneManager���g�p���邽�߂ɒǉ�

public class StageCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("�X�e�[�W�ݒ�")]
    [Tooltip("���̃J�[�h���\���X�e�[�W�̃V�[����")]
    public string stageSceneName;

    [Tooltip("�`���[�g���A���V�[���̖��O�B���̖��O�̃X�e�[�W���I�����ꂽ�ꍇ�A�A�[�}�[�I�����X�L�b�v���Ē��ڃ��[�h���܂��B")]
    public string tutorialSceneName = "TutorialScene"; // ��: "TutorialScene" ���f�t�H���g�l�Ƃ��Đݒ�

    [Header("�`���[�g���A���p�A�[�}�[�����I��")]
    [Tooltip("�`���[�g���A���V�[���ɒ��ڑJ�ڂ���ۂɎ����őI�������ArmorData�B3�ݒ肵�Ă��������B")]
    public ArmorData[] tutorialArmors; // string[] ���� ArmorData[] �ɕύX

    private Vector3 originalScale;
    private int originalSiblingIndex; // ����Hierarchy�ł̕��я�
    private Image cardImage; // �J�[�h��Image�R���|�[�l���g

    private const float HOVER_SCALE_FACTOR = 1.1f; // �}�E�X�I�[�o�[���̊g�嗦
    private const float ANIMATION_DURATION = 0.15f; // �A�j���[�V��������
    [SerializeField, Tooltip("SE�Đ�����������܂ł̑ҋ@���ԁi�b�j")]
    private float sePlayDuration = 0.3f; // SE�̒����ɉ����Ē���

    private bool isProcessingClick = false; // �N���b�N���������ǂ����̃t���O�i���d�N���b�N�h�~�j

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

    void Start()
    {
        // Debug.Log ��ǉ����āAArmorManager.Instance �̏�Ԃ��m�F
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("StageCard: Start����AudioManager.Instance��null�ł��B", this);
        }
        if (ArmorManager.Instance == null)
        {
            Debug.LogWarning("StageCard: Start����ArmorManager.Instance��null�ł��B�`���[�g���A���A�[�}�[�̎����ݒ�ɉe������\��������܂��B", this);
        }
    }

    // �}�E�X�J�[�\�����J�[�h�ɓ�������
    public void OnPointerEnter(PointerEventData eventData)
    {
        originalSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
    }

    // �}�E�X�J�[�\�����J�[�h����o����
    public void OnPointerExit(PointerEventData eventData)
    {
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
    }

    // �J�[�h���N���b�N���ꂽ��
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isProcessingClick)
        {
            return;
        }

        if (!string.IsNullOrEmpty(stageSceneName))
        {
            isProcessingClick = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSE();
                StartCoroutine(LoadSceneAfterSE(stageSceneName));
            }
            else
            {
                Debug.LogError("AudioManager.Instance ��������܂���BSE���Đ������ɃV�[�������[�h���܂��B", this);
                HandleSceneLoadImmediately(stageSceneName);
                isProcessingClick = false;
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} �� Stage Scene Name ���ݒ肳��Ă��܂���B", this);
        }
    }

    private IEnumerator LoadSceneAfterSE(string sceneToLoad)
    {
        yield return new WaitForSeconds(sePlayDuration);
        HandleSceneLoadImmediately(sceneToLoad);
        isProcessingClick = false;
    }

    private void HandleSceneLoadImmediately(string sceneToLoad)
    {
        if (sceneToLoad == tutorialSceneName)
        {
            Debug.Log($"�`���[�g���A���V�[�� '{sceneToLoad}' �𒼐ڃ��[�h���܂��B", this);

            if (ArmorManager.Instance != null)
            {
                ArmorManager.Instance.SetTutorialArmors(tutorialArmors);
            }
            else
            {
                // �����̃G���[���b�Z�[�W���o����A��L�u�l�����錴���ƏC�����@�v���Ċm�F���Ă��������B
                Debug.LogError("ArmorManager.Instance ��������܂���B�`���[�g���A���A�[�}�[�������ݒ�ł��܂���ł����B", this);
            }

            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            if (StageSelectManager.Instance != null)
            {
                StageSelectManager.Instance.SelectStage(sceneToLoad);
            }
            else
            {
                Debug.LogError("StageSelectManager ��������܂���B�V�[�����[�h�𒼐ڎ��s���܂��B", this);
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }
}