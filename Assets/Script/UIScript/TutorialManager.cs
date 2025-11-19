using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// チュートリアルの進行を制御し、プレイヤーの操作制限を管理する司令塔クラス。
/// 修正: ナビゲーションアイコンの表示/非表示制御を追加。オペレーターAIの口調を真面目に修正。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // --- 外部参照 ---
    [Header("コンポーネント参照")]
    [Tooltip("シーン内の TutorialPlayerController をアタッチ")]
    public TutorialPlayerController player;
    [Tooltip("指示メッセージを表示するためのUI Text (または TextMeshProUGUI)")]
    public Text tutorialTextUI;
    [Tooltip("メッセージを格納するパネル（任意）")]
    public GameObject messagePanel;

    // 💡 NEW: ナビゲーションアイコンのGameObject
    [Tooltip("オペレーターAIのアイコンや立ち絵など、表示/非表示を切り替えるGameObject")]
    public GameObject navIconObject;

    // 🌟 ステップ別時間設定 🌟
    // =======================================================
    [Header("ステップ別時間設定 (Inspectorで個別に設定)")]

    [Tooltip("移動チュートリアルの最小表示時間")]
    public float MinDisplay_Move = 2.0f;
    [Tooltip("移動チュートリアル完了後の待機時間")]
    public float Delay_Move = 1.0f;

    [Tooltip("垂直移動 (浮上・降下) の最小表示時間")]
    public float MinDisplay_Vertical = 3.0f;
    [Tooltip("浮上→降下間の待機時間")]
    public float Delay_Vertical_Mid = 0.5f;
    [Tooltip("垂直移動完了後の待機時間")]
    public float Delay_Vertical_End = 1.5f;

    [Tooltip("武器切り替えチュートリアルの最小表示時間")]
    public float MinDisplay_WeaponSwitch = 2.0f;
    [Tooltip("武器切り替え完了後の待機時間")]
    public float Delay_WeaponSwitch = 1.0f;

    [Tooltip("近接攻撃チュートリアルの最小表示時間")]
    public float MinDisplay_MeleeAttack = 2.5f;
    [Tooltip("近接攻撃完了後の待機時間")]
    public float Delay_MeleeAttack = 1.2f;

    [Tooltip("ビーム攻撃チュートリアルの最小表示時間")]
    public float MinDisplay_BeamAttack = 2.5f;
    [Tooltip("ビーム攻撃完了後の待機時間")]
    public float Delay_BeamAttack = 1.2f;

    [Tooltip("アーマー切り替えチュートリアルの最小表示時間")]
    public float MinDisplay_ArmorSwitch = 3.5f;
    [Tooltip("キー説明→操作間の待機時間")]
    public float Delay_ArmorSwitch_Mid = 0.5f;
    [Tooltip("アーマー切り替え完了後の待機時間")]
    public float Delay_ArmorSwitch_End = 1.8f;

    [Tooltip("チュートリアル終了メッセージの表示時間")]
    public float EndMessageDisplayTime = 3.0f;
    // =======================================================

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
            return;
        }

        // 💡 NEW: ナビゲーションアイコンを初期状態で非表示にする
        SetNavIconVisible(false);

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
    /// ナビゲーションアイコン（立ち絵など）の表示/非表示を切り替える。
    /// </summary>
    private void SetNavIconVisible(bool isVisible)
    {
        if (navIconObject != null)
        {
            navIconObject.SetActive(isVisible);
        }
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
        Debug.Log("--- チュートリアル開始 (オペレーターAI起動) ---");

        // ステップ 1: 移動のチュートリアル
        yield return StartCoroutine(RunMovementTutorial(MinDisplay_Move, Delay_Move));

        // ステップ 2: 浮遊/降下のチュートリアル
        yield return StartCoroutine(RunVerticalMovementTutorial(MinDisplay_Vertical, Delay_Vertical_Mid, Delay_Vertical_End));

        // ステップ 3: 武器切り替えのチュートリアル
        yield return StartCoroutine(RunWeaponSwitchTutorial(MinDisplay_WeaponSwitch, Delay_WeaponSwitch));

        // ステップ 4: 近接攻撃のチュートリアル
        yield return StartCoroutine(RunMeleeAttackTutorial(MinDisplay_MeleeAttack, Delay_MeleeAttack));

        // ステップ 5: ビーム攻撃のチュートリアル
        yield return StartCoroutine(RunBeamAttackTutorial(MinDisplay_BeamAttack, Delay_BeamAttack));

        // ステップ 6: アーマー切り替えのチュートリアル
        yield return StartCoroutine(RunArmorSwitchTutorial(MinDisplay_ArmorSwitch, Delay_ArmorSwitch_Mid, Delay_ArmorSwitch_End));

        // チュートリアル終了
        yield return StartCoroutine(EndTutorial());
    }

    // --- ステップごとのコルーチン (メッセージをキャラ口調に変更) ---

    private IEnumerator RunMovementTutorial(float minTime, float nextStepDelay)
    {
        // 準備: 移動のみ許可
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.ResetInputTracking();

        // 指示 (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("現在よりチュートリアルを開始します。まずは基本移動から開始してください。**[WASD]キー**にて自由に移動操作を実行してください。",
                              () => player.HasMovedHorizontally,
                              minTime,
                              nextStepDelay));

        player.allowHorizontalMove = false;
        Debug.Log("移動チュートリアル完了。");
    }

    private IEnumerator RunVerticalMovementTutorial(float minTime, float midStepDelay, float nextStepDelay)
    {
        // 準備: 垂直移動のみ許可
        player.allowVerticalMove = true;
        player.ResetInputTracking();

        // 指示 (浮上) (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("次に垂直移動の操作です。エネルギーを充填し、浮上してください。**[Space]キー**を長押ししてください。",
                              () => player.HasJumped,
                              minTime,
                              midStepDelay)); // 浮上→降下間の待機

        // 指示 (降下) (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("次は降下操作です。**[Alt]キー**を長押ししてください。",
                              () => player.HasDescended,
                              minTime,
                              nextStepDelay)); // ステップ終了時の待機

        player.allowVerticalMove = false;
        Debug.Log("垂直移動チュートリアル完了。");
    }

    private IEnumerator RunWeaponSwitchTutorial(float minTime, float nextStepDelay)
    {
        // 準備: 武器切り替えのみ許可
        player.allowWeaponSwitch = true;

        TutorialPlayerController.WeaponMode initialMode = player.currentWeaponMode;

        // 指示 (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction($"武器の切り替え操作です。現在のモードは**[{initialMode}]**です。**[E]キー**にてモードを切り替えてください。",
                              () => player.currentWeaponMode != initialMode,
                              minTime,
                              nextStepDelay));

        player.allowWeaponSwitch = false;
        Debug.Log("武器切り替えチュートリアル完了。");
    }

    private IEnumerator RunMeleeAttackTutorial(float minTime, float nextStepDelay)
    {
        // 準備: 近接攻撃のみ許可
        player.allowAttack = true;
        player.allowWeaponSwitch = false;

        // 武器を近接モードに強制設定する
        player.SwitchWeaponMode(TutorialPlayerController.WeaponMode.Melee);

        // 攻撃イベントが発生するまで待機する
        bool attackPerformed = false;
        Action handler = () => attackPerformed = true;
        player.onMeleeAttackPerformed += handler;

        // 指示 (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("近接モードへの設定を完了しました。**[左クリック]**で攻撃を実行してください。眼前のターゲットを攻撃目標とします。",
                              () => attackPerformed,
                              minTime,
                              nextStepDelay));

        player.onMeleeAttackPerformed -= handler;
        player.allowAttack = false;
        Debug.Log("近接攻撃チュートリアル完了。");
    }

    private IEnumerator RunBeamAttackTutorial(float minTime, float nextStepDelay)
    {
        // 準備: ビーム攻撃のみ許可
        player.allowAttack = true;

        // 武器をビームモードに強制設定する
        player.SwitchWeaponMode(TutorialPlayerController.WeaponMode.Beam);

        // 攻撃イベントが発生するまで待機する
        bool attackPerformed = false;
        Action handler = () => attackPerformed = true;
        player.onBeamAttackPerformed += handler;

        // 指示 (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("ビームモードへの設定を完了しました。**[左クリック]**で遠距離攻撃を実行してください。遠方のターゲットを攻撃目標とします。",
                              () => attackPerformed,
                              minTime,
                              nextStepDelay));

        player.onBeamAttackPerformed -= handler;
        player.allowAttack = false;
        Debug.Log("ビーム攻撃チュートリアル完了。");
    }

    private IEnumerator RunArmorSwitchTutorial(float minTime, float midStepDelay, float nextStepDelay)
    {
        // 準備: アーマー切り替えのみ許可
        player.allowArmorSwitch = true;
        TutorialPlayerController.ArmorMode initialMode = player.currentArmorMode;

        // 最初の指示 (キー説明) (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("最後に、アーマー切り替えの説明です。**[1]キー**でNormal、**[2]キー**でBuster、**[3]キー**でSpeedモードへの切り替えが可能です。",
                              () => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3),
                              minTime,
                              midStepDelay)); // キー説明→操作間の待機

        // 別のモードになったことを確認するまで待機 (実際に切り替えを促す) (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("いずれかのキーでアーマーモードを切り替え、性能を確認してください。",
                              () => player.currentArmorMode != initialMode,
                              minTime,
                              nextStepDelay)); // ステップ終了時の待機

        player.allowArmorSwitch = false;
        Debug.Log("アーマー切り替えチュートリアル完了。");
    }

    // =======================================================
    // チュートリアル終了
    // =======================================================

    private IEnumerator EndTutorial()
    {
        // 最終メッセージ (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("お疲れ様でした。全てのチュートリアル項目が終了し、全機能が解放されました。実戦へ移行します。",
                              () => true, // 常にtrue (即座に条件を満たす)
                                                            EndMessageDisplayTime, // 最小表示時間
                                                            0.0f));     // 終了後は待機しない

        // 全ての機能を解放する処理
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.allowVerticalMove = true;
        player.allowWeaponSwitch = true;
        player.allowArmorSwitch = true;
        player.allowAttack = true;

        if (messagePanel != null) messagePanel.SetActive(false);
        SetNavIconVisible(false); // チュートリアル終了時にアイコンを非表示にする
        Debug.Log("--- チュートリアル終了: 全機能解放 ---");
        isTutorialRunning = false;

        // SceneManager.LoadScene("MainGameScene");
    }

    // =======================================================
    // ユーティリティメソッド
    // =======================================================

    // 💡 このオーバーロードは、各ステップのコルーチンが引数を渡すようになったため、基本的に未使用になりますが、互換性のために残します。
    private IEnumerator ShowMessageAndWaitForAction(string message, System.Func<bool> condition)
    {
        // ここに来た場合は、デフォルト値に近い設定を使用する（今回は便宜的に EndTutorial の設定を使用）
        yield return StartCoroutine(ShowMessageAndWaitForAction(message, condition, 2.5f, 1.0f));
    }

    /// <summary>
    /// UIにメッセージを表示し、指定された条件が満たされるまで、または指定された時間 (minimumTime) が経過するまで待機し、
    /// 最後に指定された時間 (nextStepDelay) だけ待機する。
    /// </summary>
    private IEnumerator ShowMessageAndWaitForAction(string message, System.Func<bool> condition, float minimumTime, float nextStepDelay)
    {
        isWaitingForPlayerAction = true;

        // メッセージ表示開始時にアイコンを表示
        SetNavIconVisible(true);

        if (tutorialTextUI != null)
        {
            tutorialTextUI.text = message;
        }

        float startTime = Time.time;

        // プレイヤーの操作を待機
        yield return new WaitUntil(condition);

        // ログもナビゲーションキャラ風に少し変更 (真面目な口調に修正)
        // (メッセージが「**オペレーターAI**：...」の形式であることを前提としています)
        string logMessage = message.Contains("：") ? message.Split('：')[1] : message;
        Debug.Log($"アクション「{logMessage.Split('！')[0].Split('。')[0]}」を**確認しました。**");

        // 最小表示時間が経過するまで待機
        float elapsedTime = Time.time - startTime;
        float remainingTime = minimumTime - elapsedTime;

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 待機終了時にアイコンを非表示
        SetNavIconVisible(false);

        // 待機中のフラグをリセット
        isWaitingForPlayerAction = false;

        // 次のステップの準備のために待機 (ステップ間の個別待機時間)
        if (nextStepDelay > 0)
        {
            yield return new WaitForSeconds(nextStepDelay);
        }
    }
}