using UnityEngine;
using UnityEngine.UI; // Text, Canvas, Slider を使うため
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public PlayerController playerController;
    public Text tutorialText; // TextMeshProUGUI に変更されていることを考慮
    public GameObject enemyPrefab; // 敵のPrefab
    public Transform enemySpawnPoint; // 敵の出現位置
    public GameObject armorModeEnemyPrefab; // アーマーモード切り替え時に出現させる敵のPrefab
    public Transform armorModeEnemySpawnPoint; // アーマーモード切り替え時に出現させる敵の出現位置

    // ★追加: 自動で次のステップに進むまでのデフォルト時間
    public float defaultAutoAdvanceDuration = 3.0f; // プレイヤーの操作を待たずに自動で進む時間

    private GameObject currentEnemyInstance; // 現在出現している敵のインスタンス

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
        // TextからTextMeshProUGUIに変更されている場合、型を合わせる
        if (tutorialText == null)
        {
            Debug.LogError("TutorialManager: TutorialText (TextMeshProUGUI)が割り当てられていません。");
            return;
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

        // チュートリアル開始
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
        // プレイヤーの入力を一時的に無効化
        playerController.canReceiveInput = false;

        // ステップ1: ようこそ
        yield return StartCoroutine(ShowMessage("ようこそ！\nこの世界で生き抜くための基本を学びましょう。", 3.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // 短い間隔

        // ステップ2: WASD移動
        currentStep = TutorialStep.MoveWASD;
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("まずは基本操作です。\nWASDキーを使って、5秒間移動してください。", defaultAutoAdvanceDuration));
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

        // WASD移動が完了しなかった場合の処理を変更
        if (wasdCompleted)
        {
            yield return StartCoroutine(ShowMessage("素晴らしい！移動操作はバッチリです！", 2.0f));
        }
        else
        {
            // ループせずに次のステップに進む
            yield return StartCoroutine(ShowMessage("WASD移動の訓練は終了です。次の操作に進みましょう。", 2.0f));
            // 必要であれば、ここで失敗したことに対する特別な処理やメッセージを追加できます。
        }

        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ3: スペースキーで飛ぶ
        currentStep = TutorialStep.Jump;
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("次に、スペースキーを押して3秒間飛んでみましょう！", defaultAutoAdvanceDuration));
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
            yield return StartCoroutine(ShowMessage("スペースキーでの上昇訓練は終了です。次の操作に進みましょう。", 2.0f));
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ4: オルトキーで下がる
        currentStep = TutorialStep.Descend;
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("今度はAltキーを押して2秒間下降してみましょう。", defaultAutoAdvanceDuration));
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
            yield return StartCoroutine(ShowMessage("Altキーでの下降訓練は終了です。次の操作に進みましょう。", 2.0f));
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ5: 中心に戻す＆敵出現
        currentStep = TutorialStep.ResetPosition; // ResetPositionに戻す
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
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("左クリックで近接攻撃ができます。\n敵を攻撃してみましょう！", defaultAutoAdvanceDuration));
        playerController.canReceiveInput = true;
        bool meleeAttacked = false;
        playerController.onMeleeAttackPerformed += () => { meleeAttacked = true; };
        yield return new WaitUntil(() => meleeAttacked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0); // 攻撃して敵を倒すまで待つ (currentEnemyInstance == null ではなく、タグで確認)
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
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("少し離れた敵には右クリックでビーム攻撃が有効です。\n敵を倒しましょう！", defaultAutoAdvanceDuration));
        playerController.canReceiveInput = true;
        bool beamAttacked = false;
        playerController.onBeamAttackPerformed += () => { beamAttacked = true; };
        yield return new WaitUntil(() => beamAttacked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0); // 攻撃して敵を倒すまで待つ
        playerController.canReceiveInput = false;
        playerController.onBeamAttackPerformed -= () => { beamAttacked = true; }; // イベント購読解除

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
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ8: ホイール押込みで特殊攻撃
        currentStep = TutorialStep.SpecialAttack;
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("最後に、ホイールクリックで特殊攻撃を試してみましょう。\n複数の敵に有効です！", defaultAutoAdvanceDuration));
        playerController.canUseSwordBitAttack = true; // 特殊攻撃を有効にする
        playerController.canReceiveInput = true;
        bool bitAttacked = false;
        playerController.onBitAttackPerformed += () => { bitAttacked = true; };
        // 全ての敵が倒れるまで待つ
        yield return new WaitUntil(() => bitAttacked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        playerController.canReceiveInput = false;
        playerController.onBitAttackPerformed -= () => { bitAttacked = true; }; // イベント購読解除
        playerController.canUseSwordBitAttack = false; // 特殊攻撃を無効に戻す

        yield return StartCoroutine(ShowMessage("素晴らしい！特殊攻撃も使いこなせますね！", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // プレイヤーを再度中心に戻す
        currentStep = TutorialStep.ResetPosition;
        yield return StartCoroutine(ShowMessage("一旦、中央に戻りましょう。", 2.0f));
        TeleportPlayer(Vector3.zero);
        yield return new WaitForSeconds(1.0f);

        // ステップ9: 1, 2, 3でアーマーモード切り替え
        currentStep = TutorialStep.ArmorModeSwitch;
        // ★変更: durationをdefaultAutoAdvanceDurationに変更
        yield return StartCoroutine(ShowMessage("最後に、1, 2, 3キーでアーマーモードを切り替えることができます。\n好きなモードに切り替えてみましょう！", defaultAutoAdvanceDuration));
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

        yield return StartCoroutine(ShowMessage("アーマーモードの切り替え完了！\n状況に合わせてモードを使い分けましょう。", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // ステップ10: チュートリアル終了
        currentStep = TutorialStep.End;
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
    IEnumerator ShowMessage(string message, float duration)
    {
        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);
        Time.timeScale = 0f; // ゲームを一時停止
        yield return null; // Time.timeScale変更を適用するため1フレーム待つ

        // durationが0より大きい場合は指定時間待つ
        // durationが0の場合は、以前はクリック待ちだったが、今回は常に時間で進めるため、このif-elseはdurationが0の場合も時間で待つように機能する。
        // ただし、チュートリアル進行のロジックとして、durationが0で呼ばれることはもうないはず。
        float unscaledTime = 0f;
        while (unscaledTime < duration)
        {
            unscaledTime += Time.unscaledDeltaTime; // UnscaledDeltaTime を使用してポーズ中に時間を進める
            yield return null;
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
    }

    /// <summary>
    /// プレイヤーを特定の位置にテレポートさせる
    /// </summary>
    /// <param name="position"></param>
    void TeleportPlayer(Vector3 position)
    {
        if (playerController != null)
        {
            playerController.gameObject.SetActive(false);
            playerController.transform.position = position;
            playerController.gameObject.SetActive(true);
            Debug.Log($"プレイヤーを {position} にテレポートしました。");
        }
    }

    /// <summary>
    /// 敵を生成する。既存の敵がいれば破棄する。
    /// </summary>
    /// <param name="prefab">敵のPrefab</param>
    /// <param name="position">出現位置</param>
    void SpawnEnemy(GameObject prefab, Vector3 position)
    {
        // すでに存在している全てのEnemyタグのオブジェクトを破棄
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            Destroy(enemy);
        }

        if (prefab != null)
        {
            currentEnemyInstance = Instantiate(prefab, position, Quaternion.identity);
            // EnemyHealthスクリプトがアタッチされていることを確認
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // Rootにない場合は、子オブジェクトから検索
                enemyHealth = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                // 敵が倒されたときにイベントを購読できるようにする
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
        // 敵が倒されたら、その敵オブジェクトの参照をクリア
        if (currentEnemyInstance != null)
        {
            EnemyHealth eh = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (eh == null)
            {
                eh = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }
            if (eh != null)
            {
                eh.onDeath -= HandleEnemyDeath;
            }
            currentEnemyInstance = null;
        }
    }
}
