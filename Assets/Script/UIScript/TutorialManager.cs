using UnityEngine;
using UnityEngine.UI; // Text, Canvas, Slider を使うため
using System.Collections;
<<<<<<< HEAD
using UnityEngine.SceneManagement; // SceneManagerを使用するために追加
=======
>>>>>>> New

public class TutorialManager : MonoBehaviour
{
    public PlayerController playerController;
<<<<<<< HEAD
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
=======
    public Text tutorialText; // TextMeshProUGUI に変更
    public GameObject enemyPrefab; // 敵のPrefab
    public Transform enemySpawnPoint; // 敵の出現位置
    public GameObject armorModeEnemyPrefab; // アーマーモード切り替え時に出現させる敵のPrefab
    public Transform armorModeEnemySpawnPoint; // アーマーモード切り替え時に出現させる敵の出現位置

    private GameObject currentEnemyInstance; // 現在出現している敵のインスタンス
>>>>>>> New

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
        SpecialAttack,
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
<<<<<<< HEAD
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

=======
>>>>>>> New
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

<<<<<<< HEAD
        // イベント購読
        if (playerController != null)
        {
            playerController.onEnergyDepleted += HandleEnergyDepletionTutorial;
        }
        if (playerHealth != null)
        {
            playerHealth.onHealthDamaged += HandleHPDamageTutorial;
        }

=======

        // チュートリアル開始
>>>>>>> New
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
<<<<<<< HEAD
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
=======
        // プレイヤーの入力を一時的に無効化
        playerController.canReceiveInput = false;

        // ステップ1: ようこそ
        yield return StartCoroutine(ShowMessage("ようこそ、新米パイロット！\nこの世界で生き抜くための基本を学びましょう。", 3.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // 短い間隔

        // ステップ2: WASD移動
        currentStep = TutorialStep.MoveWASD;
        yield return StartCoroutine(ShowMessage("まずは基本操作です。\nWASDキーを使って、5秒間移動してください。", 0)); // 0は自動消去なし
        playerController.canReceiveInput = true; // WASD入力受付開始
        playerController.ResetInputTracking();

        float moveStartTime = Time.time;
        bool wasdCompleted = false;
        while (Time.time < moveStartTime + 5.0f)
        {
            if (playerController.WASDMoveTimer >= 5.0f) // 連続して5秒間移動したか
            {
                wasdCompleted = true;
                break;
            }
            yield return null;
        }
        playerController.canReceiveInput = false; // 入力受付停止
        if (wasdCompleted)
        {
            yield return StartCoroutine(ShowMessage("素晴らしい！移動操作はバッチリです！", 2.0f));
        }
        else
        {
            yield return StartCoroutine(ShowMessage("WASDキーで移動を続けてください。あと少しです。", 2.0f));
            yield return new WaitForSeconds(1f); // 少し待ってから再試行を促す
            StartCoroutine(TutorialSequence()); // このステップからやり直し
            yield break;
        }

        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ3: スペースキーで飛ぶ
        currentStep = TutorialStep.Jump;
        yield return StartCoroutine(ShowMessage("次に、スペースキーを押して3秒間飛んでみましょう！", 0));
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        float jumpStartTime = Time.time;
        bool jumpCompleted = false;
        while (Time.time < jumpStartTime + 3.0f)
        {
            if (playerController.JumpTimer >= 3.0f)
            {
                jumpCompleted = true;
                break;
            }
            yield return null;
        }
        playerController.canReceiveInput = false;
        if (jumpCompleted)
        {
            yield return StartCoroutine(ShowMessage("上昇成功！空中での移動も重要です。", 2.0f));
        }
        else
        {
            yield return StartCoroutine(ShowMessage("スペースキーを押し続けてください。あと少しで完了です。", 2.0f));
            yield return new WaitForSeconds(1f);
            StartCoroutine(TutorialSequence());
            yield break;
        }
>>>>>>> New
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ4: オルトキーで下がる
        currentStep = TutorialStep.Descend;
<<<<<<< HEAD
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
=======
        yield return StartCoroutine(ShowMessage("今度はAltキーを押して2秒間下降してみましょう。", 0));
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        float descendStartTime = Time.time;
        bool descendCompleted = false;
        while (Time.time < descendStartTime + 2.0f)
        {
            if (playerController.DescendTimer >= 2.0f)
            {
                descendCompleted = true;
                break;
            }
            yield return null;
        }
        playerController.canReceiveInput = false;
        if (descendCompleted)
        {
            yield return StartCoroutine(ShowMessage("下降も完璧です！これで自由自在に飛び回れますね。", 2.0f));
        }
        else
        {
            yield return StartCoroutine(ShowMessage("Altキーを押し続けてください。あと少しで完了です。", 2.0f));
            yield return new WaitForSeconds(1f);
            StartCoroutine(TutorialSequence());
            yield break;
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ5: 中心に戻す＆敵出現
        currentStep = TutorialStep.ResetPosition;
        yield return StartCoroutine(ShowMessage("素晴らしい！では、元の位置に戻って次の訓練に移りましょう。", 3.0f));
        TeleportPlayer(Vector3.zero); // プレイヤーを原点に戻す
        yield return new WaitForSeconds(2.0f); // テレポートの演出時間

        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            yield return StartCoroutine(ShowMessage("目の前に敵が現れました。\n攻撃の準備をしましょう！", 3.0f));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position);
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ6: 左クリックで近接攻撃
        currentStep = TutorialStep.MeleeAttack;
        yield return StartCoroutine(ShowMessage("左クリックで近接攻撃ができます。\n敵を攻撃してみましょう！", 0));
        playerController.canReceiveInput = true;
        bool meleeAttacked = false;
        playerController.onMeleeAttackPerformed += () => { meleeAttacked = true; };
        yield return new WaitUntil(() => meleeAttacked && currentEnemyInstance == null); // 攻撃して敵を倒すまで待つ
        playerController.canReceiveInput = false;
        playerController.onMeleeAttackPerformed -= () => { meleeAttacked = true; }; // イベント購読解除

        yield return StartCoroutine(ShowMessage("見事な近接攻撃でした！", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // 敵を再度出現 (ビーム攻撃用)
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            yield return StartCoroutine(ShowMessage("少し離れた位置に敵が再出現しました。", 2.0f));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 10)); // 少し離れた位置
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ7: 右クリックでビーム攻撃
        currentStep = TutorialStep.BeamAttack;
        yield return StartCoroutine(ShowMessage("少し離れた敵には右クリックでビーム攻撃が有効です。\n敵を倒しましょう！", 0));
        playerController.canReceiveInput = true;
        bool beamAttacked = false;
        playerController.onBeamAttackPerformed += () => { beamAttacked = true; };
        yield return new WaitUntil(() => beamAttacked && currentEnemyInstance == null); // 攻撃して敵を倒すまで待つ
        playerController.canReceiveInput = false;
        playerController.onBeamAttackPerformed -= () => { /*beamAttated = true;*/ }; // イベント購読解除

        yield return StartCoroutine(ShowMessage("ビーム攻撃も完璧です！\nこれで遠くの敵も怖くありません。", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // 敵を再度出現 (特殊攻撃用)
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            yield return StartCoroutine(ShowMessage("複数の敵が現れました。", 2.0f));
            // 複数出現させる場合は、適当な位置に数体配置するロジックを追加
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 5));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(5, 0, 5));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(-5, 0, 5));
        }
