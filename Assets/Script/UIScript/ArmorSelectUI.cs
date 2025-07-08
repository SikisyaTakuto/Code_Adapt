using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class ArmorSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform armorModelParent; // 3D���f�����C���X�^���X������e�I�u�W�F�N�g
    public Button confirmButton; // ����{�^��
    public Text selectedArmorsText; // �I�����ꂽ�A�[�}�[����\������UI�e�L�X�g (TextMeshPro)

    [Header("Description UI")]
    public GameObject descriptionPanel; // �����p�l�� (��\��/�\����؂�ւ���)
    public Text armorNameText; // �A�[�}�[���\���p�e�L�X�g
    public Text armorDescriptionText; // �A�[�}�[�����\���p�e�L�X�g
    public Text armorStatsText; // �A�[�}�[�X�e�[�^�X�\���p�e�L�X�g
    public Button closeDescriptionButton; // �����p�l�������{�^��

    private List<GameObject> _instantiatedArmorModels = new List<GameObject>(); // �V�[���ɃC���X�^���X�����ꂽ�A�[�}�[���f��
    private List<ArmorData> _currentlySelectedArmors = new List<ArmorData>(); // ���ݑI�𒆂̃A�[�}�[�f�[�^���X�g

    private const int MAX_SELECTED_ARMORS = 3; // �I���ł���A�[�}�[�̍ő吔

    void Start()
    {
        // �A�[�}�[���f���̕\��
        DisplayAllArmors();

        // UI�C�x���g���X�i�[�̓o�^
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        if (closeDescriptionButton != null)
        {
            closeDescriptionButton.onClick.AddListener(HideDescription);
        }

        // ������Ԃł͐����p�l�����\���ɂ���
        HideDescription();
        UpdateSelectedArmorsUI(); // �����\���̍X�V
    }

    /// <summary>
    /// ���p�\�ȑS�ẴA�[�}�[���f�����V�[���ɕ\������
    /// </summary>
    void DisplayAllArmors()
    {
        // �����̃��f�����N���A
        foreach (GameObject model in _instantiatedArmorModels)
        {
            Destroy(model);
        }
        _instantiatedArmorModels.Clear();

        if (ArmorManager.Instance == null || ArmorManager.Instance.allAvailableArmors == null || ArmorManager.Instance.allAvailableArmors.Count == 0)
        {
            Debug.LogError("���p�\�ȃA�[�}�[�f�[�^������܂���BArmorManager��ArmorData��ݒ肵�Ă��������B");
            return;
        }

        // �e�A�[�}�[��z�u
        // ���C�A�E�g��Unity�G�f�B�^�Œ������邽�߁A�����ł̓C���X�^���X���̂�
        float xOffset = -5f; // ���̔z�u�I�t�Z�b�g
        float spacing = 5f;  // ���̔z�u�Ԋu

        for (int i = 0; i < ArmorManager.Instance.allAvailableArmors.Count; i++)
        {
            ArmorData armorData = ArmorManager.Instance.allAvailableArmors[i];
            if (armorData.armorPrefab != null)
            {
                // ���f���̃C���X�^���X��
                GameObject armorModelInstance = Instantiate(armorData.armorPrefab, armorModelParent);
                armorModelInstance.transform.localPosition = new Vector3(xOffset + i * spacing, 0, 0); // �K���Ȕz�u
                armorModelInstance.transform.localRotation = Quaternion.Euler(0, 180, 0); // ���₷�������ɒ���
                armorModelInstance.transform.localScale = Vector3.one * 0.8f; // ���f���̃T�C�Y����

                _instantiatedArmorModels.Add(armorModelInstance);

                // �N���b�N�C�x���g��ǉ����邽�߂�Collider��ArmorSelectable�X�N���v�g���A�^�b�`
                // Mesh Collider�܂���Box Collider��t����K�v������
                Collider collider = armorModelInstance.GetComponent<Collider>();
                if (collider == null)
                {
                    // ���f����Collider���Ȃ��ꍇ�͒ǉ� (��: BoxCollider)
                    collider = armorModelInstance.AddComponent<BoxCollider>();
                    // Collider�̃T�C�Y�ƒ��S�����f���ɍ��킹�Ē�������K�v������
                    // Bounds�𗘗p����Ȃǂ��Ď�����������Ɨǂ�
                    Renderer renderer = armorModelInstance.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        ((BoxCollider)collider).center = renderer.bounds.center - armorModelInstance.transform.position;
                        ((BoxCollider)collider).size = renderer.bounds.size;
                    }
                }

                ArmorSelectable selectable = armorModelInstance.AddComponent<ArmorSelectable>();
                selectable.armorData = armorData;
                selectable.armorSelectUI = this; // ���g��n��
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
    /// <param name="clickedArmor">�N���b�N���ꂽ�A�[�}�[�̃f�[�^</param>
    public void OnArmorClicked(ArmorData clickedArmor)
    {
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
                return; // ����ȏ�ǉ��ł��Ȃ��ꍇ�͏����𒆒f
            }
        }
        UpdateSelectedArmorsUI();
    }

    /// <summary>
    /// �I�𒆂̃A�[�}�[UI���X�V����
    /// </summary>
    private void UpdateSelectedArmorsUI()
    {
        if (selectedArmorsText != null)
        {
            string displayText = "�I�𒆂̃A�[�}�[:\n";
            for (int i = 0; i < MAX_SELECTED_ARMORS; i++)
            {
                if (i < _currentlySelectedArmors.Count)
                {
                    displayText += $"{i + 1}. {_currentlySelectedArmors[i].armorName}\n";
                }
                else
                {
                    displayText += $"{i + 1}. (���I��)\n";
                }
            }
            selectedArmorsText.text = displayText;
        }

        // ����{�^���̗L��/������؂�ւ��� (��: 1�ȏ�I�����ꂽ��L��)
        if (confirmButton != null)
        {
            confirmButton.interactable = _currentlySelectedArmors.Count > 0;
        }
    }

    /// <summary>
    /// �����p�l����\������
    /// </summary>
    /// <param name="armorData">������\������A�[�}�[�̃f�[�^</param>
    public void ShowDescription(ArmorData armorData)
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(true);
            if (armorNameText != null) armorNameText.text = armorData.armorName;
            if (armorDescriptionText != null) armorDescriptionText.text = armorData.description;
            if (armorStatsText != null)
            {
                // �e�X�e�[�^�X�̉e���x��\�� (�K�v�ɉ����ďڍ׉�)
                string stats = "--- �X�e�[�^�X --- \n";
                stats += $"�ړ����x: x{armorData.moveSpeedModifier:F1}\n";
                stats += $"�U����: x{armorData.attackPowerModifier:F1}\n";
                stats += $"��s�\: {(armorData.canFly ? "�͂�" : "������")}\n";
                stats += $"�\�[�h�r�b�g: {(armorData.canUseSwordBit ? "�g�p�\" : "�g�p�s��")}\n";
                // ���̃X�e�[�^�X���ǉ�
                armorStatsText.text = stats;
            }
        }
    }

    /// <summary>
    /// �����p�l�����\���ɂ���
    /// </summary>
    public void HideDescription()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ����{�^�����N���b�N���ꂽ�Ƃ��ɌĂяo�����
    /// </summary>
    void OnConfirmButtonClicked()
    {
        if (_currentlySelectedArmors.Count > 0)
        {
            // ArmorManager�ɑI�����ꂽ�A�[�}�[��n��
            if (ArmorManager.Instance != null)
            {
                ArmorManager.Instance.selectedArmors.Clear();
                foreach (ArmorData armor in _currentlySelectedArmors)
                {
                    ArmorManager.Instance.selectedArmors.Add(armor);
                }
                Debug.Log("�A�[�}�[�I�������I�Q�[���V�[���֑J�ڂ��܂��B");

                // GameManager��ʂ��đI�����ꂽ�X�e�[�W�֑J��
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GoToSelectedStage();
                }
                else
                {
                    Debug.LogError("GameManager��������܂���B�X�e�[�W�ɑJ�ڂł��܂���B");
                }
            }
            else
            {
                Debug.LogError("ArmorManager��������܂���B");
            }
        }
        else
        {
            Debug.LogWarning("�A�[�}�[���I������Ă��܂���B");
        }
    }
}