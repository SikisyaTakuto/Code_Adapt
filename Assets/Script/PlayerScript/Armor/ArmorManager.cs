// ArmorManager.cs
using UnityEngine;
using System.Collections.Generic;

public class ArmorManager : MonoBehaviour
{
    public static ArmorManager Instance { get; private set; }

    // �S�Ă̗��p�\�ȃA�[�}�[�f�[�^ (Inspector�Őݒ�)
    public List<ArmorData> allAvailableArmors;

    // �A�[�}�[�I����ʂőI�����ꂽ�A�[�}�[
    [HideInInspector] // Inspector�ɂ͕\�����Ȃ�
    public List<ArmorData> selectedArmors = new List<ArmorData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �V�[�����؂�ւ���Ă��j������Ȃ��悤�ɂ���
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // �f�o�b�O�p: �S�A�[�}�[�����O�ɏo��
    public void DebugPrintAllArmors()
    {
        Debug.Log("--- All Available Armors ---");
        foreach (var armor in allAvailableArmors)
        {
            Debug.Log($"- {armor.name}");
        }
    }
}