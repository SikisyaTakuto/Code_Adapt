using UnityEngine;
using System.Collections.Generic;
using System.Linq; // LINQを使用するために追加

public class ArmorManager : MonoBehaviour
{
    public static ArmorManager Instance { get; private set; }

    [Header("全ての利用可能なアーマーデータ")]
    public List<ArmorData> allAvailableArmors;

    [HideInInspector]
    public List<ArmorData> selectedArmors = new List<ArmorData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンが切り替わっても破棄されないようにする
            Debug.Log("ArmorManager: 初めてインスタンスが生成され、DontDestroyOnLoadが適用されました。", this.gameObject);
        }
        else
        {
            // 既にインスタンスが存在する場合、新しく作られた自分自身を破棄
            Debug.LogWarning("ArmorManager: 既にインスタンスが存在するため、このオブジェクトは破棄されます。", this.gameObject);
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
            Debug.LogWarning("SetTutorialArmors: 設定するアーマーデータがありません。", this);
            return;
        }

        foreach (ArmorData armor in armors)
        {
            if (armor != null)
            {
                selectedArmors.Add(armor);
                Debug.Log($"チュートリアル用に '{armor.name}' (データ) を自動設定しました。", this);
            }
            else
            {
                Debug.LogWarning("チュートリアル用アーマーのデータがNULLです。Inspectorで正しく設定されているか確認してください。", this);
            }
        }

        if (selectedArmors.Count < 3)
        {
            Debug.LogWarning($"チュートリアル用アーマーの自動設定: {selectedArmors.Count} 個しか設定できませんでした。3つ設定されていることを確認してください。", this);
        }
    }
}