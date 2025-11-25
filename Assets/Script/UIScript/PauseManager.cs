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

        // 【追加】ゲーム開始時にカーソルを表示し、ロックを解除する
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // --- Update: ESCキーでのポーズ切り替え ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuUI == null) return;

            if (isPaused)
            {
                // サウンドパネルが開いていたら、ESCでポーズメニューに戻る
                if (soundPanelUI != null && soundPanelUI.activeSelf)
                {
                    GoToPauseMenu();
                }
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
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.interactable = false;
            pauseMenuCanvasGroup.blocksRaycasts = false;
        }

        Time.timeScale = 1f;
        isPaused = false;
        Cursor.visible = true; // カーソルを常に表示
        Cursor.lockState = CursorLockMode.None; // カーソルを画面内にロックしない
    }

    public void PauseGame()
    {
        // ポーズを開始する際、SoundPanelが誤って開いていたら閉じる
        if (soundPanelUI != null) soundPanelUI.SetActive(false);

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

    // ? NEW: ボタンSE再生用の共通メソッド
    /// <summary>
    /// ボタンクリックSEを再生します。
    /// </summary>
    public void PlayButtonClickSFX()
    {
        if (AudioManager.Instance != null && buttonClickSFX != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
    }

    // --- ボタンが呼び出す機能 ---

    public void RestartStage()
    {
        PlayButtonClickSFX(); // ? SEを再生
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitleScene()
    {
        PlayButtonClickSFX(); // ? SEを再生
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    public void ToggleSoundPanel()
    {
        PlayButtonClickSFX(); // ? SEを再生
        if (soundPanelUI != null)
        {
            soundPanelUI.SetActive(true);
            soundPanelUI.transform.SetAsLastSibling();
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            Debug.Log("SoundPanelUI を開きました。");
        }
        else
        {
            Debug.LogError("サウンドパネルが設定されていません。");
        }
    }

    public void GoToPauseMenu()
    {
        PlayButtonClickSFX(); // ? SEを再生
        if (soundPanelUI != null) soundPanelUI.SetActive(false);
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            pauseMenuUI.transform.SetAsLastSibling();
            Debug.Log("サウンドパネルを閉じ、ポーズメニューに戻りました。");
        }
    }

    public void QuitGame()
    {
        PlayButtonClickSFX(); // ? SEを再生
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("ゲーム終了処理が呼び出されました。ビルドされたアプリケーションでのみ終了します。");
#endif
    }
}