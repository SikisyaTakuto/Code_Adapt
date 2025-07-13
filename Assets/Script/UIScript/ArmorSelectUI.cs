// ArmorSelectUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; // Coroutineのために必要
using UnityEngine.SceneManagement; // SceneManagerのために追加

public class ArmorSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform armorModelParent; // 3Dモデルをインスタンス化する親オブジェクト
    public Button confirmButton; // 決定ボタン
    public Text selectedArmorsText; // 選択されたアーマー名を表示するUIテキスト

    [Header("Description UI")]
    public GameObject descriptionPanel; // 説明パネル (非表示/表示を切り替える)
    public Text armorNameText; // アーマー名表示用テキスト
    public Text armorDescriptionText; // アーマー説明表示用テキスト
    public Text armorStatsText; // アーマーステータス表示用テキスト

    private List<GameObject> _instantiatedArmorModels = new List<GameObject>(); // シーンにインスタンス化されたアーマーモデル
    private List<ArmorData> _currentlySelectedArmors = new List<ArmorData>(); // 現在選択中のアーマーデータリスト

    // 各アーマーデータに対応するArmorSelectableコンポーネントを保持
    private Dictionary<ArmorData, ArmorSelectable> _armorDataToSelectableMap = new Dictionary<ArmorData, ArmorSelectable>();

    private const int MAX_SELECTED_ARMORS = 3; // 選択できるアーマーの最大数

    [Header("Scene Transition")]
    [SerializeField, Tooltip("決定ボタンクリック後のSE再生時間（秒）")]
    private float confirmSEPlayDuration = 0.5f; // SEの長さに応じて調整
    public string nextSceneName = "GameScene"; // 決定後に遷移するシーン名

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
        _armorDataToSelectableMap.Clear(); // マップもクリア

        if (ArmorManager.Instance == null || ArmorManager.Instance.allAvailableArmors == null || ArmorManager.Instance.allAvailableArmors.Count == 0)
        {
            Debug.LogError("利用可能なアーマーデータがありません。ArmorManagerにArmorDataを設定してください。");
            return;
        }

        // 仮の配置オフセットと間隔
        float startX = -(ArmorManager.Instance.allAvailableArmors.Count - 1)*1.5f; // 中央に寄せるための調整
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

                // ArmorSelectableスクリプトをアタッチ (ColliderはArmorSelectable内で処理される)
                ArmorSelectable selectable = armorModelInstance.AddComponent<ArmorSelectable>();
                selectable.armorData = armorData;
                selectable.armorSelectUI = this;
                _armorDataToSelectableMap.Add(armorData, selectable); // マップに追加
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
    public void OnArmorClicked(ArmorData clickedArmor)
    {
        // クリック音を再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

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
                // 選択できない場合は音は鳴らすが、選択状態は更新しない
                // この場合、一時的なハイライトを解除する必要がある
                UpdateAllArmorHighlights(); // 全アーマーのハイライト状態を更新して一時ハイライトを解除
                return;
            }
        }
        UpdateSelectedArmorsUI();
        UpdateAllArmorHighlights(); // 全アーマーのハイライト状態を更新
    }

    /// <summary>
    /// マウスダウン中の一時的なハイライトを制御
    /// </summary>
    public void HighlightTemporary(ArmorData armorToHighlight)
    {
        foreach (var entry in _armorDataToSelectableMap)
        {
            // クリックされたものだけ一時的にハイライト
            entry.Value.SetTemporaryHighlight(entry.Key == armorToHighlight);
        }
    }

    /// <summary>
    /// 全てのアーマーモデルのハイライト状態を、現在の選択リストに基づいて更新する
    /// </summary>
    private void UpdateAllArmorHighlights()
    {
        foreach (var entry in _armorDataToSelectableMap)
        {
            // 現在選択リストに含まれているかどうかでハイライトを切り替える
            entry.Value.SetHighlight(_currentlySelectedArmors.Contains(entry.Key));
        }
    }

    /// <summary>
    /// 指定されたアーマーが現在選択リストに含まれているかを確認
    /// </summary>
    public bool IsArmorSelected(ArmorData armorData)
    {
        return _currentlySelectedArmors.Contains(armorData);
    }

    private void UpdateSelectedArmorsUI()
    {
        if (selectedArmorsText != null)
        {
            string displayText = "選択中のアーマー:\n";
            for (int i = 0; i < MAX_SELECTED_ARMORS; i++)
            {
                if (i < _currentlySelectedArmors.Count)
                {
                    displayText += $"{i + 1}. <color=yellow>{_currentlySelectedArmors[i].armorName}</color>\n"; // 選択されたアーマー名を強調
                }
                else
                {
                    displayText += $"{i + 1}. (未選択)\n";
                }
            }
            selectedArmorsText.text = displayText;
        }

        // 決定ボタンの有効/無効を切り替える (例: 3つ選択されたら有効)
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
                string stats = "--- ステータス --- \n";
                stats += $"移動速度: <color=#00FF00>x{armorData.moveSpeedModifier:F1}</color>\n"; // 緑色
                stats += $"攻撃力: <color=#FF0000>x{armorData.attackPowerModifier:F1}</color>\n"; // 赤色
                stats += $"飛行可能: {(armorData.canFly ? "<color=blue>はい</color>" : "いいえ")}\n"; // 青色
                stats += $"ソードビット: {(armorData.canUseSwordBit ? "<color=cyan>使用可能</color>" : "使用不可")}\n"; // シアン色
                armorStatsText.text = stats;
            }
        
    }

    void OnConfirmButtonClicked()
    {
        if (_currentlySelectedArmors.Count == MAX_SELECTED_ARMORS) // 3つ選択されていることを最終確認
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSE(); // 決定音を鳴らす
                StartCoroutine(TransitionToGameSceneAfterSE()); // 音が鳴り終わってからシーン遷移
            }
            else
            {
                Debug.LogError("AudioManagerが見つかりません。SEなしでシーン遷移します。");
                TransitionToGameScene(); // AudioManagerがない場合
            }
        }
        else
        {
            Debug.LogWarning($"アーマーが{MAX_SELECTED_ARMORS}個選択されていません。現在: {_currentlySelectedArmors.Count}個");
            if (AudioManager.Instance != null)
            {
                // エラーSEなどがあれば再生
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
            Debug.Log($"アーマー選択完了！ゲームシーン '{nextSceneName}' へ遷移します。");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError("GameManagerが見つかりません。直接シーンをロードします。");
                SceneManager.LoadScene(nextSceneName);
            }
        }
        else
        {
            Debug.LogError("ArmorManagerが見つかりません。アーマー情報を引き継げません。");
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