using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// チュートリアルの進行を制御し、プレイヤーの操作制限を管理する司令塔クラス。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // ... (外部参照, 設定, 内部状態, Start(), InitializePlayerState(), StartTutorial() は変更なし) ...
    // --- 外部参照 ---
    [Header("コンポーネント参照")]
    [Tooltip("シーン内の TutorialPlayerController をアタッチ")]
    public TutorialPlayerController player;
    [Tooltip("指示メッセージを表示するためのUI Text (または TextMeshProUGUI)")]
    public Text tutorialTextUI;
    [Tooltip("メッセージを格納するパネル（任意）")]
    public GameObject messagePanel;

    // --- 設定 ---
    [Header("設定")]
    [Tooltip("メッセージが表示される最小時間（プレイヤーの入力に関わらず）")]
    public float minDisplayTime = 1.5f;

    // --- 内部状態 ---
    private bool isTutorialRunning = false;
    private bool isWaitingForPlayerAction = false;

    // =======================================================
    // 初期化
    // =======================================================

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("TutorialPlayerControllerが設定されていません。Inspectorで設定してください。");
            // FindObjectOfType<TutorialPlayerController>()で自動取得を試みるのも良い
            return;
        }

        // 初期状態で全ての操作をロックし、チュートリアルを開始
        InitializePlayerState();
        StartTutorial();
    }

    /// <summary>
    /// チュートリアル開始前のプレイヤーの状態を設定する。
    /// </summary>
    private void InitializePlayerState()
    {
        // プレイヤーの全ての入力をロック
        player.isInputLocked = true;
        player.allowHorizontalMove = false;
        player.allowVerticalMove = false;
        player.allowWeaponSwitch = false;
        player.allowArmorSwitch = false;
        player.allowAttack = false;

        // 入力トラッキングをリセット
        player.ResetInputTracking();
    }

    /// <summary>
    /// チュートリアルの進行を開始する。
    /// </summary>
    public void StartTutorial()
    {
        if (!isTutorialRunning)
        {
            isTutorialRunning = true;
            if (messagePanel != null) messagePanel.SetActive(true);

            StartCoroutine(TutorialFlow());
        }
    }

    // =======================================================
    // チュートリアルのメインの流れ (コルーチン)
    // =======================================================

    private IEnumerator TutorialFlow()
    {
        Debug.Log("--- チュートリアル開始 ---");

        // ステップ 1: 移動のチュートリアル
        yield return StartCoroutine(RunMovementTutorial());

        // ステップ 2: 浮遊/降下のチュートリアル
        yield return StartCoroutine(RunVerticalMovementTutorial());

        // ステップ 3: 武器切り替えのチュートリアル
        yield return StartCoroutine(RunWeaponSwitchTutorial());

        // ステップ 4: 近接攻撃のチュートリアル
        yield return StartCoroutine(RunMeleeAttackTutorial());

        // ステップ 5: ビーム攻撃のチュートリアル
        yield return StartCoroutine(RunBeamAttackTutorial());

        // ステップ 6: アーマー切り替えのチュートリアル
        yield return StartCoroutine(RunArmorSwitchTutorial());

        // チュートリアル終了
        yield return StartCoroutine(EndTutorial());
    }

    // --- ステップごとのコルーチン (RunMovementTutorial, RunVerticalMovementTutorial, RunWeaponSwitchTutorial, RunMeleeAttackTutorial, RunBeamAttackTutorial, RunArmorSwitchTutorial は変更なし) ---

    private IEnumerator RunMovementTutorial()
    {
        // 準備: 移動のみ許可
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.ResetInputTracking();

        // 指示
        yield return StartCoroutine(ShowMessageAndWaitForAction("移動操作を学習します。**[WASD]キー**を押して移動してください。",
                                                               () => player.HasMovedHorizontally,
                                                               minDisplayTime));

        player.allowHorizontalMove = false;
        Debug.Log("移動チュートリアル完了。");
    }

    private IEnumerator RunVerticalMovementTutorial()
    {
        // 準備: 垂直移動のみ許可
        player.allowVerticalMove = true;
        player.ResetInputTracking();

        // 指示 (浮上)
        yield return StartCoroutine(ShowMessageAndWaitForAction("エネルギーを使って浮上できます。**[Space]キー**を長押ししてください。",
                                                               () => player.HasJumped,
                                                               minDisplayTime));

        // 指示 (降下)
        yield return StartCoroutine(ShowMessageAndWaitForAction("降下するには**[Alt]キー**を長押ししてください。",
                                                               () => player.HasDescended,
                                                               minDisplayTime));

        player.allowVerticalMove = false;
        Debug.Log("垂直移動チュートリアル完了。");
    }

    private IEnumerator RunWeaponSwitchTutorial()
    {
        // 準備: 武器切り替えのみ許可
        player.allowWeaponSwitch = true;

        TutorialPlayerController.WeaponMode initialMode = player.currentWeaponMode;

        // 指示
        yield return StartCoroutine(ShowMessageAndWaitForAction($"現在の武器は**[{initialMode}]**です。**[E]キー**を押して武器を切り替えてください。",
                                                               () => player.currentWeaponMode != initialMode,
                                                               minDisplayTime));

        player.allowWeaponSwitch = false;
        Debug.Log("武器切り替えチュートリアル完了。");
    }

    private IEnumerator RunMeleeAttackTutorial()
    {
        // 準備: 近接攻撃のみ許可
        player.allowAttack = true;
        player.allowWeaponSwitch = false;

        // 武器を近接モードに強制設定する (TutorialPlayerControllerに public SwitchWeaponMode が必要)
        player.SwitchWeaponMode(TutorialPlayerController.WeaponMode.Melee);

        // 攻撃イベントが発生するまで待機する
        bool attackPerformed = false;
        // アクションをローカル変数に格納し、解除時に確実に同じインスタンスを参照するように修正
        Action handler = () => attackPerformed = true;
        player.onMeleeAttackPerformed += handler;

        // 指示
        yield return StartCoroutine(ShowMessageAndWaitForAction("近接攻撃を試します。**[左クリック]**で攻撃してください。目の前のオブジェクトを攻撃してみましょう。",
                                                               () => attackPerformed,
                                                               minDisplayTime));

        player.onMeleeAttackPerformed -= handler;
        player.allowAttack = false;
        Debug.Log("近接攻撃チュートリアル完了。");
    }

    private IEnumerator RunBeamAttackTutorial()
    {
        // 準備: ビーム攻撃のみ許可
        player.allowAttack = true;

        // 武器をビームモードに強制設定する (TutorialPlayerControllerに public SwitchWeaponMode が必要)
        player.SwitchWeaponMode(TutorialPlayerController.WeaponMode.Beam);

        // 攻撃イベントが発生するまで待機する
        bool attackPerformed = false;
        // アクションをローカル変数に格納し、解除時に確実に同じインスタンスを参照するように修正
        Action handler = () => attackPerformed = true;
        player.onBeamAttackPerformed += handler;

        // 指示
        yield return StartCoroutine(ShowMessageAndWaitForAction("ビーム攻撃を試します。**[左クリック]**で攻撃してください。遠くのターゲットを狙ってみましょう。",
                                                               () => attackPerformed,
                                                               minDisplayTime));

        player.onBeamAttackPerformed -= handler;
        player.allowAttack = false;
        Debug.Log("ビーム攻撃チュートリアル完了。");
    }

    private IEnumerator RunArmorSwitchTutorial()
    {
        // 準備: アーマー切り替えのみ許可
        player.allowArmorSwitch = true;
        TutorialPlayerController.ArmorMode initialMode = player.currentArmorMode;

        // 最初の指示
        yield return StartCoroutine(ShowMessageAndWaitForAction("アーマー切り替えを試します。**[1]キー**でNormalモード、**[2]キー**でBusterモード、**[3]キー**でSpeedモードに切り替えます。",
                                                               () => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3),
                                                               minDisplayTime));

        // 別のモードになったことを確認するまで待機
        yield return StartCoroutine(ShowMessageAndWaitForAction("いずれかのアーマーに切り替えて、その特性を体験してみましょう。",
                                                               () => player.currentArmorMode != initialMode,
                                                               minDisplayTime));

        player.allowArmorSwitch = false;
        Debug.Log("アーマー切り替えチュートリアル完了。");
    }

    // =======================================================
    // チュートリアル終了 (修正箇所)
    // =======================================================

    private IEnumerator EndTutorial()
    {
        // 最終メッセージ
        // ? 修正: 待機条件を「常にtrue」に変更し、minDisplayTime (3.0f) だけ待機するようにする
        yield return StartCoroutine(ShowMessageAndWaitForAction("チュートリアルを終了します。すべての機能が解放されました。本編を始めましょう！",
                                                               () => true, // 常にtrue (即座に条件を満たす)
                                                               3.0f)); // 3秒間メッセージを表示する

        // 全ての機能を解放する処理
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.allowVerticalMove = true;
        player.allowWeaponSwitch = true;
        player.allowArmorSwitch = true;
        player.allowAttack = true;

        if (messagePanel != null) messagePanel.SetActive(false);
        Debug.Log("--- チュートリアル終了: 機能解放 ---");
        isTutorialRunning = false;

        // SceneManager.LoadScene("MainGameScene");
    }

    // =======================================================
    // ユーティリティメソッド (変更なし)
    // =======================================================

    /// <summary>
    /// UIにメッセージを表示し、指定された条件が満たされるまで、または最小表示時間が経過するまで待機する。
    /// </summary>
    private IEnumerator ShowMessageAndWaitForAction(string message, System.Func<bool> condition, float minimumTime)
    {
        isWaitingForPlayerAction = true;

        if (tutorialTextUI != null)
        {
            tutorialTextUI.text = message;
        }

        float startTime = Time.time;

        // プレイヤーの操作を待機
        yield return new WaitUntil(condition);

        // 待機条件が満たされた後にメッセージを更新（任意）
        // if (condition()) tutorialTextUI.text = "完了！次のステップへ進みます...";

        Debug.Log($"アクション実行: 「{message}」が満たされました。");

        // 最小表示時間が経過するまで待機
        float elapsedTime = Time.time - startTime;
        float remainingTime = minimumTime - elapsedTime;

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 待機中のフラグをリセット
        isWaitingForPlayerAction = false;

        // 次のステップの準備のために少し待機（任意）
        yield return new WaitForSeconds(0.5f);
    }
}