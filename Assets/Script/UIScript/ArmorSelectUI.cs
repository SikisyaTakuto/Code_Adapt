using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class ArmorSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform armorModelParent; // 3Dモデルをインスタンス化する親オブジェクト
    public Button confirmButton; // 決定ボタン
    public Text selectedArmorsText; // 選択されたアーマー名を表示するUIテキスト (TextMeshPro)

    [Header("Description UI")]
    public GameObject descriptionPanel; // 説明パネル (非表示/表示を切り替える)
    public Text armorNameText; // アーマー名表示用テキスト
    public Text armorDescriptionText; // アーマー説明表示用テキスト
    public Text armorStatsText; // アーマーステータス表示用テキスト
    public Button closeDescriptionButton; // 説明パネルを閉じるボタン

    private List<GameObject> _instantiatedArmorModels = new List<GameObject>(); // シーンにインスタンス化されたアーマーモデル
    private List<ArmorData> _currentlySelectedArmors = new List<ArmorData>(); // 現在選択中のアーマーデータリスト

    private const int MAX_SELECTED_ARMORS = 3; // 選択できるアーマーの最大数

    void Start()
    {
        // アーマーモデルの表示
        DisplayAllArmors();

        // UIイベントリスナーの登録
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        if (closeDescriptionButton != null)
        {
            closeDescriptionButton.onClick.AddListener(HideDescription);
        }

        // 初期状態では説明パネルを非表示にする
        HideDescription();
        UpdateSelectedArmorsUI(); // 初期表示の更新
    }

    /// <summary>
    /// 利用可能な全てのアーマーモデルをシーンに表示する
    /// </summary>
    void DisplayAllArmors()
    {
        // 既存のモデルをクリア
        foreach (GameObject model in _instantiatedArmorModels)
        {
            Destroy(model);
        }
        _instantiatedArmorModels.Clear();

        if (ArmorManager.Instance == null || ArmorManager.Instance.allAvailableArmors == null || ArmorManager.Instance.allAvailableArmors.Count == 0)
        {
            Debug.LogError("利用可能なアーマーデータがありません。ArmorManagerにArmorDataを設定してください。");
            return;
        }

        // 各アーマーを配置
        // レイアウトはUnityエディタで調整するため、ここではインスタンス化のみ
        float xOffset = -5f; // 仮の配置オフセット
        float spacing = 5f;  // 仮の配置間隔

        for (int i = 0; i < ArmorManager.Instance.allAvailableArmors.Count; i++)
        {
            ArmorData armorData = ArmorManager.Instance.allAvailableArmors[i];
            if (armorData.armorPrefab != null)
            {
                // モデルのインスタンス化
                GameObject armorModelInstance = Instantiate(armorData.armorPrefab, armorModelParent);
                armorModelInstance.transform.localPosition = new Vector3(xOffset + i * spacing, 0, 0); // 適当な配置
                armorModelInstance.transform.localRotation = Quaternion.Euler(0, 180, 0); // 見やすい向きに調整
                armorModelInstance.transform.localScale = Vector3.one * 0.8f; // モデルのサイズ調整

                _instantiatedArmorModels.Add(armorModelInstance);

                // クリックイベントを追加するためにColliderとArmorSelectableスクリプトをアタッチ
                // Mesh ColliderまたはBox Colliderを付ける必要がある
                Collider collider = armorModelInstance.GetComponent<Collider>();
                if (collider == null)
                {
                    // モデルにColliderがない場合は追加 (例: BoxCollider)
                    collider = armorModelInstance.AddComponent<BoxCollider>();
                    // Colliderのサイズと中心をモデルに合わせて調整する必要がある
                    // Boundsを利用するなどして自動調整すると良い
                    Renderer renderer = armorModelInstance.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        ((BoxCollider)collider).center = renderer.bounds.center - armorModelInstance.transform.position;
                        ((BoxCollider)collider).size = renderer.bounds.size;
                    }
                }

                ArmorSelectable selectable = armorModelInstance.AddComponent<ArmorSelectable>();
                selectable.armorData = armorData;
                selectable.armorSelectUI = this; // 自身を渡す
            }
            else
            {
                Debug.LogWarning($"アーマー '{armorData.armorName}' のPrefabが設定されていません。");
            }
        }
    }

    /// <summary>
    /// アーマーがクリックされたときに呼び出される
    /// </summary>
    /// <param name="clickedArmor">クリックされたアーマーのデータ</param>
    public void OnArmorClicked(ArmorData clickedArmor)
    {
        if (_currentlySelectedArmors.Contains(clickedArmor))
        {
            // 既に選択されている場合は解除
            _currentlySelectedArmors.Remove(clickedArmor);
            Debug.Log($"アーマー解除: {clickedArmor.armorName}");
        }
        else
        {
            // 新たに選択する場合
            if (_currentlySelectedArmors.Count < MAX_SELECTED_ARMORS)
            {
                _currentlySelectedArmors.Add(clickedArmor);
                Debug.Log($"アーマー選択: {clickedArmor.armorName}");
            }
            else
            {
                Debug.LogWarning($"これ以上アーマーを選択できません。最大 {MAX_SELECTED_ARMORS} 個までです。");
                return; // これ以上追加できない場合は処理を中断
            }
        }
        UpdateSelectedArmorsUI();
    }

    /// <summary>
    /// 選択中のアーマーUIを更新する
    /// </summary>
    private void UpdateSelectedArmorsUI()
    {
        if (selectedArmorsText != null)
        {
            string displayText = "選択中のアーマー:\n";
            for (int i = 0; i < MAX_SELECTED_ARMORS; i++)
            {
                if (i < _currentlySelectedArmors.Count)
                {
                    displayText += $"{i + 1}. {_currentlySelectedArmors[i].armorName}\n";
                }
                else
                {
                    displayText += $"{i + 1}. (未選択)\n";
                }
            }
            selectedArmorsText.text = displayText;
        }

        // 決定ボタンの有効/無効を切り替える (例: 1つ以上選択されたら有効)
        if (confirmButton != null)
        {
            confirmButton.interactable = _currentlySelectedArmors.Count > 0;
        }
    }

    /// <summary>
    /// 説明パネルを表示する
    /// </summary>
    /// <param name="armorData">説明を表示するアーマーのデータ</param>
    public void ShowDescription(ArmorData armorData)
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(true);
            if (armorNameText != null) armorNameText.text = armorData.armorName;
            if (armorDescriptionText != null) armorDescriptionText.text = armorData.description;
            if (armorStatsText != null)
            {
                // 各ステータスの影響度を表示 (必要に応じて詳細化)
                string stats = "--- ステータス --- \n";
                stats += $"移動速度: x{armorData.moveSpeedModifier:F1}\n";
                stats += $"攻撃力: x{armorData.attackPowerModifier:F1}\n";
                stats += $"飛行可能: {(armorData.canFly ? "はい" : "いいえ")}\n";
                stats += $"ソードビット: {(armorData.canUseSwordBit ? "使用可能" : "使用不可")}\n";
                // 他のステータスも追加
                armorStatsText.text = stats;
            }
        }
    }

    /// <summary>
    /// 説明パネルを非表示にする
    /// </summary>
    public void HideDescription()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 決定ボタンがクリックされたときに呼び出される
    /// </summary>
    void OnConfirmButtonClicked()
    {
        if (_currentlySelectedArmors.Count > 0)
        {
            // ArmorManagerに選択されたアーマーを渡す
            if (ArmorManager.Instance != null)
            {
                ArmorManager.Instance.selectedArmors.Clear();
                foreach (ArmorData armor in _currentlySelectedArmors)
                {
                    ArmorManager.Instance.selectedArmors.Add(armor);
                }
                Debug.Log("アーマー選択完了！ゲームシーンへ遷移します。");

                // GameManagerを通して選択されたステージへ遷移
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GoToSelectedStage();
                }
                else
                {
                    Debug.LogError("GameManagerが見つかりません。ステージに遷移できません。");
                }
            }
            else
            {
                Debug.LogError("ArmorManagerが見つかりません。");
            }
        }
        else
        {
            Debug.LogWarning("アーマーが選択されていません。");
        }
    }
}