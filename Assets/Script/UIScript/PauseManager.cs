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
    [Tooltip("操作説明などのサブパネル (ExplanationPanel) を設定してください。")]
    public GameObject explanationPanelUI;
    [Tooltip("操作手順などのさらに深いサブパネル (OperatingInstructionsPanel) を設定してください。")]
    public GameObject operatingInstructionsPanelUI;

    // ★ NEW: Player/Enemy/Extra Explanation Panelの参照を追加
    [Header("Explanation Details")]
    [Tooltip("プレイヤー説明パネルを設定してください。")]
    public GameObject playerExplanationPanelUI;      // ★ 追加
    [Tooltip("エネミー説明パネルを設定してください。")]
    public GameObject enemyExplanationPanelUI;       // ★ 追加
    [Tooltip("追加情報パネル (Extra) を設定してください。")]
    public GameObject extraPanelUI;                  // ★ 追加

    // ? NEW: Audio Settings
    [Header("Audio Settings")]
    [Tooltip("ボタンをクリックしたときに再生するSEクリップ")]
    public AudioClip buttonClickSFX;

    // --- 内部状態管理変数 ---
    private bool isPaused = false;
    private CanvasGroup pauseMenuCanvasGroup;

    // ★ 追加: 親Canvasを保持する変数
    private Canvas rootCanvas;

    // --- 1. Awakeでシングルトン処理を行う ---
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // シングルトン化の場合、必要に応じて
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // --- 2. StartでUIの初期設定と最前面設定を行う ---
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

        // 【修正】ゲーム開始時にカーソルを非表示/ロック
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- Update: ESCキーでのポーズ切り替え ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuUI == null) return;

            if (isPaused)
            {
                // ★ 3階層目のパネル（OperatingInstructions, PlayerExplanation, EnemyExplanation, Extra）が開いていたら、2階層目(ExplanationPanel)に戻る
                if ((operatingInstructionsPanelUI != null && operatingInstructionsPanelUI.activeSelf) ||
                    (playerExplanationPanelUI != null && playerExplanationPanelUI.activeSelf) || // ★ 追加
                    (enemyExplanationPanelUI != null && enemyExplanationPanelUI.activeSelf) ||   // ★ 追加
                    (extraPanelUI != null && extraPanelUI.activeSelf))                           // ★ 追加
                {
                    GoToExplanationPanel();
                }
                // 2階層目のパネルが開いていたら、1階層目(ポーズメニュー)に戻る
                else if ((soundPanelUI != null && soundPanelUI.activeSelf) ||
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

    // --- ポーズ/ポーズ解除処理 ---
    public void ResumeGame()
    {
        PlayButtonClickSFX(); // ボタンクリックSEを再生

        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        // ★ 追加: 新しいパネルを非表示
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        // ---
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.interactable = false;
            pauseMenuCanvasGroup.blocksRaycasts = false;
        }

        Time.timeScale = 1f;
        isPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void PauseGame()
    {
        // ポーズを開始する際、すべてのサブパネルを閉じる
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        // ★ 追加: 新しいパネルを非表示
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        // ---

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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ? NEW: ボタンSE再生用の共通メソッド
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

    // --- ボタンが呼び出す機能 ---

    public void RestartStage()
    {
        PlayButtonClickSFX();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitleScene()
    {
        PlayButtonClickSFX();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
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
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false); // ★ 追加
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);   // ★ 追加
            if (extraPanelUI != null) extraPanelUI.SetActive(false);                         // ★ 追加
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

            // 他のパネルを閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false); // ★ 追加
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);   // ★ 追加
            if (extraPanelUI != null) extraPanelUI.SetActive(false);                         // ★ 追加
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

            // ExplanationPanel以外を閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false); // ★ 念のため
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);   // ★ 念のため
            if (extraPanelUI != null) extraPanelUI.SetActive(false);                         // ★ 念のため
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("OperatingInstructionsPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("操作手順パネル (OperatingInstructionsPanelUI) が設定されていません。");
        }
    }

    // ★ NEW: PlayerExplanationPanel表示/非表示の切り替え (ボタンアクション)
    public void TogglePlayerExplanationPanel()
    {
        PlayButtonClickSFX();
        if (playerExplanationPanelUI != null)
        {
            playerExplanationPanelUI.SetActive(true);
            playerExplanationPanelUI.transform.SetAsLastSibling();

            // ExplanationPanel以外を閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("PlayerExplanationPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("プレイヤー説明パネル (PlayerExplanationPanelUI) が設定されていません。");
        }
    }

    // ★ NEW: EnemyExplanationPanel表示/非表示の切り替え (ボタンアクション)
    public void ToggleEnemyExplanationPanel()
    {
        PlayButtonClickSFX();
        if (enemyExplanationPanelUI != null)
        {
            enemyExplanationPanelUI.SetActive(true);
            enemyExplanationPanelUI.transform.SetAsLastSibling();

            // ExplanationPanel以外を閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (extraPanelUI != null) extraPanelUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("EnemyExplanationPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("エネミー説明パネル (EnemyExplanationPanelUI) が設定されていません。");
        }
    }

    // ★ NEW: ExtraPanel表示/非表示の切り替え (ボタンアクション)
    public void ToggleExtraPanel()
    {
        PlayButtonClickSFX();
        if (extraPanelUI != null)
        {
            extraPanelUI.SetActive(true);
            extraPanelUI.transform.SetAsLastSibling();

            // ExplanationPanel以外を閉じる
            if (soundPanelUI != null) soundPanelUI.SetActive(false);
            if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
            if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
            if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
            if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            Debug.Log("ExtraPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("追加情報パネル (ExtraPanelUI) が設定されていません。");
        }
    }

    // サブパネルを閉じ、ポーズメニューに戻る (ESCキー、または「戻る」ボタンアクション)
    public void GoToPauseMenu()
    {
        PlayButtonClickSFX();
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (explanationPanelUI != null) explanationPanelUI.SetActive(false);
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        // ★ 追加: 新しいパネルを非表示
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);
        if (extraPanelUI != null) extraPanelUI.SetActive(false);
        // ---

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
        // 3階層目のパネルをすべて非表示
        if (operatingInstructionsPanelUI != null) operatingInstructionsPanelUI.SetActive(false);
        if (playerExplanationPanelUI != null) playerExplanationPanelUI.SetActive(false);      // ★ 追加
        if (enemyExplanationPanelUI != null) enemyExplanationPanelUI.SetActive(false);        // ★ 追加
        if (extraPanelUI != null) extraPanelUI.SetActive(false);                              // ★ 追加

        if (explanationPanelUI != null)
        {
            explanationPanelUI.SetActive(true);
            explanationPanelUI.transform.SetAsLastSibling();
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false); // 念のため
            Debug.Log("詳細パネルを閉じ、操作説明パネルに戻りました。");
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