using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// アーマー選択シーンの全体管理、UIの更新、モデル表示、シーン遷移を制御します。
/// </summary>
public class ArmorSelectManager : MonoBehaviour
{
    // --- UI/表示設定 ---
    [Header("UI References")]
    public Text descriptionText;       // ナビゲーターの吹き出し内の説明文
    public Button decisionButton;      // 決定ボタン

    [Header("Armor Buttons")]
    public Button[] armorButtons = new Button[4]; // 選択肢となるボタン (1, 2, 3, 4)

    // ★追加: 各アーマーボタンの上にある強調表示用のImage
    [Tooltip("各アーマーボタンに対応するハイライト/枠線用のImageを設定")]
    public Image[] armorHighlightImages = new Image[4];

    [Header("3D Model Display")]
    [Tooltip("Normal(0), Buster(1), Speed(2), Random(3) の順で設定")]
    public GameObject[] armorModels = new GameObject[4]; // 表示する3Dモデルの配列
    public float modelRotationSpeed = 30f; // 3Dモデルの回転速度 (Y軸)

    // --- データと状態 ---
    // ★追加: 選択されたアーマーのインデックスを格納するリスト
    private List<int> selectedArmorList = new List<int>();
    // ★変更: 最大選択数を3に設定
    private const int MaxSelectionCount = 3;

    // ★追加: マウスオーバーされているアーマーのインデックスを格納するリスト
    private List<int> hoveredArmorList = new List<int>();

    // ★用途変更: 現在、説明文を表示しているアーマーのインデックス (選択リストの最後の要素)
    private int currentDisplayIndex = 0;

    private bool isTutorialSelected = false;

    private readonly string[] armorNames = { "ノーマル", "バスター", "スピード", "ランダム" };
    private readonly string[] armorDescriptions =
    {
        "【ノーマル】\nバランスの取れた標準的なアーマーです。",
        "【バスター】\n防御力に特化し、攻撃力が向上します。",
        "【スピード】\n機動性に優れ、エネルギー回復速度が向上します。",
        "【ランダム】\n開始時にランダムなアーマーが選択されます。"
    };

    // ★追加: 初期表示用の説明文
    private const string InitialDescription = "クリックしてアーマーを3つ選びましょう。\n選択されたアーマーは黄色く強調表示されます。\n\n**マウスカーソルを合わせる**と詳細な説明が見られます。";


    // 決定ボタンを押した後のシーン名
    private const string GameSceneName = "TutorialScene";

    // 選択されたアーマーのデータを保持・引き継ぐためのキー (単一選択から変更が必要なら、ここも修正が必要)
    private const string SelectedArmorKey = "SelectedArmorIndex";

    // Y軸180度の初期回転値をQuaternionで定義
    private readonly Quaternion initialRotation = Quaternion.Euler(0f, 180f, 0f);

    void Start()
    {
        EnableAllArmorModels();
        SetArmorButtonsTransparent();

        // ★修正点1: 初期選択の削除
        // 最初のアーマー(0)をリストに追加する処理を削除し、初期状態を「何も選択されていない」状態にする

        // ★修正点2: 初期表示テキストの更新
        UpdateDescriptionTextInitial();

        UpdateDecisionButtonState();

        for (int i = 0; i < armorButtons.Length; i++)
        {
            int index = i;
            if (armorButtons[i] != null)
            {
                armorButtons[i].onClick.RemoveAllListeners();
                armorButtons[i].onClick.AddListener(() => OnArmorButtonClicked(index));

                // ★追加: マウスオーバーイベントの設定
                SetupHoverEvents(armorButtons[i], index);
            }
        }

        if (decisionButton != null)
        {
            decisionButton.onClick.RemoveAllListeners();
            decisionButton.onClick.AddListener(OnDecisionButtonClicked);
        }

        UpdateUIEmphasis(); // ボタンの強調表示を初期化 (全て透明になる)
    }

