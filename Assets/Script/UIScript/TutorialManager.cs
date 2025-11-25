using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// チュートリアルの進行を制御し、プレイヤーの操作制限を管理する司令塔クラス。
/// 変更点: 視点操作完了条件のためのカメラ回転閾値を設定。
/// 【修正点】RunCameraLookTutorialからマウスカーソル制御を削除し、常時表示を維持します。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // --- 定数設定 ---
    // プレイヤーの操作待ちの最大時間 (1分)
    private const float DEFAULT_MAX_WAIT_TIME = 60.0f;

    // ⭐ NEW: 視点操作チュートリアルに適用する強制スキップ時間 (5秒)
    private const float CAMERA_LOOK_MAX_WAIT_TIME = 5.0f;

    // ⭐ NEW: カメラ操作完了と見なすための回転の合計閾値 (PlayerController側で実装が必要)
    [Header("視点操作設定")]
    [Tooltip("視点操作完了と見なすために必要なマウス回転の合計移動量 (度)")]
    public float CAMERA_LOOK_THRESHOLD = 5.0f;


    // --- 外部参照 ---
    [Header("コンポーネント参照")]
    [Tooltip("シーン内の TutorialPlayerController をアタッチ")]
    public TutorialPlayerController player;
    [Tooltip("指示メッセージを表示するためのUI Text (または TextMeshProUGUI)")]
    public Text tutorialTextUI;
    [Tooltip("メッセージを格納するパネル（任意）")]
    public GameObject messagePanel;

    [Tooltip("オペレーターAIのアイコンや立ち絵など、表示/非表示を切り替えるGameObject")]
    public GameObject navIconObject;

    // --- チュートリアル敵関連 ---
    [Header("チュートリアル敵参照")]
    [Tooltip("出現させる敵（サンドバック）のプレハブ")]
    public GameObject enemyPrefab;
    [Tooltip("敵が出現するTransform（位置と回転）")]
    public Transform enemySpawnPoint;
    private GameObject currentEnemyInstance = null;

    // 🌟 ステップ別時間設定 🌟
    // =======================================================
    [Header("ステップ別時間設定 (Inspectorで個別に設定)")]
    // ... (他の時間設定は省略) ...
    [Tooltip("移動チュートリアルの最小表示時間")]
    public float MinDisplay_Move = 2.0f;
    [Tooltip("移動チュートリアル完了後の待機時間")]
    public float Delay_Move = 1.0f;

    [Tooltip("ダッシュチュートリアルの最小表示時間")]
    public float MinDisplay_Dash = 4.0f;
    [Tooltip("ダッシュチュートリアル完了後の待機時間")]
    public float Delay_Dash = 1.0f;

    [Tooltip("エネルギー説明の最小表示時間 (分割後の各ステップ)")]
    public float MinDisplay_EnergyFragment = 3.0f;

    [Tooltip("垂直移動 (浮上・降下) の最小表示時間")]
    public float MinDisplay_Vertical = 3.0f;
    [Tooltip("浮上→降下間の待機時間")]
    public float Delay_Vertical_Mid = 0.5f;
    [Tooltip("垂直移動完了後の待機時間")]
    public float Delay_Vertical_End = 1.5f;

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

    [Tooltip("カメラ操作の最小表示時間")]
    public float MinDisplay_CameraLook = 3.0f;
    [Tooltip("ロックオン操作の最小表示時間")]
    public float MinDisplay_LockOn = 3.5f;
    [Tooltip("ロックオンチュートリアル完了後の待機時間")]
    public float Delay_LockOn_End = 1.5f;

    [Tooltip("チュートリアル終了メッセージの表示時間")]
    public float EndMessageDisplayTime = 5.0f; // 3.0f から 5.0f に変更
    // =======================================================

    // --- 内部状態 ---
    private bool isTutorialRunning = false;
    private bool isWaitingForPlayerAction = false;
    private bool isEnemyDestroyed = false; // 敵の撃破フラグ

    private bool isCameraLooked = false;
    private bool isTargetLocked = false;

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

        SetNavIconVisible(false);

        InitializePlayerState();
        StartTutorial();

        // ⭐ NEW: カーソルは常に表示し、ロックしない状態を維持 (Startで設定)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SetCameraLooked() { isCameraLooked = true; }
    public void SetTargetLocked() { isTargetLocked = true; }

    private void InitializePlayerState()
    {
        player.isInputLocked = true;
        player.allowHorizontalMove = false;
        player.allowVerticalMove = false;
        player.allowDash = false;
        player.allowWeaponSwitch = false;
        player.allowArmorSwitch = false;
        player.allowAttack = false;

        player.ResetInputTracking();
        // ⭐ 視点操作フラグの初期化（PlayerController側に実装されている前提）
        // player.ResetCameraInputTracking(); 
        isCameraLooked = false;
        isTargetLocked = false;
    }

    private void SetNavIconVisible(bool isVisible)
    {
        if (navIconObject != null)
        {
            navIconObject.SetActive(isVisible);
        }
    }

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

    public IEnumerator TutorialFlow()
    {
        Debug.Log("--- チュートリアル開始 (オペレーターAI起動) ---");

        // ⭐ 1. 視点操作のチュートリアル (最初に移動)
        yield return StartCoroutine(RunCameraLookTutorial(MinDisplay_CameraLook, Delay_Move));

        // 2. 移動のチュートリアル
        yield return StartCoroutine(RunMovementTutorial(MinDisplay_Move, Delay_Move));

        // 3. 浮遊/降下のチュートリアル
        yield return StartCoroutine(RunVerticalMovementTutorial(MinDisplay_Vertical, Delay_Vertical_Mid, Delay_Vertical_End));

        // 4. ダッシュのチュートリアル
        yield return StartCoroutine(RunDashTutorial(MinDisplay_Dash, Delay_Dash));

        // ⭐ 5. エネルギー説明のチュートリアル (3ステップに分割)
        yield return StartCoroutine(RunEnergyExplanation1(MinDisplay_EnergyFragment, Delay_Dash));
        yield return StartCoroutine(RunEnergyExplanation2(MinDisplay_EnergyFragment, Delay_Dash));
        yield return StartCoroutine(RunEnergyExplanation3(MinDisplay_EnergyFragment, Delay_Dash));

        // 6. 武器切り替えのチュートリアル
        yield return StartCoroutine(RunWeaponSwitchTutorial(Delay_WeaponSwitch));

        // ⭐ 7. ロックオン操作のチュートリアル (攻撃前に移動)
        yield return StartCoroutine(RunLockOnTutorial(MinDisplay_LockOn, Delay_LockOn_End));

        // 8. 近接攻撃のチュートリアル (ロックオン後)
        yield return StartCoroutine(RunAttackEnemyTutorial(
      TutorialPlayerController.WeaponMode.Melee,
      "近接モードでロックオン状態です。標的に向かって[左クリック]で攻撃し、撃破してください。",
      Delay_MeleeAttack_Enemy));

        // 9. ビーム攻撃のチュートリアル
        yield return StartCoroutine(RunAttackEnemyTutorial(
      TutorialPlayerController.WeaponMode.Beam,
      "ビームモードに切り替えます。標的に向かって[左クリック]で攻撃し、撃破してください。",
      Delay_BeamAttack_Enemy));

        // 10. アーマー切り替えのチュートリアル
        yield return StartCoroutine(RunArmorSwitchTutorial(MinDisplay_ArmorSwitch, Delay_ArmorSwitch_Mid, Delay_ArmorSwitch_End));

        // チュートリアル終了
        yield return StartCoroutine(EndTutorial());
    }

    // --- ステップごとのコルーチン (全て public に維持) ---

    // ⭐ 視点操作のチュートリアル (完了条件を回転閾値に対応)
    public IEnumerator RunCameraLookTutorial(float minTime, float nextStepDelay)
    {
        // 準備: カメラ操作を許可し、トラッキングフラグをリセット

        // ⭐ 修正: PlayerController側にallowCameraLookフラグがある場合はここで有効化
        // player.allowCameraLook = true; 

        isCameraLooked = false;

        // ⭐ 修正: PlayerController側にResetCameraInputTrackingを実装し、ここで呼び出す
        // player.ResetCameraInputTracking(); 

        // マウスカーソルをロックし、非表示にする
        // 【修正完了】マウスカーソルを常時表示するため、以下の行を削除しました。
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;

        yield return StartCoroutine(ShowMessageAndWaitForAction(
            $"システム起動。まず、視点操作を行います。[マウス]を動かして、周囲を見回してください。",
            () => isCameraLooked, // PlayerController側が、累積回転量が閾値を超えたら SetCameraLooked() を呼ぶ前提
                minTime,
            nextStepDelay,
        // ⭐ 修正: 視点操作の強制スキップ時間を5.0秒に設定
                CAMERA_LOOK_MAX_WAIT_TIME));

        // ⭐ 修正: PlayerController側にallowCameraLookフラグがある場合はここで無効化
        // player.allowCameraLook = false;

        Debug.Log("カメラ操作チュートリアル完了。");
    }

    public IEnumerator RunMovementTutorial(float minTime, float nextStepDelay)
    {
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.allowVerticalMove = true;
        player.allowDash = false;
        player.ResetInputTracking();

        yield return StartCoroutine(ShowMessageAndWaitForAction("基本移動として、[WASD]キーで移動操作を実行してください。",
                               () => player.HasMovedHorizontally,
                               minTime,
                               nextStepDelay,
                               DEFAULT_MAX_WAIT_TIME));
        Debug.Log("水平移動チュートリアル完了。");
    }

    public IEnumerator RunVerticalMovementTutorial(float minTime, float midStepDelay, float nextStepDelay)
    {
        player.allowVerticalMove = true;
        player.ResetInputTracking();

        // 浮上
        yield return StartCoroutine(ShowMessageAndWaitForAction("垂直移動の操作です。[Space]キーを長押しし、浮上してください。",
                       () => player.HasJumped,
                       minTime,
                       midStepDelay,
                       DEFAULT_MAX_WAIT_TIME));

        player.ResetInputTracking();

        // 降下
        yield return StartCoroutine(ShowMessageAndWaitForAction("降下操作です。[Alt]キーを長押ししてください。",
                       () => player.HasDescended,
                       minTime,
                       nextStepDelay,
                       DEFAULT_MAX_WAIT_TIME));
        Debug.Log("垂直移動チュートリアル完了。");
    }

    public IEnumerator RunDashTutorial(float minTime, float nextStepDelay)
    {
        player.allowDash = true;
        player.ResetInputTracking();

        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "[Left+Shift]キーを押しながら[WASD]キーでダッシュを実行してください。",
            () => player.HasDashed,
            minTime,
            nextStepDelay,
            DEFAULT_MAX_WAIT_TIME));

        Debug.Log("ダッシュチュートリアル完了。");
    }

    // ⭐ エネルギー説明の分割ステップ 1/3
    public IEnumerator RunEnergyExplanation1(float minTime, float nextStepDelay)
    {
        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "重要事項です。ダッシュ、攻撃、浮上操作にはエネルギーを消費します。",
            () => true,
            minTime,
            nextStepDelay));
        Debug.Log("エネルギーに関する説明 1/3 完了。");
    }

    // ⭐ エネルギー説明の分割ステップ 2/3
    public IEnumerator RunEnergyExplanation2(float minTime, float nextStepDelay)
    {
        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "エネルギーは、連続的な行動を制限するための重要なリソースです。",
            () => true,
            minTime,
            nextStepDelay));
        Debug.Log("エネルギーに関する説明 2/3 完了。");
    }

    // ⭐ エネルギー説明の分割ステップ 3/3
    public IEnumerator RunEnergyExplanation3(float minTime, float nextStepDelay)
    {
        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "画面上のエネルギーゲージを確認し、残量にご注意ください。",
            () => true,
            minTime,
            nextStepDelay));
        Debug.Log("エネルギーに関する説明 3/3 完了。");
    }


    public IEnumerator RunWeaponSwitchTutorial(float nextStepDelay)
    {
        player.allowWeaponSwitch = true;
        player.allowAttack = false;

        TutorialPlayerController.WeaponMode initialMode = player.currentWeaponMode;

        yield return StartCoroutine(ShowMessageAndWaitForAction($"武器の切り替え操作です。現在のモードは[{initialMode}]です。[E]キーでモードを切り替えてください。",
                               () => player.currentWeaponMode != initialMode,
                               2.0f,
                               nextStepDelay,
                               DEFAULT_MAX_WAIT_TIME));

        player.allowWeaponSwitch = false;
        Debug.Log("武器切り替えチュートリアル完了。");
    }

    // ⭐ ロックオン操作のチュートリアル (攻撃前に移動)
    public IEnumerator RunLockOnTutorial(float minTime, float nextStepDelay)
    {
        // player.allowLockOn = true; 
        isTargetLocked = false;

        // ロックオンの対象が必要なため、敵を出現させる
        SpawnEnemy();

        yield return StartCoroutine(ShowMessageAndWaitForAction(
            "戦闘ターゲットのロックオン操作です。[右クリック]を押して、標的を捕捉してください。",
            () => isTargetLocked || Input.GetMouseButtonDown(1),
            minTime,
            nextStepDelay,
            DEFAULT_MAX_WAIT_TIME));

        // player.allowLockOn = false; 
        Debug.Log("ロックオンチュートリアル完了。");
    }


    public IEnumerator RunAttackEnemyTutorial(TutorialPlayerController.WeaponMode requiredMode, string message, float nextStepDelay)
    {
        if (currentEnemyInstance == null)
        {
            if (enemyPrefab == null || enemySpawnPoint == null)
            {
                Debug.LogWarning($"敵のPrefabまたはSpawnPointが設定されていないため、{requiredMode}攻撃チュートリアルをスキップします。");
                yield break;
            }
            SpawnEnemy();
        }

        player.allowAttack = true;
        player.allowWeaponSwitch = false;
        isEnemyDestroyed = false;

        player.SwitchWeaponMode(requiredMode);

        TutorialEnemyController enemyController = currentEnemyInstance.GetComponent<TutorialEnemyController>();
        if (enemyController != null)
        {
            enemyController.onDeath += OnEnemyDestroyed;
        }
        else
        {
            Debug.LogWarning("敵にTutorialEnemyControllerがアタッチされていません。撃破待機をスキップします。");
        }

        yield return StartCoroutine(ShowMessageAndWaitForAction(message,
                               () => isEnemyDestroyed,
                               3.0f,
                               nextStepDelay,
                               DEFAULT_MAX_WAIT_TIME));

        if (enemyController != null)
        {
            enemyController.onDeath -= OnEnemyDestroyed;
        }

        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance);
            currentEnemyInstance = null;
        }

        player.allowAttack = false;
        Debug.Log($"{requiredMode}攻撃チュートリアル完了。敵ターゲットを撃破。");
    }

    private void SpawnEnemy()
    {
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance);
            currentEnemyInstance = null;
        }
        currentEnemyInstance = Instantiate(enemyPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
        Debug.Log("訓練用サンドバックを出現させました。");
    }

    public void OnEnemyDestroyed()
    {
        isEnemyDestroyed = true;
        Debug.Log("サンドバックの撃破を確認。次のステップへ移行します。");
    }


    public IEnumerator RunArmorSwitchTutorial(float minTime, float midStepDelay, float nextStepDelay)
    {
        player.allowArmorSwitch = true;
        TutorialPlayerController.ArmorMode initialMode = player.currentArmorMode;

        // キー説明
        yield return StartCoroutine(ShowMessageAndWaitForAction("アーマー切り替えの説明です。[1] Normal、[2] Buster、[3] Speedモードへの切り替えが可能です。",
                       () => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3),
                       minTime,
                       midStepDelay,
                       DEFAULT_MAX_WAIT_TIME));

        // 実際に切り替えを促す
        yield return StartCoroutine(ShowMessageAndWaitForAction("任意のキーでアーマーモードを切り替え、性能を確認してください。",
                       () => player.currentArmorMode != initialMode,
                       minTime,
                       nextStepDelay,
                       DEFAULT_MAX_WAIT_TIME));

        player.allowArmorSwitch = false;
        Debug.Log("アーマー切り替えチュートリアル完了。");
    }

    // =======================================================
    // チュートリアル終了
    // =======================================================

    private IEnumerator EndTutorial()
    {
        // 最終メッセージ
        yield return StartCoroutine(ShowMessageAndWaitForAction("お疲れ様でした。全てのチュートリアル項目を終了し、全機能が解放されました。実戦へ移行します。",
                     () => true,
                     EndMessageDisplayTime, // 5.0f の値が使用される
                                                0.0f));

        // 全ての機能を解放する処理
        player.isInputLocked = false;
        player.allowHorizontalMove = true;
        player.allowVerticalMove = true;
        player.allowDash = true;
        player.allowWeaponSwitch = true;
        player.allowArmorSwitch = true;
        player.allowAttack = true;
        // 💡 備考: PlayerControllerの allowCameraLook, allowLockOn も true にする必要があります。

        if (messagePanel != null) messagePanel.SetActive(false);
        SetNavIconVisible(false);
        Debug.Log("--- チュートリアル終了: 全機能解放 ---");
        isTutorialRunning = false;

        // シーン遷移前にマウスカーソルを再表示し、ロックを解除する
        // Start()で既に設定されているが、念のため再度実行して常時表示を保証
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("マウスカーソル表示を解放しました。");

        // ClearSceneへの遷移処理を実行
        Debug.Log("シーンを「ClearScene」へ遷移します...");
        SceneManager.LoadScene("ClearScene");
    }

    // =======================================================
    // ユーティリティメソッド (強制スキップ機能追加)
    // =======================================================

    // デフォルトの maxWaitTime (0f) を使用するオーバーロード
    private IEnumerator ShowMessageAndWaitForAction(string message, System.Func<bool> condition, float minimumTime, float nextStepDelay)
    {
        // maxWaitTime が指定されない場合 (引数4つ) は、強制スキップなし (0f)
        yield return StartCoroutine(ShowMessageAndWaitForAction(message, condition, minimumTime, nextStepDelay, 0f));
    }

    /// <summary>
    /// UIにメッセージを表示し、指定された条件が満たされるまで待機する。
    /// 強制スキップ (maxWaitTime) 機能付き。
    /// </summary>
    private IEnumerator ShowMessageAndWaitForAction(string message, System.Func<bool> condition, float minimumTime, float nextStepDelay, float maxWaitTime)
    {
        isWaitingForPlayerAction = true;
        SetNavIconVisible(true);

        if (tutorialTextUI != null)
        {
            tutorialTextUI.text = message;
        }

        float startTime = Time.time;

        // ⭐ プレイヤー操作を待機 (最大 maxWaitTime まで)
        float currentWaitTime = 0f;
        while (!condition())
        {
            if (maxWaitTime > 0f && currentWaitTime >= maxWaitTime)
            {
                Debug.LogWarning($"プレイヤー操作が {maxWaitTime} 秒を超えたため、強制的に次のステップへ移行します。");
                break;
            }

            yield return null;
            currentWaitTime += Time.deltaTime;
        }

        string logMessage = message.Contains("：") ? message.Split('：')[1] : message;
        Debug.Log($"アクション「{logMessage.Split('！')[0].Split('。')[0]}」を確認しました。");

        // 最小表示時間が経過するまで待機
        float elapsedTime = Time.time - startTime;
        float remainingTime = minimumTime - elapsedTime;

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        isWaitingForPlayerAction = false;

        // ステップ間の待機
        if (nextStepDelay > 0)
        {
            yield return new WaitForSeconds(nextStepDelay);
        }

        SetNavIconVisible(false);
    }
}