>>>>>>> New
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ8: ホイール押込みで特殊攻撃
        currentStep = TutorialStep.SpecialAttack;
<<<<<<< HEAD
        objectiveText.gameObject.SetActive(false);
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            // 敵の出現位置にカメラを向ける (遠くから見下ろすように)
            SetCameraToLookAtPosition(enemySpawnPoint.position + new Vector3(0, 0, 5), enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 5));
            yield return StartCoroutine(ShowMessage("敵が現れました。\nホイールクリックで特殊攻撃を試してみましょう。\n複数の敵に有効です！", 4.0f));
            UpdateObjectiveText("目標: ホイールクリックで特殊攻撃を使い、全ての敵を倒しましょう。"); // 常時目標を更新
        }
        else
        {
            yield return StartCoroutine(ShowMessage("特殊攻撃のチュートリアルを開始します。（敵Prefabが設定されていません）", 3.0f));
            UpdateObjectiveText("目標: 特殊攻撃のチュートリアル（敵Prefabなし）。"); // 常時目標を更新
        }
        // メッセージ表示後にTPSカメラに戻す
        ResetCameraToTPS();

        playerController.canUseSwordBitAttack = true;
        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        playerController.canReceiveInput = false;
        playerController.canUseSwordBitAttack = false;

        objectiveText.gameObject.SetActive(false);
