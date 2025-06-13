using UnityEngine;
using UnityEngine.UI;

public class ArmorUIController : MonoBehaviour
{
    public static ArmorUIController Instance;

    [System.Serializable]
    public class ArmorSlot
    {
        public Text label;          // Text
        public Image highlight;
    }

    public ArmorSlot[] slots = new ArmorSlot[3];

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var mode = ArmorManager.Instance.equippedModes[i];
            slots[i].label.text = mode.ToString();

            // ���ݑI�𒆂̃X���b�g�����n�C���C�g��\��
            slots[i].highlight.enabled = (i == ArmorManager.Instance.currentModeIndex);
        }
    }

    private void Start()
    {
        UpdateUI();
    }
}
