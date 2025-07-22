using UnityEngine;
using UnityEngine.UI; // Text, Canvas, Slider を使うため
using System.Collections;
using UnityEngine.SceneManagement; // SceneManagerを使用するために追加

public class TutorialManager : MonoBehaviour
{
    public PlayerController playerController;
    public TPSCameraController tpsCameraController;
    public Text tutorialText; // 一時的な説明用テキスト
    public Text objectiveText; // 常時表示される目標テキスト

    // チュートリアル進行度ゲージ
    [Header("UI Elements")]
    [Tooltip("チュートリアルの進行度を示すスライダー。")]
    public Slider tutorialProgressBar; // チュートリアル進行度を示すUIスライダー

    // HPゲージとPlayerHealthへの参照
    [Tooltip("プレイヤーのHPゲージ（Slider）への参照。")]
    public Slider hpSlider;
    [Tooltip("プレイヤーのPlayerHealthスクリプトへの参照。")]
    public PlayerHealth playerHealth;

    public GameObject enemyPrefab;
    public Transform enemySpawnPoint;
    public GameObject armorModeEnemyPrefab;
    public Transform armorModeEnemySpawnPoint;

    [Header("Tutorial Camera Settings")]
    [Tooltip("プレイヤーの正面からどれくらい離れるか")]
    public float tutorialCameraDistance = 3.0f;
    [Tooltip("プレイヤーの基準点からどれくらい高さにカメラを置くか")]
    public float tutorialCameraHeight = 1.5f;
    [Tooltip("カメラがプレイヤーのどの高さを見るか（プレイヤーの中心からのオフセット）")]
    public float tutorialCameraLookAtOffset = 1.0f;
    [Tooltip("チュートリアルカメラへの切り替え、またはTPSカメラへの復帰のスムーズさ")]
    public float tutorialCameraSmoothTime = 0.1f; // 例えば、より速くするために値を小さくする

    [Header("Enemy Reveal Camera Settings")]
    [Tooltip("敵出現時にカメラが敵からどれくらい離れるか")]
    public float enemyRevealCameraDistance = 15.0f; // 遠くに設定するための新しい変数
    [Tooltip("敵出現時にカメラが敵からどれくらい高さに位置するか")]
    public float enemyRevealCameraHeight = 5.0f; // 高さに設定するための新しい変数

    // チュートリアル説明済みフラグ
    [Header("Tutorial Explanations Flags")]
    private bool hasExplainedEnergyDepletion = false;
    private bool hasExplainedHPDamage = false;

    private GameObject currentEnemyInstance; // 現在出現している敵のインスタンス
    private Image energyFillImage; // エネルギーゲージのFill Image

    private Coroutine energyBlinkCoroutine; // エネルギーゲージ点滅コルーチンの参照

    // チュートリアルステップの定義
    private enum TutorialStep
    {
        Welcome,
        MoveWASD,
        Jump,
        Descend,
        ResetPosition,
        MeleeAttack,
        BeamAttack,
        // SpecialAttack, // 特殊攻撃のステップを削除
        ArmorModeSwitch,
        End
    }

    private TutorialStep currentStep = TutorialStep.Welcome;