=======
        yield return StartCoroutine(ShowMessage("最後に、ホイールクリックで特殊攻撃を試してみましょう。\n複数の敵に有効です！", 0));
        playerController.canUseSwordBitAttack = true; // 特殊攻撃を有効にする
        playerController.canReceiveInput = true;
        bool bitAttacked = false;
        playerController.onBitAttackPerformed += () => { bitAttacked = true; };
        // 全ての敵が倒れるまで待つ
        yield return new WaitUntil(() => bitAttacked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        playerController.canReceiveInput = false;
        playerController.onBitAttackPerformed -= () => { bitAttacked = true; }; // イベント購読解除
        playerController.canUseSwordBitAttack = false; // 特殊攻撃を無効に戻す

>>>>>>> New
        yield return StartCoroutine(ShowMessage("素晴らしい！特殊攻撃も使いこなせますね！", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // プレイヤーを再度中心に戻す
        currentStep = TutorialStep.ResetPosition;
<<<<<<< HEAD
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("一旦、中央に戻りましょう。", 2.0f));

        // カメラをプレイヤー正面に設定し、移動が完了するまで待つ
        SetCameraToPlayerFront();

        // プレイヤーをテレポートし、同時にカメラをTPSモードに戻す
        TeleportPlayer(Vector3.zero);
        ResetCameraToTPS(); // カメラをTPSモードに戻す

        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // 全ての移動が完了した後の短い待機


        // ステップ9: 1, 2, 3でアーマーモード切り替え
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
=======
        yield return StartCoroutine(ShowMessage("一旦、中央に戻りましょう。", 2.0f));
        TeleportPlayer(Vector3.zero);
        yield return new WaitForSeconds(1.0f);

        // ステップ9: 1, 2, 3でアーマーモード切り替え
        currentStep = TutorialStep.ArmorModeSwitch;
        yield return StartCoroutine(ShowMessage("最後に、1, 2, 3キーでアーマーモードを切り替えることができます。\n好きなモードに切り替えてみましょう！", 0));
        // プレイヤーの近くに敵を出現させて、モード切り替えの理由を与えることも可能
        if (armorModeEnemyPrefab != null && armorModeEnemySpawnPoint != null)
        {
            SpawnEnemy(armorModeEnemyPrefab, armorModeEnemySpawnPoint.position);
            yield return StartCoroutine(ShowMessage("アーマーモードを切り替えて、新しい敵に挑んでみましょう！", 2.0f));
        }

        playerController.canReceiveInput = true;
        bool armorModeChanged = false;
        playerController.onArmorModeChanged += (mode) => {
            Debug.Log($"Armor mode changed to: {mode}");
            armorModeChanged = true;
        };
        yield return new WaitUntil(() => armorModeChanged); // いずれかのモードに切り替わるまで待つ
        playerController.canReceiveInput = false;
        playerController.onArmorModeChanged -= (mode) => { armorModeChanged = true; }; // イベント購読解除

>>>>>>> New
        yield return StartCoroutine(ShowMessage("アーマーモードの切り替え完了！\n状況に合わせてモードを使い分けましょう。", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ10: チュートリアル終了
        currentStep = TutorialStep.End;
<<<<<<< HEAD
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("これで基本訓練は終了です！\n広大な世界へ飛び立ちましょう！", 5.0f));
        objectiveText.gameObject.SetActive(false); // チュートリアル終了時にobjectiveTextを非表示にする場合
        ResetCameraToTPS();
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        if (playerController != null)
        {
            playerController.canReceiveInput = true;
            Debug.Log("チュートリアル終了。プレイヤーの入力を有効にしました。");
        }
        else
        {
            Debug.LogError("PlayerControllerがnullのため、チュートリアル終了時にプレイヤーの入力を有効にできませんでした。");
        }
    }

    /// <summary>
    /// メッセージを表示し、指定秒数待つ。ゲームは一時停止し、プレイヤー入力は無効化される。
    /// このメソッドはtutorialTextを使用し、表示後に非アクティブにする。
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="duration">メッセージ表示時間（秒）。この時間経過後にゲーム再開、入力有効化。</param>
=======
        yield return StartCoroutine(ShowMessage("これで基本訓練は終了です！\n広大な世界へ飛び立ちましょう！", 5.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // チュートリアル終了後の処理（例: メインゲームシーンへの遷移）
        Debug.Log("チュートリアル終了。");
        // ここにメインゲームシーンをロードする処理などを追加
        // 例: SceneManager.LoadScene("MainGameScene");
    }

    /// <summary>
    /// メッセージを表示し、指定秒数待つ（0秒の場合はプレイヤー入力待ち）
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="duration">表示時間（秒）。0の場合はプレイヤー入力待ち。</param>
    /// <returns></returns>
>>>>>>> New
    IEnumerator ShowMessage(string message, float duration)
    {
        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);
<<<<<<< HEAD
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
=======
        Time.timeScale = 0f; // ゲームを一時停止
        yield return null; // Time.timeScale変更を適用するため1フレーム待つ

        if (duration > 0)
        {
            float unscaledTime = 0f;
            while (unscaledTime < duration)
            {
                unscaledTime += Time.unscaledDeltaTime; // UnscaledDeltaTime を使用してポーズ中に時間を進める
                yield return null;
            }
        }
        else // durationが0の場合、プレイヤーがクリックするまで待つ
        {
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return));
        }
        tutorialText.gameObject.SetActive(false);
        Time.timeScale = 1f; // ゲームを再開
    }

    /// <summary>
    /// 短い間隔でプレイヤーに思考時間を与える
    /// </summary>
    /// <param name="delay">遅延時間（秒）</param>
    /// <returns></returns>
    IEnumerator WaitForPlayerAction(float delay)
    {
        yield return new WaitForSeconds(delay);
>>>>>>> New
    }

    /// <summary>
    /// プレイヤーを特定の位置にテレポートさせる
    /// </summary>
