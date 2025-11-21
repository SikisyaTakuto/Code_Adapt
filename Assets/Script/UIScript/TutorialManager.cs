using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// チュートリアルの進行を制御し、プレイヤーの操作制限を管理する司令塔クラス。
/// 修正: ナビゲーションアイコンの表示/非表示制御を追加。オペレーターAIの口調を真面目に修正。
/// 変更: ダッシュチュートリアル、敵出現と撃破を待つ攻撃チュートリアルを追加。
/// 最終変更: ClearSceneへの遷移時カーソル修正、アイコン表示タイミング調整、エネルギー説明を追加。
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

    // 💡 NEW: チュートリアル敵関連
    [Header("チュートリアル敵参照")]
    [Tooltip("出現させる敵（サンドバック）のプレハブ")]
    public GameObject enemyPrefab;
    [Tooltip("敵が出現するTransform（位置と回転）")]
    public Transform enemySpawnPoint;
    private GameObject currentEnemyInstance = null;
    // 敵のControllerクラスが存在すると仮定します (例: TutorialEnemyController)
    // このクラスには 'onDeath' イベントまたは 'IsAlive()' メソッドがあることを想定

    // 🌟 ステップ別時間設定 🌟
    // =======================================================
    [Header("ステップ別時間設定 (Inspectorで個別に設定)")]

    [Tooltip("移動チュートリアルの最小表示時間")]
    public float MinDisplay_Move = 2.0f;
    [Tooltip("移動チュートリアル完了後の待機時間")]
    public float Delay_Move = 1.0f;

    // 💡 NEW: ダッシュ用設定
    [Tooltip("ダッシュチュートリアルの最小表示時間")]
    public float MinDisplay_Dash = 4.0f; // 読み取り時間を確保するため、3.0fから4.0fに増加
    [Tooltip("ダッシュチュートリアル完了後の待機時間")]
    public float Delay_Dash = 1.0f;

    [Tooltip("垂直移動 (浮上・降下) の最小表示時間")]
    public float MinDisplay_Vertical = 3.0f;
    [Tooltip("浮上→降下間の待機時間")]
    public float Delay_Vertical_Mid = 0.5f;
    [Tooltip("垂直移動完了後の待機時間")]
    public float Delay_Vertical_End = 1.5f;

    // 💡 攻撃チュートリアルは敵の撃破を待つため、時間設定は不要。待機時間のみ必要。
    [Tooltip("武器切り替え完了後の待機時間")]
    public float Delay_WeaponSwitch = 1.0f;
    [Tooltip("近接攻撃による敵撃破完了後の待機時間")]
    public float Delay_MeleeAttack_Enemy = 1.5f;
    [Tooltip("ビーム攻撃による敵撃破完了後の待機時間")]
    public float Delay_BeamAttack_Enemy = 1.5f;

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
    private bool isEnemyDestroyed = false; // 敵の撃破フラグ

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
        if (enemyPrefab == null || enemySpawnPoint == null)
        {
            Debug.LogWarning("チュートリアル敵用のPrefabまたはSpawnPointが設定されていません。攻撃チュートリアルがスキップされる可能性があります。");
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
        player.allowDash = false; // 💡 NEW: ダッシュを初期ロック
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

        // 💡 NEW: ステップ 3: ダッシュのチュートリアル
        yield return StartCoroutine(RunDashTutorial(MinDisplay_Dash, Delay_Dash));

        // 💡 NEW: ステップ 3.5: エネルギー説明のチュートリアル (追加)
        yield return StartCoroutine(RunEnergyExplanationTutorial(4.0f, Delay_Dash));

        // ステップ 4: 武器切り替えのチュートリアル
        yield return StartCoroutine(RunWeaponSwitchTutorial(Delay_WeaponSwitch)); // minTimeを削除

        // 💡 NEW: ステップ 5: 近接攻撃のチュートリアル (敵を出現させて撃破を待つ)
        yield return StartCoroutine(RunAttackEnemyTutorial(
            TutorialPlayerController.WeaponMode.Melee,
            "近接モードへの設定を完了しました。眼前の**訓練用サンドバック**を**[左クリック]**で撃破してください。",
            Delay_MeleeAttack_Enemy));

        // 💡 NEW: ステップ 6: ビーム攻撃のチュートリアル (敵を出現させて撃破を待つ)
        yield return StartCoroutine(RunAttackEnemyTutorial(
            TutorialPlayerController.WeaponMode.Beam,
            "ビームモードへの設定を完了しました。眼前の**訓練用サンドバック**を**[左クリック]**で撃破してください。",
            Delay_BeamAttack_Enemy));

        // ステップ 7: アーマー切り替えのチュートリアル
        yield return StartCoroutine(RunArmorSwitchTutorial(MinDisplay_ArmorSwitch, Delay_ArmorSwitch_Mid, Delay_ArmorSwitch_End));

        // チュートリアル終了
        yield return StartCoroutine(EndTutorial());
    }

    // --- ステップごとのコルーチン ---

    private IEnumerator RunMovementTutorial(float minTime, float nextStepDelay)
    {
        // 準備: 移動、浮上/降下、ダッシュ**以外**を許可
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.allowVerticalMove = true; // チュートリアルを通して移動、上昇、下降は許可
        player.allowDash = false;
        player.ResetInputTracking();

        // 指示 (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("現在よりチュートリアルを開始します。まずは基本移動から開始してください。**[WASD]キー**にて自由に移動操作を実行してください。",
                                   () => player.HasMovedHorizontally,
                                   minTime,
                                   nextStepDelay));

        // 💡 NEW: 移動チュートリアル完了後も移動と垂直移動は許可したままにする
        // player.allowHorizontalMove = false; // 無効化しない
        Debug.Log("水平移動チュートリアル完了。");
    }

    private IEnumerator RunVerticalMovementTutorial(float minTime, float midStepDelay, float nextStepDelay)
    {
        // 準備: 既に allowVerticalMove は true のはずだが、念のため。
        player.allowVerticalMove = true;
        player.ResetInputTracking();

        // 指示 (浮上) (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("次に垂直移動の操作です。エネルギーを充填し、浮上してください。**[Space]キー**を長押ししてください。",
                                   () => player.HasJumped,
                                   minTime,
                                   midStepDelay)); // 浮上→降下間の待機

        player.ResetInputTracking(); // 降下操作をトラックするためリセット

        // 指示 (降下) (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction("次は降下操作です。**[Alt]キー**を長押ししてください。",
                                   () => player.HasDescended,
                                   minTime,
                                   nextStepDelay)); // ステップ終了時の待機

        // player.allowVerticalMove = false; // 無効化しない
        Debug.Log("垂直移動チュートリアル完了。");
    }

    // 💡 NEW: ダッシュのチュートリアル
    private IEnumerator RunDashTutorial(float minTime, float nextStepDelay)
    {
        // 準備: ダッシュのみを許可し、トラックをリセット
        player.allowDash = true;
        player.ResetInputTracking();

        // 指示 (ダッシュの説明と実行)
        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "緊急回避、および高速移動に使用するダッシュ操作です。**[Left Shift]キー**を押しながら**[WASD]キー**で移動し、ダッシュを実行してください。",
            () => player.HasDashed, // player.HasDashedというプロパティがPlayerControllerにあると仮定
            minTime,
            nextStepDelay));

        // player.allowDash = false; // 無効化しない
        Debug.Log("ダッシュチュートリアル完了。");
    }

    /// <summary>
    /// 💡 NEW: エネルギー消費に関する説明のコルーチン
    /// </summary>
    private IEnumerator RunEnergyExplanationTutorial(float minTime, float nextStepDelay)
    {
        // 移動系は既に許可されている

        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "重要事項の伝達です。現在実行したダッシュ、および今後の攻撃、浮上操作にはエネルギーを消費します。画面上のエネルギーゲージを常に確認し、残量に注意してください。",
            () => true, // 読み上げたら即座に次のステップへ (最小時間待機は適用)
            minTime,
            nextStepDelay));

        Debug.Log("エネルギーに関する説明を完了。");
    }


    private IEnumerator RunWeaponSwitchTutorial(float nextStepDelay) // minTimeを削除
    {
        // 準備: 武器切り替えのみ許可 (他は移動系のみ許可)
        player.allowWeaponSwitch = true;
        player.allowAttack = false;

        TutorialPlayerController.WeaponMode initialMode = player.currentWeaponMode;

        // 指示 (真面目な口調に修正)
        yield return StartCoroutine(ShowMessageAndWaitForAction($"武器の切り替え操作です。現在のモードは**[{initialMode}]**です。**[E]キー**にてモードを切り替えてください。",
                                   () => player.currentWeaponMode != initialMode,
                                   2.0f, // minTimeを固定値で設定
                                   nextStepDelay));

        player.allowWeaponSwitch = false;
        Debug.Log("武器切り替えチュートリアル完了。");
    }

    // 💡 NEW: 敵出現＆攻撃＆撃破待ちの汎用コルーチン
    private IEnumerator RunAttackEnemyTutorial(TutorialPlayerController.WeaponMode requiredMode, string message, float nextStepDelay)
    {
        if (enemyPrefab == null || enemySpawnPoint == null)
        {
            Debug.LogWarning($"敵のPrefabまたはSpawnPointが設定されていないため、{requiredMode}攻撃チュートリアルをスキップします。");
            yield break;
        }

        // 準備: 攻撃のみ許可 (移動系は既に許可)
        player.allowAttack = true;
        player.allowWeaponSwitch = false; // 誤操作防止
        isEnemyDestroyed = false; // 敵撃破フラグをリセット

        // 武器を必須モードに強制設定
        player.SwitchWeaponMode(requiredMode);

        // 敵の出現とイベント登録
        SpawnEnemy();

        // 敵コントローラーを取得し、死亡イベントに登録 (TutorialEnemyControllerが存在すると仮定)
        // ⚠️ 要修正: 実際の TutorialEnemyController クラス名に合わせてください
        // (ここでは仮の TutorialEnemyController クラスが存在すると仮定して処理を継続)
        Component enemyController = currentEnemyInstance.GetComponent(typeof(TutorialEnemyController));
        // if (enemyController != null)
        // {
        //     ((TutorialEnemyController)enemyController).onDeath += OnEnemyDestroyed;
        // }

        // 指示を出し、敵が破壊されるまで待機
        yield return StartCoroutine(ShowMessageAndWaitForAction(message,
                                   () => isEnemyDestroyed,
                                   3.0f, // 最小表示時間
                                   nextStepDelay));

        // イベント登録を解除
        // if (enemyController != null)
        // {
        //     ((TutorialEnemyController)enemyController).onDeath -= OnEnemyDestroyed;
        // }

        player.allowAttack = false;
        Debug.Log($"{requiredMode}攻撃チュートリアル完了。敵ターゲットを撃破。");
    }

    // 💡 NEW: 敵の出現処理
    private void SpawnEnemy()
    {
        // 既存の敵がいれば削除
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance);
            currentEnemyInstance = null;
        }

        // 新しい敵を生成
        currentEnemyInstance = Instantiate(enemyPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);

        // 敵が出現したことをログに記録
        Debug.Log("訓練用サンドバックを出現させました。");
    }

    // 💡 NEW: 敵の撃破時のコールバック
    private void OnEnemyDestroyed()
    {
        isEnemyDestroyed = true;
        Debug.Log("サンドバックの撃破を確認。次のステップへ移行します。");
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
                                   0.0f));     // 終了後は待機しない

        // 全ての機能を解放する処理
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.allowVerticalMove = true;
        player.allowDash = true; // 💡 NEW: ダッシュも解放
        player.allowWeaponSwitch = true;
        player.allowArmorSwitch = true;
        player.allowAttack = true;

        if (messagePanel != null) messagePanel.SetActive(false);
        SetNavIconVisible(false); // チュートリアル終了時にアイコンを非表示にする
        Debug.Log("--- チュートリアル終了: 全機能解放 ---");
        isTutorialRunning = false;

        // ⭐ 修正点: シーン遷移前にマウスカーソルを再表示し、ロックを解除する
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("マウスカーソル表示を解放しました。");

        // 🚀 ClearSceneへの遷移処理を実行
        Debug.Log("シーンを「ClearScene」へ遷移します...");
        SceneManager.LoadScene("ClearScene");
    }

    // =======================================================
    // ユーティリティメソッド (ナビゲーションアイコン表示タイミングを修正)
    // =======================================================

    // ... (ShowMessageAndWaitForAction メソッドは変更なし)
    private IEnumerator ShowMessageAndWaitForAction(string message, System.Func<bool> condition)
    {
        // ここに来た場合は、デフォルト値に近い設定を使用する
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
        string logMessage = message.Contains("：") ? message.Split('：')[1] : message;
        Debug.Log($"アクション「{logMessage.Split('！')[0].Split('。')[0]}」を**確認しました。**");

        // 最小表示時間が経過するまで待機
        float elapsedTime = Time.time - startTime;
        float remainingTime = minimumTime - elapsedTime;

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 待機中のフラグをリセット
        isWaitingForPlayerAction = false;

        // 次のステップの準備のために待機 (ステップ間の個別待機時間)
        if (nextStepDelay > 0)
        {
            yield return new WaitForSeconds(nextStepDelay);
        }

        // ⭐ 修正点: アイコンの非表示を nextStepDelay の後 (次のメッセージが表示される直前) に移動
        SetNavIconVisible(false);
    }
}