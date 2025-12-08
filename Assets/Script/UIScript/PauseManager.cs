using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    // --- シングルトン化のための静的変数 ---
    public static PauseManager Instance;

    // --- Inspectorから設定する変数 ---
    [Header("UI References")]
    [Tooltip("ポーズメニューのルートUIパネル (Menupanel) を設定してください。")]
    public GameObject pauseMenuUI;
    [Tooltip("オプションなどのサブパネル (SoundPanel) を設定してください。")]
    public GameObject soundPanelUI;
    [Tooltip("感度設定などのサブパネル (SensitivitySettingPanel) を設定してください。")]
    public GameObject sensitivitySettingPanelUI;
    [Tooltip("操作説明などのサブパネル (ExplanationPanel) を設定してください。")]
    public GameObject explanationPanelUI;
    [Tooltip("操作手順などのさらに深いサブパネル (OperatingInstructionsPanel) を設定してください。")]
    public GameObject operatingInstructionsPanelUI;
    [Header("Explanation Details")]
    [Tooltip("プレイヤー説明パネルを設定してください。")]
    public GameObject playerExplanationPanelUI;
    [Tooltip("エネミー説明パネルを設定してください。")]
    public GameObject enemyExplanationPanelUI;
    [Tooltip("追加情報パネル (Extra) を設定してください。")]
    public GameObject extraPanelUI;
    [Header("Player Explanation Sub Panels")]
    [Tooltip("バランスタイプの説明パネルを設定してください。")]
    public GameObject BalancePanelUI;
    [Tooltip("バスタータイプの説明パネルを設定してください。")]
    public GameObject BusterPanelUI;
    [Tooltip("スピードタイプの説明パネルを設定してください。")]
    public GameObject SpeedPanelUI;
    [Tooltip("キーボード操作説明パネルを設定してください。")]
    public GameObject KeyBordPanelUI;
    [Tooltip("ゲームパッド操作説明パネルを設定してください。")]
    public GameObject GamePadPanelUI;

    [Header("Audio Settings")]
    [Tooltip("ボタンをクリックしたときに再生するSEクリップ")]
    public AudioClip buttonClickSFX;

    private bool isPaused = false;
    private CanvasGroup pauseMenuCanvasGroup;

    private Canvas rootCanvas;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (pauseMenuUI != null)
        {
            // pauseMenuUIの最も近い親Canvasを取得する
            rootCanvas = pauseMenuUI.GetComponentInParent<Canvas>();

            if (rootCanvas != null)
            {
                // 取得したCanvasのSorting Orderを最大値に設定し、最前面に持ってくる
                rootCanvas.sortingOrder = 9999;
            }
            else
            {
                Debug.LogError("PauseManager: pauseMenuUIの親または自身にCanvasコンポーネントが見つかりませんでした。最前面化できません。");
            }

            // CanvasGroupを取得または追加
            pauseMenuCanvasGroup = pauseMenuUI.GetComponent<CanvasGroup>();
            if (pauseMenuCanvasGroup == null)
            {
                pauseMenuCanvasGroup = pauseMenuUI.AddComponent<CanvasGroup>();
            }

            // 初期状態はポーズ解除
            ResumeGame();
        }
        else
        {
            Debug.LogError("PauseManager: pauseMenuUIがInspectorに設定されていません。ポーズ機能は動作しません。");
        }
    }

    // --- Update: ESCキーでのポーズ切り替え ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuUI == null) return;

            if (isPaused)
            {
                // 5階層目のパネル（Balance, Buster, Speed）が開いていたら、4階層目(PlayerExplanationPanel)に戻る
                if ((BalancePanelUI != null && BalancePanelUI.activeSelf) ||
                    (BusterPanelUI != null && BusterPanelUI.activeSelf) ||
                    (SpeedPanelUI != null && SpeedPanelUI.activeSelf))
                {
                    GoToPlayerExplanationPanel(); // PlayerExplanationPanelに戻る
                }
                // 4階層目のパネル（KeyBordPanel, GamePadPanel）が開いていたら、3階層目(OperatingInstructionsPanel)に戻る
                else if ((KeyBordPanelUI != null && KeyBordPanelUI.activeSelf) ||
                          (GamePadPanelUI != null && GamePadPanelUI.activeSelf))
                {
                    GoToOperatingInstructionsPanel();
                }
                // 3階層目のパネル（OperatingInstructions, PlayerExplanation, EnemyExplanation, Extra）が開いていたら、2階層目(ExplanationPanel)に戻る
                else if ((operatingInstructionsPanelUI != null && operatingInstructionsPanelUI.activeSelf) ||
                    (playerExplanationPanelUI != null && playerExplanationPanelUI.activeSelf) ||
                    (enemyExplanationPanelUI != null && enemyExplanationPanelUI.activeSelf) ||
                    (extraPanelUI != null && extraPanelUI.activeSelf))
                {
                    GoToExplanationPanel();
                }
                // 2階層目のパネルが開いていたら、1階層目(ポーズメニュー)に戻る
                else if ((soundPanelUI != null && soundPanelUI.activeSelf) ||
                         (sensitivitySettingPanelUI != null && sensitivitySettingPanelUI.activeSelf) || // ★ 修正
                         (explanationPanelUI != null && explanationPanelUI.activeSelf))
                {
                    GoToPauseMenu();
                }
                // ポーズメニュー(1階層目)が開いていたら、ゲームに戻る
                else
                {
                    ResumeGame(); // ポーズメニューからゲームへ戻る
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    /// <summary>
    /// ポーズ状態を切り替えます。歯車ボタンなど、UIボタンのクリックイベントにアタッチします。
    /// </summary>
    public void TogglePauseState()
    {
        PlayButtonClickSFX();

        if (isPaused)
        {
            // ポーズ中の場合はゲームに戻る（メニュー階層の戻る処理はESCキーに任せる）
            ResumeGame();
        }
        else
        {
            // ポーズ解除中の場合はポーズメニューを開く
            PauseGame();
        }
    }
    // --- ポーズ/ポーズ解除処理 ---
    public void ResumeGame()
    {
        // ★ 修正: 新しいパネルを非表示
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
        if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        // 新しいパネルを非表示
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
        if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
        // PlayerExplanationPanelUIの子パネルを非表示
        if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
        if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
        if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.interactable = false;
            pauseMenuCanvasGroup.blocksRaycasts = false;
        }

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void PauseGame()
    {
        // ポーズを開始する際、すべてのサブパネルを閉じてメインポーズメニューに戻す
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
        if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        // 新しいパネルを非表示
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
        if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
        // PlayerExplanationPanelUIの子パネルを非表示
        if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
        if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
        if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.interactable = true;
            pauseMenuCanvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;

        if (rootCanvas != null)
        {
            rootCanvas.sortingOrder = 9999;
        }

        isPaused = true;
    }

    // ボタンSE再生用の共通メソッド
    /// <summary>
    /// ボタンクリックSEを再生します。
    /// </summary>
    public void PlayButtonClickSFX()
    {
        if (/*AudioManager.Instance != null && */buttonClickSFX != null)
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(buttonClickSFX);
        }
    }

    // --- NEW: 感度設定パネル表示/非表示の切り替え (ボタンアクション) ---
    /// <summary>
    /// SensitivitySettingPanelを表示し、ポーズメニューの他の兄弟パネルを閉じます。
    /// </summary>
    public void ToggleSensitivitySettingPanel()
    {
        PlayButtonClickSFX();
        if (sensitivitySettingPanelUI != null)
        {
            sensitivitySettingPanelUI.SetActive(true);
            sensitivitySettingPanelUI.transform.SetAsLastSibling();

            // 他の2階層目のパネルを閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);

            // 3階層目以下のパネルもすべて閉じる
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            if (pauseMenuUI != null) pauseMenuUI.SetActive(false); // メインポーズメニューを非表示

            Debug.Log("SensitivitySettingPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("感度設定パネルが設定されていません。");
        }
    }

    // サウンドパネル表示/非表示の切り替え (ボタンアクション)
    public void ToggleSoundPanel()
    {
        PlayButtonClickSFX();
        if (soundPanelUI != null)
        {
            soundPanelUI.SetActive(true);
            soundPanelUI.transform.SetAsLastSibling();

            // 他のパネルを閉じる
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("SoundPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("サウンドパネルが設定されていません。");
        }
    }

    // Explanationパネル表示/非表示の切り替え (ボタンアクション)
    public void ToggleExplanationPanel()
    {
        PlayButtonClickSFX();
        if (explanationPanelUI != null)
        {
            explanationPanelUI.SetActive(true);
            explanationPanelUI.transform.SetAsLastSibling();
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("ExplanationPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("操作説明パネル (ExplanationPanelUI) が設定されていません。");
        }
    }

    // OperatingInstructionsPanel表示/非表示の切り替え (ボタンアクション)
    public void ToggleOperatingInstructionsPanel()
    {
        PlayButtonClickSFX();
        if (operatingInstructionsPanelUI != null)
        {
            operatingInstructionsPanelUI.SetActive(true);
            operatingInstructionsPanelUI.transform.SetAsLastSibling();

            // ExplanationPanelの子パネル（プレイヤー/エネミーなど）を閉じる
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            // OperatingInstructionsPanelの子パネル（キーボード/ゲームパッド）を閉じる
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
            // PlayerExplanationPanelUIの子パネルを非表示
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // ポーズメニューの他の兄弟パネルを閉じる (サウンドパネルなど)
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true); // 親は表示状態を維持
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("OperatingInstructionsPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("操作手順パネル (OperatingInstructionsPanelUI) が設定されていません。");
        }
    }

    // PlayerExplanationPanel表示/非表示の切り替え (ボタンアクション)
    public void TogglePlayerExplanationPanel()
    {
        PlayButtonClickSFX();
        if (playerExplanationPanelUI != null)
        {
            playerExplanationPanelUI.SetActive(true);
            playerExplanationPanelUI.transform.SetAsLastSibling();

            // OperatingInstructionsPanelの子パネルを閉じる
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // ExplanationPanelの他の子パネルを閉じる
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);

            // PlayerExplanationPanelUIの子パネルをすべて閉じる (初期状態)
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // 親パネルやポーズメニューを閉じる
            // ★ 修正: 新しいパネルを閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true); // 親は表示状態を維持
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("PlayerExplanationPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("プレイヤー説明パネル (PlayerExplanationPanelUI) が設定されていません。");
        }
    }

    // EnemyExplanationPanel表示/非表示の切り替え (ボタンアクション)
    public void ToggleEnemyExplanationPanel()
    {
        PlayButtonClickSFX();
        if (enemyExplanationPanelUI != null)
        {
            enemyExplanationPanelUI.SetActive(true);
            enemyExplanationPanelUI.transform.SetAsLastSibling();

            // OperatingInstructionsPanelの子パネルを閉じる
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // ExplanationPanelの他の子パネルを閉じる
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            // PlayerExplanationPanelUIの子パネルを非表示
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // 親パネルやポーズメニューを閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true); // 親は表示状態を維持
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("EnemyExplanationPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("エネミー説明パネル (EnemyExplanationPanelUI) が設定されていません。");
        }
    }

    // ExtraPanel表示/非表示の切り替え (ボタンアクション)
    public void ToggleExtraPanel()
    {
        PlayButtonClickSFX();
        if (extraPanelUI != null)
        {
            extraPanelUI.SetActive(true);
            extraPanelUI.transform.SetAsLastSibling();

            // OperatingInstructionsPanelの子パネルを閉じる
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // ExplanationPanelの他の子パネルを閉じる
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            // PlayerExplanationPanelUIの子パネルを非表示
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // 親パネルやポーズメニューを閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true); // 親は表示状態を維持
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("ExtraPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("追加情報パネル (ExtraPanelUI) が設定されていません。");
        }
    }
    /// <summary>
    /// KeyBordPanelを表示し、OperatingInstructionsPanel内の他のパネルを閉じます。
    /// </summary>
    public void ToggleKeyBordPanel()
    {
        PlayButtonClickSFX();
        if (KeyBordPanelUI != null)
        {
            KeyBordPanelUI.SetActive(true);
            KeyBordPanelUI.transform.SetAsLastSibling(); // 最前面に表示

            // OperatingInstructionsPanel内の他の操作パネルを閉じる
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // その他のパネルは操作の階層に応じて制御する（基本的に非表示でOK）
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            // PlayerExplanationPanelUIの子パネルを非表示
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // 親パネルは表示状態を維持
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(true);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("KeyBordPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("キーボードパネル (KeyBordPanelUI) が設定されていません。");
        }
    }
    /// <summary>
    /// GamePadPanelを表示し、OperatingInstructionsPanel内の他のパネルを閉じます。
    /// </summary>
    public void ToggleGamePadPanel()
    {
        PlayButtonClickSFX();
        if (GamePadPanelUI != null)
        {
            GamePadPanelUI.SetActive(true);
            GamePadPanelUI.transform.SetAsLastSibling(); // 最前面に表示

            // OperatingInstructionsPanel内の他の操作パネルを閉じる
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);

            // その他のパネルは操作の階層に応じて制御する（基本的に非表示でOK）
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            // PlayerExplanationPanelUIの子パネルを非表示
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // 親パネルは表示状態を維持
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(true);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("GamePadPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("ゲームパッドパネル (GamePadPanelUI) が設定されていません。");
        }
    }
    /// <summary>
    /// BalancePanelを表示し、PlayerExplanationPanel内の他のパネルを閉じます。
    /// </summary>
    public void ToggleBalancePanel()
    {
        PlayButtonClickSFX();
        if (BalancePanelUI != null)
        {
            BalancePanelUI.SetActive(true);
            BalancePanelUI.transform.SetAsLastSibling(); // 最前面に表示

            // PlayerExplanationPanel内の他のパネルを閉じる
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // その他の兄弟パネルや親パネルは状態を維持
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(true);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // 親パネルは表示状態を維持
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("BalancePanelUI を開きました。");
        }
        else
        {
            Debug.LogError("バランスパネル (BalancePanelUI) が設定されていません。");
        }
    }

    /// <summary>
    /// BusterPanelを表示し、PlayerExplanationPanel内の他のパネルを閉じます。
    /// </summary>
    public void ToggleBusterPanel()
    {
        PlayButtonClickSFX();
        if (BusterPanelUI != null)
        {
            BusterPanelUI.SetActive(true);
            BusterPanelUI.transform.SetAsLastSibling(); // 最前面に表示

            // PlayerExplanationPanel内の他のパネルを閉じる
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

            // その他の兄弟パネルや親パネルは状態を維持
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(true);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // 親パネルは表示状態を維持
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("BusterPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("バスターパネル (BusterPanelUI) が設定されていません。");
        }
    }

    /// <summary>
    /// SpeedPanelを表示し、PlayerExplanationPanel内の他のパネルを閉じます。
    /// </summary>
    public void ToggleSpeedPanel()
    {
        PlayButtonClickSFX();
        if (SpeedPanelUI != null)
        {
            SpeedPanelUI.SetActive(true);
            SpeedPanelUI.transform.SetAsLastSibling(); // 最前面に表示

            // PlayerExplanationPanel内の他のパネルを閉じる
            if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
            if (BusterPanelUI != null) BusterPanelUI.SetActive(false);

            // その他の兄弟パネルや親パネルは状態を維持
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(true);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
            if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);

            // 親パネルは表示状態を維持
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("SpeedPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("スピードパネル (SpeedPanelUI) が設定されていません。");
        }
    }

    // サブパネルを閉じ、ポーズメニューに戻る (ESCキー、または「戻る」ボタンアクション)
    public void GoToPauseMenu()
    {
        PlayButtonClickSFX();
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);
        if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
        if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
        if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
        if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
        if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            pauseMenuUI.transform.SetAsLastSibling();
            Debug.Log("サブパネルを閉じ、ポーズメニューに戻りました。");
        }
    }

    // OperatingInstructionsPanel、PlayerExplanationPanel、EnemyExplanationPanel、ExtraPanelを閉じ、ExplanationPanelに戻る
    public void GoToExplanationPanel()
    {
        PlayButtonClickSFX();
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
        if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
        if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
        if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
        if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);


        if (explanationPanelUI != null)
        {
            explanationPanelUI.SetActive(true);
            explanationPanelUI.transform.SetAsLastSibling();
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false); // 念のため

            // ★ 修正: 2階層目の他のパネルも閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (sensitivitySettingPanelUI != null) sensitivitySettingPanelUI.SetActive(false);

            Debug.Log("詳細パネルを閉じ、操作説明パネルに戻りました。");
        }
    }
    /// <summary>
    /// キーボード/ゲームパッドパネルを閉じ、操作手順パネルに戻ります。（ESCキー用）
    /// </summary>
    public void GoToOperatingInstructionsPanel()
    {
        PlayButtonClickSFX();
        if (KeyBordPanelUI != null) KeyBordPanelUI.SetActive(false);
        if (GamePadPanelUI != null) GamePadPanelUI.SetActive(false);
        if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
        if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
        if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

        if (operatingInstructionsPanelUI != null)
        {
            operatingInstructionsPanelUI.SetActive(true);
            operatingInstructionsPanelUI.transform.SetAsLastSibling();

            // 親パネルは表示状態を維持
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("詳細操作パネルを閉じ、操作手順パネルに戻りました。");
        }
        else
        {
            // OperatingInstructionsPanelが設定されていない場合は、さらに親のExplanationPanelに戻る
            GoToExplanationPanel();
        }
    }
    /// <summary>
    /// バランス/バスター/スピードパネルを閉じ、プレイヤー説明パネルに戻ります。（ESCキー用）
    /// </summary>
    public void GoToPlayerExplanationPanel()
    {
        PlayButtonClickSFX();
        // 5階層目のパネルをすべて非表示
        if (BalancePanelUI != null) BalancePanelUI.SetActive(false);
        if (BusterPanelUI != null) BusterPanelUI.SetActive(false);
        if (SpeedPanelUI != null) SpeedPanelUI.SetActive(false);

        if (playerExplanationPanelUI != null)
        {
            playerExplanationPanelUI.SetActive(true);
            playerExplanationPanelUI.transform.SetAsLastSibling();

            // 親パネルは表示状態を維持
            if (explanationPanelUI != null) explanationPanelUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("詳細プレイヤー情報パネルを閉じ、プレイヤー説明パネルに戻りました。");
        }
        else
        {
            // PlayerExplanationPanelが設定されていない場合は、さらに親のExplanationPanelに戻る
            GoToExplanationPanel();
        }
    }

    /// <summary>
    /// 現在のゲームシーンをアンロードし、指定されたタイトルシーンをロードします。
    /// </summary>
    public void GoToTitle()
    {
        PlayButtonClickSFX();
        Time.timeScale = 1f; // タイムスケールを元に戻す

        // タイトルシーンの名前に置き換えてください
        const string titleSceneName = "TitleScene";

        try
        {
            SceneManager.LoadScene(titleSceneName);
            Debug.Log("タイトルシーン (" + titleSceneName + ") へ移動します。");
        }
        catch (System.Exception e)
        {
            Debug.LogError("タイトルシーンのロードに失敗しました。シーン名が正しいか、Build Settingsに含まれているか確認してください。エラー: " + e.Message);
        }
    }
    public void QuitGame()
    {
        PlayButtonClickSFX();
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("ゲーム終了処理が呼び出されました。ビルドされたアプリケーションでのみ終了します。");
#endif
    }
}