using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ArmorSelectManager : MonoBehaviour
{
    [Header("Settings")]
    public int maxSelectable = 2;
    public string nextSceneName = "GameScene";

    [Header("UI References")]
    public Button[] armorButtons;      // 0:Normal, 1:Buster, 2:Speed
    public Image[] buttonBackgrounds;
    public Button startButton;

    [Header("Explanation UI")]
    public Text descriptionText;      // ★性能説明用（画面に1つ配置）

    [Header("Colors")]
    public Color selectedColor = Color.yellow;
    public Color defaultColor = Color.white;

    // 各アーマーの詳細説明（1つのTextに流し込む内容）
    private string[] armorDescriptions = {
        "【ノーマルアーマー】\n標準的な性能。安定した操作が可能です。",
        "【バスターアーマー】\n攻撃特化型。攻撃の威力が高いです。",
        "【スピードアーマー】\n機動力特化型。移動速度が向上し、空中ダッシュが可能です。"
    };

    private List<int> selectedArmorIds = new List<int>();

    void Start()
    {
        if (startButton != null) startButton.interactable = false;

        for (int i = 0; i < armorButtons.Length; i++)
        {
            int index = i;
            armorButtons[i].onClick.AddListener(() => OnArmorClick(index));
        }

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        // 最初のメッセージ
        if (descriptionText != null)
            descriptionText.text = "装備するアーマーを2つまで選んでください。";

        UpdateUI();
    }

    void OnArmorClick(int id)
    {
        if (selectedArmorIds.Contains(id))
        {
            selectedArmorIds.Remove(id);
        }
        else
        {
            if (selectedArmorIds.Count < maxSelectable)
            {
                selectedArmorIds.Add(id);
            }
        }

        // ★クリックされたアーマーの説明を表示する
        ShowDescription(id);

        UpdateUI();
    }

    // 説明文を更新する関数
    void ShowDescription(int id)
    {
        if (descriptionText != null && id >= 0 && id < armorDescriptions.Length)
        {
            descriptionText.text = armorDescriptions[id];
        }
    }

    void UpdateUI()
    {
        for (int i = 0; i < buttonBackgrounds.Length; i++)
        {
            if (buttonBackgrounds[i] != null)
            {
                buttonBackgrounds[i].color = selectedArmorIds.Contains(i) ? selectedColor : defaultColor;
            }
        }

        if (startButton != null)
            startButton.interactable = selectedArmorIds.Count > 0;
    }

    public void StartGame()
    {
        if (selectedArmorIds.Count == 0) return;
        PlayerPrefs.SetInt("SelectedArmor_Slot0", selectedArmorIds[0]);
        PlayerPrefs.SetInt("SelectedArmor_Slot1", selectedArmorIds.Count > 1 ? selectedArmorIds[1] : -1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nextSceneName);
    }
}