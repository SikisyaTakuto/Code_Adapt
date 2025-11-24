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
            // ★ 修正点1: pauseMenuUIの最も近い親Canvasを取得する
            rootCanvas = pauseMenuUI.GetComponentInParent<Canvas>();

            if (rootCanvas != null)
            {
                // ★ 修正点2: 取得したCanvasのSorting Orderを最大値に設定し、最前面に持ってくる
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

    // シーン遷移イベントの登録/解除は削除しました

    // --- Update: ESCキーでのポーズ切り替え ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuUI == null) return;

            if (isPaused)
            {
                ResumeGame();
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
    }

    public void PauseGame()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.interactable = true;
            pauseMenuCanvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;

        // ★ 補足: ポーズ時に最前面のCanvasをさらに手前に持ってくる（確実性向上）
        if (rootCanvas != null)
        {
            rootCanvas.sortingOrder = 9999;
        }

        isPaused = true;
    }

    // --- ボタンが呼び出す機能 ---

    public void RestartStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitleScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    public void ToggleSoundPanel()
    {
        if (soundPanelUI != null)
        {
            soundPanelUI.SetActive(!soundPanelUI.activeSelf);
        }
        else
        {
            Debug.LogWarning("サウンドパネルが設定されていないか、シーン内に存在しません。");
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("ゲーム終了処理が呼び出されました。ビルドされたアプリケーションでのみ終了します。");
#endif
    }
}