    void Start()
    {
        if (playerController == null)
        {
            Debug.LogError("TutorialManager: PlayerControllerが割り当てられていません。");
            return;
        }
        if (tutorialText == null)
        {
            Debug.LogError("TutorialManager: TutorialText (TextMeshProUGUI)が割り当てられていません。");
            return;
        }
        if (objectiveText == null)
        {
            Debug.LogError("TutorialManager: ObjectiveText (TextMeshProProUGUI)が割り当てられていません。");
            return;
        }

        if (tutorialProgressBar == null)
        {
            Debug.LogError("TutorialManager: Tutorial Progress Bar (Slider)が割り当てられていません。");
            return;
        }
        tutorialProgressBar.gameObject.SetActive(false);
        tutorialText.gameObject.SetActive(false); // 初期状態では一時説明テキストは非表示

        // エネルギーゲージのFill Imageを取得
        if (playerController != null && playerController.energySlider != null)
        {
            // SliderのFill Area > Fill のImageコンポーネントを探すのが一般的
            Transform fillArea = playerController.energySlider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    energyFillImage = fill.GetComponent<Image>();
                }
            }
            if (energyFillImage == null)
            {
                Debug.LogWarning("TutorialManager: Energy Slider Fill Imageが見つかりません。エネルギーゲージの点滅が機能しません。Energy SliderのFill Area/FillオブジェクトにImageコンポーネントがあるか確認してください。");
            }
        }
        else
        {
            Debug.LogError("TutorialManager: PlayerControllerまたはEnergy Sliderが割り当てられていないため、エネルギーゲージの点滅を設定できません。");
        }

        // PlayerHealthの参照を取得
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("TutorialManager: PlayerHealthが割り当てられていません。HPダメージのチュートリアルが機能しない可能性があります。");
            }
        }
        if (hpSlider == null)
        {
            Debug.LogWarning("TutorialManager: HP Sliderが割り当てられていません。HPダメージのチュートリアルが機能しない可能性があります。");
        }

        if (tpsCameraController == null)
        {
            tpsCameraController = FindObjectOfType<TPSCameraController>();
            if (tpsCameraController == null)
            {
                Debug.LogError("TutorialManager: TPSCameraControllerが割り当てられていません。シーンにTPSCameraControllerが存在するか確認してください。");
                return;
            }
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning("TutorialManager: Enemy Prefabが割り当てられていません。近接/ビーム攻撃のチュートリアルが機能しない可能性があります。");
        }
        if (enemySpawnPoint == null)
        {
            Debug.LogWarning("TutorialManager: Enemy Spawn Pointが割り当てられていません。近接/ビーム攻撃のチュートリアルが機能しない可能性があります。");
        }
        if (armorModeEnemyPrefab == null)
        {
            Debug.LogWarning("TutorialManager: Armor Mode Enemy Prefabが割り当てられていません。アーマーモード切り替えのチュートリアルが機能しない可能性があります。");
        }
        if (armorModeEnemySpawnPoint == null)
        {
            Debug.LogWarning("TutorialManager: Armor Mode Enemy Spawn Pointが割り当てられていません。アーマーモード切り替えのチュートリアルが機能しない可能性があります。");
        }

        // イベント購読
        if (playerController != null)
        {
            playerController.onEnergyDepleted += HandleEnergyDepletionTutorial;
        }
        if (playerHealth != null)
        {
            playerHealth.onHealthDamaged += HandleHPDamageTutorial;
        }

        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
        playerController.canReceiveInput = false;

        // ステップ1: ようこそ
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("この世界で生き抜くための基本を学びましょう。", 3.0f));
        UpdateObjectiveText("ようこそ！\nチュートリアルを開始します。");


        // ステップ2: WASD移動
        currentStep = TutorialStep.MoveWASD;
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("まずは基本操作です。\nWASDキーを使って、移動してください。", 3.0f));
        UpdateObjectiveText("目標: WASDキーを使って移動してください。"); // 常時目標を更新
        ResetCameraToTPS();
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        yield return StartCoroutine(WaitForWASDMoveCompletion(5.0f));
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("素晴らしい！移動操作はバッチリです！", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // 短いウェイト

        // ステップ3: スペースキーで飛ぶ
        currentStep = TutorialStep.Jump;
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("次に、スペースキーを押して飛んでみましょう！", 3.0f));
        UpdateObjectiveText("目標: スペースキーを押して飛んでみましょう。"); // 常時目標を更新
        ResetCameraToTPS();
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        yield return StartCoroutine(WaitForJumpCompletion(3.0f));
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("上昇成功！空中での移動も重要です。", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ4: オルトキーで下がる
        currentStep = TutorialStep.Descend;
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("今度はAltキーを押して下降してみましょう。", 3.0f));
        UpdateObjectiveText("目標: Altキーを押して下降してみましょう。"); // 常時目標を更新
        ResetCameraToTPS();
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        yield return StartCoroutine(WaitForDescendCompletion(2.0f));
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("下降も完璧です！これで自由自在に飛び回れますね。", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ5: 中心に戻す
        currentStep = TutorialStep.ResetPosition;
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("素晴らしい！では、元の位置に戻って次の訓練に移りましょう。", 3.0f));

        // カメラをプレイヤー正面に設定し、移動が完了するまで待つ
        SetCameraToPlayerFront();

        // プレイヤーをテレポートし、同時にカメラをTPSモードに戻す
        TeleportPlayer(Vector3.zero);
        ResetCameraToTPS(); // カメラをTPSモードに戻す

        yield return StartCoroutine(WaitForPlayerAction(2.0f)); // 全ての移動が完了した後の短い待機


        // ステップ6: 左クリックで近接攻撃
        currentStep = TutorialStep.MeleeAttack;
        objectiveText.gameObject.SetActive(false);
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            // 敵の出現位置にカメラを向ける (遠くから見下ろすように)
            SetCameraToLookAtPosition(enemySpawnPoint.position, enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(enemyPrefab, enemySpawnPoint.position); // 新しい敵をスポーン
            yield return StartCoroutine(ShowMessage("目の前に敵が現れました。\n左クリックで近接攻撃ができます。", 4.0f));
            UpdateObjectiveText("目標: 左クリックで近接攻撃を使い、敵を倒しましょう。"); // 常時目標を更新
        }
        else
        {
            yield return StartCoroutine(ShowMessage("近接攻撃のチュートリアルを開始します。（敵Prefabが設定されていません）", 3.0f));
            UpdateObjectiveText("目標: 近接攻撃のチュートリアル（敵Prefabなし）。"); // 常時目標を更新
        }
        // メッセージ表示後にTPSカメラに戻す
        ResetCameraToTPS();

        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("見事な近接攻撃でした！", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ7: 右クリックでビーム攻撃
        currentStep = TutorialStep.BeamAttack;
        objectiveText.gameObject.SetActive(false);
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            // 敵の出現位置にカメラを向ける (遠くから見下ろすように)
            SetCameraToLookAtPosition(enemySpawnPoint.position + new Vector3(0, 0, 10), enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 10)); // 少し離れた位置に新しい敵をスポーン
            yield return StartCoroutine(ShowMessage("少し離れた位置に敵が再出現しました。\n右クリックでビーム攻撃が有効です。", 4.0f));
            UpdateObjectiveText("目標: 右クリックでビーム攻撃を使い、敵を倒しましょう。"); // 常時目標を更新
        }
        else
        {
            yield return StartCoroutine(ShowMessage("ビーム攻撃のチュートリアルを開始します。（敵Prefabが設定されていません）", 3.0f));
            UpdateObjectiveText("目標: ビーム攻撃のチュートリアル（敵Prefabなし）。"); // 常時目標を更新
        }
        // メッセージ表示後にTPSカメラに戻す
        ResetCameraToTPS();

        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("ビーム攻撃も完璧です！\nこれで遠くの敵も怖くありません。", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ8: プレイヤーを再度中心に戻す (特殊攻撃ステップが削除されたため、ステップ番号を調整)
        currentStep = TutorialStep.ResetPosition; // ResetPositionを再利用
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("一旦、中央に戻りましょう。", 2.0f));

        // カメラをプレイヤー正面に設定し、移動が完了するまで待つ
        SetCameraToPlayerFront();

        // プレイヤーをテレポートし、同時にカメラをTPSモードに戻す
        TeleportPlayer(Vector3.zero);
        ResetCameraToTPS(); // カメラをTPSモードに戻す

        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // 全ての移動が完了した後の短い待機


        // ステップ9: 1, 2, 3でアーマーモード切り替え (特殊攻撃ステップが削除されたため、ステップ番号を調整)
        currentStep = TutorialStep.ArmorModeSwitch;
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("最後に、1, 2, 3キーでアーマーモードを切り替えることができます。\n好きなモードに切り替えてみましょう！", 4.0f));
        UpdateObjectiveText("目標: 1, 2, 3キーでアーマーモードを切り替えてみましょう。"); // 常時目標を更新
        ResetCameraToTPS();
        if (armorModeEnemyPrefab != null && armorModeEnemySpawnPoint != null)
        {
            // 敵の出現位置にカメラを向ける (遠くから見下ろすように)
            SetCameraToLookAtPosition(armorModeEnemySpawnPoint.position, enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(armorModeEnemyPrefab, armorModeEnemySpawnPoint.position);
            objectiveText.gameObject.SetActive(false);
            yield return StartCoroutine(ShowMessage("アーマーモードを切り替えて、新しい敵に挑んでみましょう！", 2.0f));
            UpdateObjectiveText("目標: アーマーモードを切り替えて、敵を倒しましょう。"); // 常時目標を更新
        }
        // メッセージ表示後にTPSカメラに戻す
        ResetCameraToTPS();

        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForArmorModeChangeCompletion());

        if (armorModeEnemyPrefab != null && armorModeEnemySpawnPoint != null)
        {
            yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        }

        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("チュートリアル完了！ClearSceneに移動します。", 4.0f));
        yield return StartCoroutine(WaitForPlayerAction(1.5f));

        // チュートリアル終了後、ClearSceneに移動
        Debug.Log("チュートリアル完了！ClearSceneに移動します。");
        SceneManager.LoadScene("ClearScene");

        // 以下のチュートリアル終了処理は、シーン遷移によって実行されないためコメントアウトまたは削除
        // currentStep = TutorialStep.End;
        // SetCameraToPlayerFront();
        // objectiveText.gameObject.SetActive(false);
        // yield return StartCoroutine(ShowMessage("これで基本訓練は終了です！\n広大な世界へ飛び立ちましょう！", 5.0f));
        // objectiveText.gameObject.SetActive(false); // チュートリアル終了時にobjectiveTextを非表示にする場合
        // ResetCameraToTPS();
        // yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // if (playerController != null)
        // {
        //     playerController.canReceiveInput = true;
        //     Debug.Log("チュートリアル終了。プレイヤーの入力を有効にしました。");
        // }
        // else
        // {
        //     Debug.LogError("PlayerControllerがnullのため、チュートリアル終了時にプレイヤーの入力を有効にできませんでした。");
        // }
    }

    /// <summary>
    /// メッセージを表示し、指定秒数待つ。ゲームは一時停止し、プレイヤー入力は無効化される。
    /// このメソッドはtutorialTextを使用し、表示後に非アクティブにする。
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="duration">メッセージ表示時間（秒）。この時間経過後にゲーム再開、入力有効化。</param>
    IEnumerator ShowMessage(string message, float duration)
    {
        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);
        objectiveText.gameObject.SetActive(false); // tutorialText表示中はobjectiveTextを非表示にする
        tutorialProgressBar.gameObject.SetActive(false); // メッセージ中はゲージを非表示
        Time.timeScale = 0f; // ゲームを一時停止
        playerController.canReceiveInput = false; // 入力を無効化
        yield return null; // Time.timeScale変更を適用するため1フレーム待つ

        float unscaledTime = 0f;
        while (unscaledTime < duration)
        {
            unscaledTime += Time.unscaledDeltaTime; // UnscaledDeltaTime を使用してポーズ中に時間を進める
            yield return null;
        }

        tutorialText.gameObject.SetActive(false); // メッセージ表示後、非表示にする
        Time.timeScale = 1f; // ゲームを再開
        playerController.canReceiveInput = true; // 入力を有効化
        Debug.Log($"ShowMessage: Player input enabled after message. Current canReceiveInput: {playerController.canReceiveInput}", playerController.gameObject);
    }

    /// <summary>
    /// 常時表示される目標テキストを更新する。
    /// </summary>
    /// <param name="objective">表示する目標テキスト</param>
    void UpdateObjectiveText(string objective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = objective;
            objectiveText.gameObject.SetActive(true); // 常に表示を維持
        }
    }

    /// <summary>
    /// 指定秒数待機する（Time.timeScaleの影響を受けない）。
    /// </summary>
    /// <param name="delay">待機時間（秒）</param>
    IEnumerator WaitForPlayerAction(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
    }

    /// <summary>
    /// WASD移動が指定時間完了するまで待機し、ゲージを更新する。
    /// </summary>
    /// <param name="requiredTime">WASD移動が必要な時間（秒）</param>
    IEnumerator WaitForWASDMoveCompletion(float requiredTime)
    {
        tutorialProgressBar.gameObject.SetActive(true); // ゲージを表示
        tutorialProgressBar.value = 0;
        tutorialProgressBar.maxValue = requiredTime;
        Time.timeScale = 1f; // ゲームを再開

        while (playerController.WASDMoveTimer < requiredTime)
        {
            tutorialProgressBar.value = playerController.WASDMoveTimer;
            yield return null;
        }
        tutorialProgressBar.gameObject.SetActive(false); // ゲージを非表示
    }

    /// <summary>
    /// ジャンプが指定時間完了するまで待機し、ゲージを更新する。
    /// </summary>
    /// <param name="requiredTime">ジャンプが必要な時間（秒）</param>
    IEnumerator WaitForJumpCompletion(float requiredTime)
    {
        tutorialProgressBar.gameObject.SetActive(true);
        tutorialProgressBar.value = 0;
        tutorialProgressBar.maxValue = requiredTime;
        Time.timeScale = 1f; // ゲームを再開

        while (playerController.JumpTimer < requiredTime)
        {
            tutorialProgressBar.value = playerController.JumpTimer;
            yield return null;
        }
        tutorialProgressBar.gameObject.SetActive(false);
    }

    /// <summary>
    /// 下降が指定時間完了するまで待機し、ゲージを更新する。
    /// </summary>
    /// <param name="requiredTime">下降が必要な時間（秒）</param>
    IEnumerator WaitForDescendCompletion(float requiredTime)
    {
        tutorialProgressBar.gameObject.SetActive(true);
        tutorialProgressBar.value = 0;
        tutorialProgressBar.maxValue = requiredTime;
        Time.timeScale = 1f; // ゲームを再開

        while (playerController.DescendTimer < requiredTime)
        {
            tutorialProgressBar.value = playerController.DescendTimer;
            yield return null;
        }
        tutorialProgressBar.gameObject.SetActive(false);
    }

    /// <summary>
    /// 敵が全て倒されるまで待機する。
    /// (このコルーチンが呼ばれた時点で currentEnemyInstance に有効な敵が設定されていることを前提とします)
    /// </summary>
    IEnumerator WaitForEnemyDefeatCompletion()
    {
        tutorialProgressBar.gameObject.SetActive(false); // ゲージは使用しない
        Time.timeScale = 1f; // ゲームを再開

        // currentEnemyInstance が null になるまで待機
        // SpawnEnemy で currentEnemyInstance が設定され、HandleEnemyDeath で null になることを利用
        while (currentEnemyInstance != null)
        {
            yield return null;
        }
        Debug.Log("WaitForEnemyDefeatCompletion: Enemy defeated, proceeding.");
    }

    /// <summary>
    /// アーマーモードが変更されるまで待機する。
    /// </summary>
    IEnumerator WaitForArmorModeChangeCompletion()
    {
        tutorialProgressBar.gameObject.SetActive(false); // ゲージは使用しない
        Time.timeScale = 1f; // ゲームを再開

        bool armorModeChanged = false;
        System.Action<int> onArmorModeChangedHandler = (mode) => { armorModeChanged = true; };
        playerController.onArmorModeChanged += onArmorModeChangedHandler;

        yield return new WaitUntil(() => armorModeChanged); // フラグがtrueになるまで待つ

        playerController.onArmorModeChanged -= onArmorModeChangedHandler; // イベント購読を解除
    }

    /// <summary>
    /// カメラをプレイヤーの正面に設定するヘルパーメソッド
    /// </summary>
    void SetCameraToPlayerFront()
    {
        if (playerController == null || tpsCameraController == null) return;

        Vector3 playerPos = playerController.transform.position;
        Vector3 cameraLookAtPoint = playerPos + Vector3.up * tutorialCameraLookAtOffset;
        Vector3 cameraDesiredPos = playerPos + playerController.transform.forward * -tutorialCameraDistance + Vector3.up * tutorialCameraHeight;
        Quaternion cameraDesiredRot = Quaternion.LookRotation(cameraLookAtPoint - cameraDesiredPos);

        tpsCameraController.SetFixedCameraView(cameraDesiredPos, cameraDesiredRot, tutorialCameraSmoothTime);
        Debug.Log("カメラをプレイヤー正面に設定しました。");
    }

    /// <summary>
    /// 特定のターゲット位置にカメラを向けるヘルパーメソッド
    /// </summary>
    /// <param name="targetPosition">カメラが向くターゲットの位置</param>
    /// <param name="distance">ターゲットからの距離</param>
    /// <param name="height">ターゲットからの高さ</param>
    /// <param name="smoothTime">スムーズな移動時間</param>
    void SetCameraToLookAtPosition(Vector3 targetPosition, float distance, float height, float smoothTime)
    {
        if (tpsCameraController == null) return;

        // ターゲットを中心に、後方から見下ろすような位置を計算
        Vector3 cameraDesiredPos = targetPosition + Vector3.back * distance + Vector3.up * height;
        Quaternion cameraDesiredRot = Quaternion.LookRotation(targetPosition - cameraDesiredPos);

        tpsCameraController.SetFixedCameraView(cameraDesiredPos, cameraDesiredRot, smoothTime);
        Debug.Log($"カメラをターゲット {targetPosition} に設定しました。");
    }

    /// <summary>
    /// カメラを通常のTPS追従モードに戻すヘルパーメソッド
    /// </summary>
    void ResetCameraToTPS()
    {
        if (tpsCameraController == null) return;
        tpsCameraController.ResetToTPSView(tutorialCameraSmoothTime);
        Debug.Log("カメラをTPSモードに戻しました。");
    }

    /// <summary>
    /// プレイヤーを特定の位置にテレポートさせる
    /// </summary>
    /// <param name="position">テレポート先の位置</param>
    void TeleportPlayer(Vector3 position)
    {
        if (playerController != null)
        {
            CharacterController charController = playerController.gameObject.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                // テレポート位置を少しY軸方向に上げることで、地面へのめり込みを防ぐ
                playerController.transform.position = position + Vector3.up * 0.1f; // 0.1f は調整可能
                charController.enabled = true;
                Debug.Log($"プレイヤーを {playerController.transform.position} にテレポートしました。");
            }
            else
            {
                playerController.transform.position = position + Vector3.up * 0.1f;
                Debug.LogWarning($"プレイヤーにCharacterControllerが見つかりません。直接位置を設定しました。");
            }
        }
    }

    /// <summary>
    /// 敵を生成する。既存の「Enemy」タグのオブジェクトは全て破棄される。
    /// </summary>
    /// <param name="prefab">敵のPrefab</param>
    /// <param name="position">出現位置</param>
    void SpawnEnemy(GameObject prefab, Vector3 position)
    {
        // 注意: この処理は、新しい敵をスポーンする前に、シーン内の全ての「Enemy」タグの付いたオブジェクトを破棄します。
        // これにより、前のチュートリアルステップで倒しきれなかった敵や、以前にスポーンした敵がクリアされます。
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            Destroy(enemy);
        }

        if (prefab != null)
        {
            currentEnemyInstance = Instantiate(prefab, position, Quaternion.identity);
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.onDeath += HandleEnemyDeath;
                Debug.Log($"敵を {position} に出現させました。");
            }
            else
            {
                Debug.LogWarning($"出現させた敵 '{prefab.name}' に EnemyHealth スクリプトが見つかりません。");
            }
        }
    }

    /// <summary>
    /// 敵が倒されたときに呼び出されるハンドラ
    /// </summary>
    private void HandleEnemyDeath()
    {
        Debug.Log("敵が倒されました！");
        if (currentEnemyInstance != null)
        {
            EnemyHealth eh = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (eh == null)
            {
                eh = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }
            if (eh != null)
            {
                eh.onDeath -= HandleEnemyDeath; // イベント購読解除を忘れずに
            }
            currentEnemyInstance = null; // 敵が倒されたら参照をクリア
        }
    }

    // エネルギー枯渇時のチュートリアルハンドラ
    void HandleEnergyDepletionTutorial()
    {
        if (hasExplainedEnergyDepletion) return; // 一度だけ説明
        hasExplainedEnergyDepletion = true;

        StartCoroutine(EnergyDepletionSequence());
    }

    IEnumerator EnergyDepletionSequence()
    {
        // 他の点滅コルーチンが動いていれば停止
        if (energyBlinkCoroutine != null) StopCoroutine(energyBlinkCoroutine);

        // ShowMessageを呼び出す前に、tutorialProgressBarの現在の表示状態を保存
        bool wasProgressBarActive = tutorialProgressBar.gameObject.activeSelf;

        SetCameraToPlayerFront(); // カメラをプレイヤー正面に設定
        objectiveText.gameObject.SetActive(false); // 一時的に目標テキストを非表示
        // ShowMessageはtutorialTextを使用し、メッセージ表示後には非表示にする
        yield return StartCoroutine(ShowMessage("エネルギーがなくなりました！\nブーストや特殊攻撃はエネルギーを消費します。\nエネルギーは時間で回復します。", 4.0f));

        // メッセージ表示後、現在の目標を再表示
        UpdateObjectiveText(GetCurrentObjectiveString());
        ResetCameraToTPS(); // カメラをTPSモードに戻す

        // ShowMessageを呼び出す前の状態に戻す
        if (wasProgressBarActive)
        {
            tutorialProgressBar.gameObject.SetActive(true);
        }

        // エネルギーゲージの点滅を開始
        if (energyFillImage != null)
        {
            energyBlinkCoroutine = StartCoroutine(EnergyGaugeBlink(energyFillImage, 5.0f, 10.0f)); // 5秒間点滅、点滅速度10
        }

        yield return StartCoroutine(WaitForPlayerAction(2.0f)); // プレイヤーに観察時間を与える

        // 点滅を停止し、元の色に戻す
        if (energyBlinkCoroutine != null)
        {
            StopCoroutine(energyBlinkCoroutine);
            energyBlinkCoroutine = null;
        }
        if (energyFillImage != null)
        {
            energyFillImage.color = new Color(energyFillImage.color.r, energyFillImage.color.g, energyFillImage.color.b, 1f); // アルファ値を1に戻す
        }
    }

    // エネルギーゲージ点滅コルーチン
    IEnumerator EnergyGaugeBlink(Image targetImage, float blinkDuration, float blinkSpeed)
    {
        if (targetImage == null) yield break;

        Color originalColor = targetImage.color;
        float timer = 0f;
        while (timer < blinkDuration)
        {
            // Time.unscaledTime を使用して、Time.timeScaleが0でも点滅するようにする
            float alpha = Mathf.Abs(Mathf.Sin(Time.unscaledTime * blinkSpeed));
            targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        targetImage.color = originalColor; // 点滅終了後、元の色に戻す
    }

    // HPダメージ時のチュートリアルハンドラ
    void HandleHPDamageTutorial()
    {
        if (hasExplainedHPDamage) return; // 一度だけ説明
        hasExplainedHPDamage = true;

        StartCoroutine(HPDamageSequence());
    }

    IEnumerator HPDamageSequence()
    {
        // ShowMessageを呼び出す前に、tutorialProgressBarの現在の表示状態を保存
        bool wasProgressBarActive = tutorialProgressBar.gameObject.activeSelf;

        SetCameraToPlayerFront(); // カメラをプレイヤー正面に設定
        objectiveText.gameObject.SetActive(false); // 一時的に目標テキストを非表示
        // ShowMessageはtutorialTextを使用し、メッセージ表示後には非表示にする
        yield return StartCoroutine(ShowMessage("ダメージを受けました！\nHPが0になるとゲームオーバーです。\n敵の攻撃に注意しましょう！", 2.0f));

        // メッセージ表示後、現在の目標を再表示
        UpdateObjectiveText(GetCurrentObjectiveString());
        ResetCameraToTPS(); // カメラをTPSモードに戻す

        // ShowMessageを呼び出す前の状態に戻す
        if (wasProgressBarActive)
        {
            tutorialProgressBar.gameObject.SetActive(true);
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));
    }

    /// <summary>
    /// 現在のチュートリアルステップに応じた目標テキストを返す。
    /// </summary>
    /// <returns>現在の目標テキスト</returns>
    private string GetCurrentObjectiveString()
    {
        switch (currentStep)
        {
            case TutorialStep.Welcome:
                return "ようこそ！\nチュートリアルを開始します。";
            case TutorialStep.MoveWASD:
                return "目標: WASDキーを使って移動してください。";
            case TutorialStep.Jump:
                return "目標: スペースキーを押して飛んでみましょう。";
            case TutorialStep.Descend:
                return "目標: Altキーを押して下降してみましょう。";
            case TutorialStep.ResetPosition:
                return "目標: 元の位置に戻りましょう。"; // プレイヤーを中央に戻す際の目標
            case TutorialStep.MeleeAttack:
                return "目標: 左クリックで近接攻撃を使い、敵を倒しましょう。";
            case TutorialStep.BeamAttack:
                return "目標: 右クリックでビーム攻撃を使い、敵を倒しましょう。";
            case TutorialStep.ArmorModeSwitch:
                // アーマーモード切り替えと敵討伐が複合しているため、状況に応じて調整
                if (currentEnemyInstance != null)
                {
                    return "目標: アーマーモードを切り替えて、敵を倒しましょう。";
                }
                else
                {
                    return "目標: 1, 2, 3キーでアーマーモードを切り替えてみましょう。";
                }
            case TutorialStep.End:
                return "チュートリアル終了！";
            default:
                return "現在の目標はありません。";
        }
    }
}
