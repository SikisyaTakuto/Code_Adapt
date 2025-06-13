using UnityEngine;
using System.Collections.Generic;

public class ArmorManager : MonoBehaviour
{
    public static ArmorManager Instance { get; private set; }

    public List<ArmorMode> equippedModes = new List<ArmorMode>(); // 装備中の3つ
    public int currentModeIndex = 0;

    public ArmorMode CurrentMode => equippedModes[currentModeIndex];

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;

        // デフォルト装備（例）
        equippedModes.Add(ArmorMode.Balance);
        equippedModes.Add(ArmorMode.Buster);
        equippedModes.Add(ArmorMode.Stealth);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchMode(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchMode(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchMode(2);
    }

    public void SwitchMode(int index)
    {
        if (index >= 0 && index < equippedModes.Count)
        {
            currentModeIndex = index;
            Debug.Log($"Switched to: {equippedModes[index]}");
            ArmorUIController.Instance.UpdateUI();
        }
    }
}