<<<<<<< HEAD
    /// <param name="position">テレポート先の位置</param>
=======
    /// <param name="position"></param>
>>>>>>> New
    void TeleportPlayer(Vector3 position)
    {
        if (playerController != null)
        {
<<<<<<< HEAD
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
=======
            // CharacterControllerを使用しているため、直接positionを設定するのではなく、
            // Disableして位置を変更し、再度Enableする手法を取るか、
            // またはController.Moveを使って非常に短い時間で移動させるなどの工夫が必要。
            // ここでは簡易的に、一旦オフにして位置を設定、すぐにオンに戻す。
            // （ただし、これにより一時的にコリジョンが無視される可能性があるので注意）
            playerController.gameObject.SetActive(false);
            playerController.transform.position = position;
            playerController.gameObject.SetActive(true);
            Debug.Log($"プレイヤーを {position} にテレポートしました。");
>>>>>>> New
        }
    }

    /// <summary>
<<<<<<< HEAD
    /// 敵を生成する。既存の「Enemy」タグのオブジェクトは全て破棄される。
=======
    /// 敵を生成する。既存の敵がいれば破棄する。
>>>>>>> New
    /// </summary>
    /// <param name="prefab">敵のPrefab</param>
    /// <param name="position">出現位置</param>
    void SpawnEnemy(GameObject prefab, Vector3 position)
    {
<<<<<<< HEAD
        // 注意: この処理は、新しい敵をスポーンする前に、シーン内の全ての「Enemy」タグの付いたオブジェクトを破棄します。
        // これにより、前のチュートリアルステップで倒しきれなかった敵や、以前にスポーンした敵がクリアされます。
=======
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance); // 既存の敵を破棄
            currentEnemyInstance = null;
        }

        // すでに存在している全てのEnemyタグのオブジェクトを破棄
>>>>>>> New
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            Destroy(enemy);
        }

        if (prefab != null)
        {
            currentEnemyInstance = Instantiate(prefab, position, Quaternion.identity);
<<<<<<< HEAD
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
=======
            // EnemyHealthスクリプトがアタッチされていることを確認
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // Rootにない場合は、子オブジェクトから検索
>>>>>>> New
                enemyHealth = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
<<<<<<< HEAD
=======
                // 敵が倒されたときにイベントを購読できるようにする
>>>>>>> New
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
<<<<<<< HEAD
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

        SetCameraToPlayerFront(); // カメラをプレイヤー正面に設定
        objectiveText.gameObject.SetActive(false);
        // ShowMessageはtutorialTextを使用し、メッセージ表示後には非表示にする
        yield return StartCoroutine(ShowMessage("エネルギーがなくなりました！\nブーストや特殊攻撃はエネルギーを消費します。\nエネルギーは時間で回復します。", 4.0f));

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
        ResetCameraToTPS(); // カメラをTPSモードに戻す
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
        SetCameraToPlayerFront(); // カメラをプレイヤー正面に設定
        objectiveText.gameObject.SetActive(false);
        // ShowMessageはtutorialTextを使用し、メッセージ表示後には非表示にする
        ResetCameraToTPS(); // カメラをTPSモードに戻す
        yield return StartCoroutine(ShowMessage("ダメージを受けました！\nHPが0になるとゲームオーバーです。\n敵の攻撃に注意しましょう！", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));
    }
}
=======
        // 敵が倒されたら、その敵オブジェクトの参照をクリア
        if (currentEnemyInstance != null)
        {
            // 複数の敵が出現している場合のために、Destroyされたオブジェクトの参照を確実にクリアする
            // ここではイベントを購読解除し、nullにする
            currentEnemyInstance.GetComponent<EnemyHealth>().onDeath -= HandleEnemyDeath;
            currentEnemyInstance = null; // 単一のcurrentEnemyInstanceしか見ていないので注意
        }
    }
}
>>>>>>> New