    /// <summary>
    /// 各ButtonにPointerEnterとPointerExitイベントを設定します。
    /// </summary>
    private void SetupHoverEvents(Button button, int index)
    {
        // EventTriggerコンポーネントを取得または追加
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // PointerEnterイベントの追加
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) => OnArmorPointerEnter(index));
        trigger.triggers.Add(entryEnter);

        // PointerExitイベントの追加
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((eventData) => OnArmorPointerExit(index));
        trigger.triggers.Add(entryExit);
    }

    /// <summary>
    /// マウスカーソルがボタンに入ったときに呼ばれます。
    /// </summary>
    public void OnArmorPointerEnter(int index)
    {
        if (!hoveredArmorList.Contains(index))
        {
            hoveredArmorList.Add(index);
        }

        // マウスオーバーしたアーマーの説明を表示
        UpdateDescriptionText(index);
    }

    /// <summary>
    /// マウスカーソルがボタンから出たときに呼ばれます。
    /// </summary>
    public void OnArmorPointerExit(int index)
    {
        hoveredArmorList.Remove(index);

        // カーソルが離れた後、選択中のアーマーがあればその説明に戻す
        if (selectedArmorList.Any())
        {
            // リストの最後に選択されたアーマーの説明に戻る
            currentDisplayIndex = selectedArmorList.Last();
            UpdateDescriptionText(currentDisplayIndex);
        }
        else
        {
            // 何も選択されていない場合は初期説明文に戻す
            UpdateDescriptionTextInitial();
        }

        // マウスオーバーが解除されたとき、そのモデルが選択されていない場合のみ回転を初期化（停止/正面向きに戻す）
        if (!selectedArmorList.Contains(index))
        {
            if (index < armorModels.Length && armorModels[index] != null)
            {
                armorModels[index].transform.localRotation = initialRotation;
            }
        }
    }


    /// <summary>
    /// アーマーボタンのImageコンポーネントを透明にします。
    /// </summary>
    private void SetArmorButtonsTransparent()
    {
        // 完全に透明な色 (R=1, G=1, B=1, A=0)
        Color transparentColor = new Color(1f, 1f, 1f, 0f);

        for (int i = 0; i < armorButtons.Length; i++)
        {
            if (armorButtons[i] != null)
            {
                Image buttonImage = armorButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    // 完全に透明にする
                    buttonImage.color = transparentColor;
                }
            }
        }
    }
    // ------------------------------------

    /// <summary>
    /// シーン開始時に全てのアーマーモデルを有効化し、初期回転を設定します。
    /// </summary>
    private void EnableAllArmorModels()
    {
        for (int i = 0; i < armorModels.Length; i++)
        {
            if (armorModels[i] != null)
            {
                armorModels[i].SetActive(true);
                armorModels[i].transform.localRotation = initialRotation;
            }
        }
    }

    void Update()
    {
        // 3Dモデルの回転処理
        for (int i = 0; i < armorModels.Length; i++)
        {
            // 回転させるのは、マウスオーバーされている **かつ** 選択されていないモデルのみ
            bool isHovered = hoveredArmorList.Contains(i);
            bool isSelected = selectedArmorList.Contains(i);

            // 選択されていない（黄色ではない）モデルが、マウスオーバーされている場合のみ回転
            bool shouldRotate = isHovered && !isSelected;

            if (shouldRotate && armorModels[i] != null)
            {
                // 回転させる
                armorModels[i].transform.Rotate(Vector3.up, modelRotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

    /// <summary>
    /// アーマー選択ボタンがクリックされたときに呼ばれます。
    /// </summary>
    /// <param name="index">クリックされたアーマーのインデックス (0～3)</param>
    public void OnArmorButtonClicked(int index)
    {
        bool wasPreviouslySelected = selectedArmorList.Contains(index);

        if (wasPreviouslySelected)
        {
            // 選択解除 (黄色 -> 透明)
            selectedArmorList.Remove(index);

            // モデルの回転を初期値に戻す (解除されたモデルのみ)
            if (index < armorModels.Length && armorModels[index] != null)
            {
                armorModels[index].transform.localRotation = initialRotation;
            }
            Debug.Log($"アーマー {armorNames[index]} を解除しました。残り: {string.Join(", ", selectedArmorList.Select(i => armorNames[i]))}");
        }
        else
        {
            // ★修正点3: 選択数の制限チェックを追加
            if (selectedArmorList.Count >= MaxSelectionCount)
            {
                // 選択上限に達している場合は何もしない
                Debug.LogWarning($"選択上限({MaxSelectionCount}個)に達しています。");
                return;
            }

            // 選択 (透明 -> 黄色)
            selectedArmorList.Add(index);

            // 選択されたとき（黄色になったとき）にモデルを正面に向かせる
            if (index < armorModels.Length && armorModels[index] != null)
            {
                // ★選択された時点で回転を停止し、正面を向かせる
                armorModels[index].transform.localRotation = initialRotation;
            }

            Debug.Log($"アーマー {armorNames[index]} を追加しました。現在: {string.Join(", ", selectedArmorList.Select(i => armorNames[i]))}");
        }

        // 常に最新の選択/解除されたアーマーを説明文に表示する
        currentDisplayIndex = index;
        UpdateDescriptionText(currentDisplayIndex);

        // UIの強調表示を更新
        UpdateUIEmphasis();
        UpdateDecisionButtonState(); // ボタンの有効/無効状態を更新

        isTutorialSelected = false;
    }

    /// <summary>
    /// 選択リストに基づいて、強調表示用のImageを制御します。
    /// </summary>
    private void UpdateUIEmphasis()
    {
        for (int i = 0; i < armorHighlightImages.Length; i++)
        {
            if (armorHighlightImages[i] != null)
            {
                bool isSelected = selectedArmorList.Contains(i);

                // Imageの色を変更して、光っているように見せます
                Color targetColor = isSelected ? Color.yellow : Color.clear; // 選択中は黄色、解除中は透明

                // Imageを有効化し、色を適用
                armorHighlightImages[i].gameObject.SetActive(true);
                armorHighlightImages[i].color = targetColor;
            }
        }
    }


    /// <summary>
    /// 吹き出し内の説明文を更新します。（アーマー選択時/マウスオーバー時）
    /// </summary>
    private void UpdateDescriptionText(int index)
    {
        if (descriptionText != null)
        {
            descriptionText.text = armorDescriptions[index];
        }
    }

    /// <summary>
    /// 吹き出し内の説明文を初期表示用（チュートリアル）に更新します。
    /// </summary>
    private void UpdateDescriptionTextInitial()
    {
        if (descriptionText != null)
        {
            descriptionText.text = InitialDescription;
        }
    }

    /// <summary>
    /// 決定ボタンの状態を更新します。（選択リストに一つ以上のアーマーがあるかチェック）
    /// </summary>
    private void UpdateDecisionButtonState()
    {
        if (decisionButton == null) return;

        // 少なくとも1つのアーマーが選択されていれば決定ボタンを有効化
        bool canProceed = selectedArmorList.Count > 0;
        decisionButton.interactable = canProceed;

        // デバッグログで格納順を確認
        if (canProceed)
        {
            Debug.Log($"決定ボタン有効。格納順: {string.Join(", ", selectedArmorList.Select(i => armorNames[i]))}");
        }
        else
        {
            Debug.Log("決定ボタン無効。アーマーが選択されていません。");
        }
    }

    /// <summary>
    /// 決定ボタンがクリックされたときに呼ばれます。
    /// </summary>
    public void OnDecisionButtonClicked()
    {
        if (selectedArmorList.Count == 0)
        {
            Debug.LogWarning("アーマーが選択されていません。");
            return;
        }

        // 複数選択されていても、引き継ぐデータは最初の1つだけ、という前提はそのまま残しています
        int armorToPass = selectedArmorList.First();

        // 選択された全てのアーマー情報を引き継ぎたい場合は、この部分のロジックを修正してください
        // 例: 3つのIDをカンマ区切りの文字列にしてPlayerPrefsに保存するなど

        PlayerPrefs.SetInt(SelectedArmorKey, armorToPass);
        PlayerPrefs.Save();

        Debug.Log($"選択アーマー(格納順): {string.Join(", ", selectedArmorList.Select(i => armorNames[i]))} のうち、最初の {armorNames[armorToPass]} をゲームシーンへ渡します。");

        // メインゲームシーンへ遷移
        SceneManager.LoadScene(GameSceneName);
    }

    // 他のシーンから選択されたアーマーを取得するための静的メソッド
    public static int GetSelectedArmorIndex()
    {
        // デフォルト値として Normal (0) を返す
        return PlayerPrefs.GetInt(SelectedArmorKey, 0);
    }
}