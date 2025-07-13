// ArmorSelectUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; // Coroutine�̂��߂ɕK�v
using UnityEngine.SceneManagement; // SceneManager�̂��߂ɒǉ�

public class ArmorSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform armorModelParent; // 3D���f�����C���X�^���X������e�I�u�W�F�N�g
    public Button confirmButton; // ����{�^��
    public Text selectedArmorsText; // �I�����ꂽ�A�[�}�[����\������UI�e�L�X�g

    [Header("Description UI")]
    public GameObject descriptionPanel; // �����p�l�� (��\��/�\����؂�ւ���)
    public Text armorNameText; // �A�[�}�[���\���p�e�L�X�g
    public Text armorDescriptionText; // �A�[�}�[�����\���p�e�L�X�g
    public Text armorStatsText; // �A�[�}�[�X�e�[�^�X�\���p�e�L�X�g

    private List<GameObject> _instantiatedArmorModels = new List<GameObject>(); // �V�[���ɃC���X�^���X�����ꂽ�A�[�}�[���f��
    private List<ArmorData> _currentlySelectedArmors = new List<ArmorData>(); // ���ݑI�𒆂̃A�[�}�[�f�[�^���X�g

    // �e�A�[�}�[�f�[�^�ɑΉ�����ArmorSelectable�R���|�[�l���g��ێ�
    private Dictionary<ArmorData, ArmorSelectable> _armorDataToSelectableMap = new Dictionary<ArmorData, ArmorSelectable>();

    private const int MAX_SELECTED_ARMORS = 3; // �I���ł���A�[�}�[�̍ő吔

    [Header("Scene Transition")]
    [SerializeField, Tooltip("����{�^���N���b�N���SE�Đ����ԁi�b�j")]
    private float confirmSEPlayDuration = 0.5f; // SE�̒����ɉ����Ē���
    public string nextSceneName = "GameScene"; // �����ɑJ�ڂ���V�[����

    void Start()
    {
        DisplayAllArmors();

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        UpdateSelectedArmorsUI();
    }

    void DisplayAllArmors()
    {
        foreach (GameObject model in _instantiatedArmorModels)
        {
            Destroy(model);
        }
        _instantiatedArmorModels.Clear();
        _armorDataToSelectableMap.Clear(); // �}�b�v���N���A

        if (ArmorManager.Instance == null || ArmorManager.Instance.allAvailableArmors == null || ArmorManager.Instance.allAvailableArmors.Count == 0)
        {
            Debug.LogError("���p�\�ȃA�[�}�[�f�[�^������܂���BArmorManager��ArmorData��ݒ肵�Ă��������B");
            return;
        }

        // ���̔z�u�I�t�Z�b�g�ƊԊu
        float startX = -(ArmorManager.Instance.allAvailableArmors.Count - 1)*1.5f; // �����Ɋ񂹂邽�߂̒���
        float spacing = 3f;

        for (int i = 0; i < ArmorManager.Instance.allAvailableArmors.Count; i++)
        {
            ArmorData armorData = ArmorManager.Instance.allAvailableArmors[i];
            if (armorData.armorPrefab != null)
            {
                GameObject armorModelInstance = Instantiate(armorData.armorPrefab, armorModelParent);
                armorModelInstance.transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
                armorModelInstance.transform.localRotation = Quaternion.Euler(0, 180, 0);
                armorModelInstance.transform.localScale = Vector3.one * 0.8f;

                _instantiatedArmorModels.Add(armorModelInstance);

                // ArmorSelectable�X�N���v�g���A�^�b�` (Collider��ArmorSelectable���ŏ��������)
                ArmorSelectable selectable = armorModelInstance.AddComponent<ArmorSelectable>();
                selectable.armorData = armorData;
                selectable.armorSelectUI = this;
                _armorDataToSelectableMap.Add(armorData, selectable); // �}�b�v�ɒǉ�
            }
            else
            {
                Debug.LogWarning($"�A�[�}�[ '{armorData.armorName}' ��Prefab���ݒ肳��Ă��܂���B");
            }
        }
    }

    /// <summary>
    /// �A�[�}�[���N���b�N���ꂽ�Ƃ��ɌĂяo�����
    /// </summary>
    public void OnArmorClicked(ArmorData clickedArmor)
    {
        // �N���b�N�����Đ�
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

        if (_currentlySelectedArmors.Contains(clickedArmor))
        {
            // ���ɑI������Ă���ꍇ�͉���
            _currentlySelectedArmors.Remove(clickedArmor);
            Debug.Log($"�A�[�}�[����: {clickedArmor.armorName}");
        }
        else
        {
            // �V���ɑI������ꍇ
            if (_currentlySelectedArmors.Count < MAX_SELECTED_ARMORS)
            {
                _currentlySelectedArmors.Add(clickedArmor);
                Debug.Log($"�A�[�}�[�I��: {clickedArmor.armorName}");
            }
            else
            {
                Debug.LogWarning($"����ȏ�A�[�}�[��I���ł��܂���B�ő� {MAX_SELECTED_ARMORS} �܂łł��B");
                // �I���ł��Ȃ��ꍇ�͉��͖炷���A�I����Ԃ͍X�V���Ȃ�
                // ���̏ꍇ�A�ꎞ�I�ȃn�C���C�g����������K�v������
                UpdateAllArmorHighlights(); // �S�A�[�}�[�̃n�C���C�g��Ԃ��X�V���Ĉꎞ�n�C���C�g������
                return;
            }
        }
        UpdateSelectedArmorsUI();
        UpdateAllArmorHighlights(); // �S�A�[�}�[�̃n�C���C�g��Ԃ��X�V
    }

    /// <summary>
    /// �}�E�X�_�E�����̈ꎞ�I�ȃn�C���C�g�𐧌�
    /// </summary>
    public void HighlightTemporary(ArmorData armorToHighlight)
    {
        foreach (var entry in _armorDataToSelectableMap)
        {
            // �N���b�N���ꂽ���̂����ꎞ�I�Ƀn�C���C�g
            entry.Value.SetTemporaryHighlight(entry.Key == armorToHighlight);
        }
    }

    /// <summary>
    /// �S�ẴA�[�}�[���f���̃n�C���C�g��Ԃ��A���݂̑I�����X�g�Ɋ�Â��čX�V����
    /// </summary>
    private void UpdateAllArmorHighlights()
    {
        foreach (var entry in _armorDataToSelectableMap)
        {
            // ���ݑI�����X�g�Ɋ܂܂�Ă��邩�ǂ����Ńn�C���C�g��؂�ւ���
            entry.Value.SetHighlight(_currentlySelectedArmors.Contains(entry.Key));
        }
    }

    /// <summary>
    /// �w�肳�ꂽ�A�[�}�[�����ݑI�����X�g�Ɋ܂܂�Ă��邩���m�F
    /// </summary>
    public bool IsArmorSelected(ArmorData armorData)
    {
        return _currentlySelectedArmors.Contains(armorData);
    }

    private void UpdateSelectedArmorsUI()
    {
        if (selectedArmorsText != null)
        {
            string displayText = "�I�𒆂̃A�[�}�[:\n";
            for (int i = 0; i < MAX_SELECTED_ARMORS; i++)
            {
                if (i < _currentlySelectedArmors.Count)
                {
                    displayText += $"{i + 1}. <color=yellow>{_currentlySelectedArmors[i].armorName}</color>\n"; // �I�����ꂽ�A�[�}�[��������
                }
                else
                {
                    displayText += $"{i + 1}. (���I��)\n";
                }
            }
            selectedArmorsText.text = displayText;
        }

        // ����{�^���̗L��/������؂�ւ��� (��: 3�I�����ꂽ��L��)
        if (confirmButton != null)
        {
            confirmButton.interactable = _currentlySelectedArmors.Count == MAX_SELECTED_ARMORS;
        }
    }

    public void ShowDescription(ArmorData armorData)
    {

            if (armorNameText != null) armorNameText.text = armorData.armorName;
            if (armorDescriptionText != null) armorDescriptionText.text = armorData.description;
            if (armorStatsText != null)
            {
                string stats = "--- �X�e�[�^�X --- \n";
                stats += $"�ړ����x: <color=#00FF00>x{armorData.moveSpeedModifier:F1}</color>\n"; // �ΐF
                stats += $"�U����: <color=#FF0000>x{armorData.attackPowerModifier:F1}</color>\n"; // �ԐF
                stats += $"��s�\: {(armorData.canFly ? "<color=blue>�͂�</color>" : "������")}\n"; // �F
                stats += $"�\�[�h�r�b�g: {(armorData.canUseSwordBit ? "<color=cyan>�g�p�\</color>" : "�g�p�s��")}\n"; // �V�A���F
                armorStatsText.text = stats;
            }
        
    }

    void OnConfirmButtonClicked()
    {
        if (_currentlySelectedArmors.Count == MAX_SELECTED_ARMORS) // 3�I������Ă��邱�Ƃ��ŏI�m�F
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSE(); // ���艹��炷
                StartCoroutine(TransitionToGameSceneAfterSE()); // ������I����Ă���V�[���J��
            }
            else
            {
                Debug.LogError("AudioManager��������܂���BSE�Ȃ��ŃV�[���J�ڂ��܂��B");
                TransitionToGameScene(); // AudioManager���Ȃ��ꍇ
            }
        }
        else
        {
            Debug.LogWarning($"�A�[�}�[��{MAX_SELECTED_ARMORS}�I������Ă��܂���B����: {_currentlySelectedArmors.Count}��");
            if (AudioManager.Instance != null)
            {
                // �G���[SE�Ȃǂ�����΍Đ�
                // AudioManager.Instance.PlayErrorSE(); 
            }
        }
    }

    private IEnumerator TransitionToGameSceneAfterSE()
    {
        yield return new WaitForSeconds(confirmSEPlayDuration);
        TransitionToGameScene();
    }

    private void TransitionToGameScene()
    {
        if (ArmorManager.Instance != null)
        {
            ArmorManager.Instance.selectedArmors.Clear();
            foreach (ArmorData armor in _currentlySelectedArmors)
            {
                ArmorManager.Instance.selectedArmors.Add(armor);
            }
            Debug.Log($"�A�[�}�[�I�������I�Q�[���V�[�� '{nextSceneName}' �֑J�ڂ��܂��B");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError("GameManager��������܂���B���ڃV�[�������[�h���܂��B");
                SceneManager.LoadScene(nextSceneName);
            }
        }
        else
        {
            Debug.LogError("ArmorManager��������܂���B�A�[�}�[���������p���܂���B");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}