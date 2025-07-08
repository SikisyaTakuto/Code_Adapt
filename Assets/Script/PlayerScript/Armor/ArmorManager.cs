// ArmorManager.cs
using UnityEngine;
using System.Collections.Generic;

public class ArmorManager : MonoBehaviour
{
    public static ArmorManager Instance { get; private set; }

    // 全ての利用可能なアーマーデータ (Inspectorで設定)
    public List<ArmorData> allAvailableArmors;

    // アーマー選択画面で選択されたアーマー
    [HideInInspector] // Inspectorには表示しない
    public List<ArmorData> selectedArmors = new List<ArmorData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンが切り替わっても破棄されないようにする
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // デバッグ用: 全アーマーをログに出力
    public void DebugPrintAllArmors()
    {
        Debug.Log("--- All Available Armors ---");
        foreach (var armor in allAvailableArmors)
        {
            Debug.Log($"- {armor.name}");
        }
    }
}