using UnityEngine;
using System.Collections.Generic;
using System.Linq; // LINQ���g�p���邽�߂ɒǉ�

public class ArmorManager : MonoBehaviour
{
    public static ArmorManager Instance { get; private set; }

    [Header("�S�Ă̗��p�\�ȃA�[�}�[�f�[�^")]
    public List<ArmorData> allAvailableArmors;

    [HideInInspector]
    public List<ArmorData> selectedArmors = new List<ArmorData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �V�[�����؂�ւ���Ă��j������Ȃ��悤�ɂ���
            Debug.Log("ArmorManager: ���߂ăC���X�^���X����������ADontDestroyOnLoad���K�p����܂����B", this.gameObject);
        }
        else
        {
            // ���ɃC���X�^���X�����݂���ꍇ�A�V�������ꂽ�������g��j��
            Debug.LogWarning("ArmorManager: ���ɃC���X�^���X�����݂��邽�߁A���̃I�u�W�F�N�g�͔j������܂��B", this.gameObject);
            Destroy(gameObject);
        }
    }

    public void DebugPrintAllArmors()
    {
        Debug.Log("--- All Available Armors ---");
        foreach (var armor in allAvailableArmors)
        {
            Debug.Log($"- {armor.name}");
        }
    }

    public void ClearSelectedArmors()
    {
        selectedArmors.Clear();
    }

    public void SetTutorialArmors(ArmorData[] armors)
    {
        ClearSelectedArmors();

        if (armors == null || armors.Length == 0)
        {
            Debug.LogWarning("SetTutorialArmors: �ݒ肷��A�[�}�[�f�[�^������܂���B", this);
            return;
        }

        foreach (ArmorData armor in armors)
        {
            if (armor != null)
            {
                selectedArmors.Add(armor);
                Debug.Log($"�`���[�g���A���p�� '{armor.name}' (�f�[�^) �������ݒ肵�܂����B", this);
            }
            else
            {
                Debug.LogWarning("�`���[�g���A���p�A�[�}�[�̃f�[�^��NULL�ł��BInspector�Ő������ݒ肳��Ă��邩�m�F���Ă��������B", this);
            }
        }

        if (selectedArmors.Count < 3)
        {
            Debug.LogWarning($"�`���[�g���A���p�A�[�}�[�̎����ݒ�: {selectedArmors.Count} �����ݒ�ł��܂���ł����B3�ݒ肳��Ă��邱�Ƃ��m�F���Ă��������B", this);
        }
    }
}