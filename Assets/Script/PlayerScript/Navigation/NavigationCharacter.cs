using UnityEngine;
using System.Collections;
using UnityEngine.UI; // UIコンポーネントを使用

/// <summary>
/// GameStage1におけるナビゲーションキャラクターの挙動を制御する。
/// プレイヤーを一定の距離を保ちながら追尾し、必要に応じてメッセージを表示する。
/// </summary>
public class NavigationCharacter : MonoBehaviour
{
    // --- 外部設定 ---
    [Header("ターゲットと移動設定")]
    [Tooltip("追尾するターゲット (通常はプレイヤー)")]
    public Transform target;

    [Tooltip("追尾を開始する距離 (プレイヤーがこれ以上離れたら移動開始)")]
    public float followDistance = 5.0f;

    [Tooltip("プレイヤーに接近しすぎないように停止する距離")]
    public float stopDistance = 3.0f;

    [Tooltip("ナビゲーションキャラクターの移動速度")]
    public float moveSpeed = 4.0f;

    [Tooltip("スムーズな回転のための速度")]
    public float rotationSpeed = 5.0f;

    // --- メッセージUIとの連携 (必須) ---
    [Header("UI連携")]
    [Tooltip("メッセージ全体を格納するパネルのGameObject")]
    public GameObject messagePanel;

    [Tooltip("メッセージ本文を表示するためのUI Textコンポーネント")]
    public Text messageTextUI;

    [Tooltip("UI上に表示されるナビゲーターのアイコン (Image コンポーネント)")]
    public Image characterIconUI;

    // =======================================================
    // ? NEW: 矢印モデルの参照と設定
    // =======================================================
    [Header("ナビゲーション矢印設定")]
    [Tooltip("方向を示すための3D矢印モデルのTransform")]
    public Transform directionArrow;

    private bool isMoving = false;

    // =======================================================
    // ? NEW: テスト用ナビゲーション変数
    // =======================================================

    // ? テスト用の目標地点（シーン内に「TestWaypoint」というGameObjectを用意）
    private Transform testWaypoint;
    private bool message1Displayed = false;
    private bool message2Displayed = false;
    private bool message3Displayed = false;
    private bool message4Displayed = false; // 矢印の表示/非表示テスト用

    private float startTime;

    // =======================================================
    // 初期化
    // =======================================================

    void Start()
    {
        // ? テスト用ロジック: 開始時間を記録
        startTime = Time.time;

        // ? テスト用ロジック: シーン内の「TestWaypoint」という名前のオブジェクトを検索
        GameObject wpObject = GameObject.Find("TestWaypoint");
        if (wpObject != null)
        {
            testWaypoint = wpObject.transform;
            Debug.Log("テスト用ウェイポイント 'TestWaypoint' を見つけました。");
        }
        else
        {
            Debug.LogWarning("テスト用ウェイポイント 'TestWaypoint' がシーンに見つかりません。テスト3/矢印ナビゲーションは動作しません。");
        }

        // 初期状態としてUIを非表示にしておく
        HideGuidanceMessage();

        // 矢印を初期状態で非表示にしておく
        if (directionArrow != null)
        {
            directionArrow.gameObject.SetActive(false);
        }
    }


    // =======================================================
    // フレーム更新
    // =======================================================

    void Update()
    {
        if (target == null)
        {
            Debug.LogWarning("ターゲットが設定されていません。");
            return;
        }

        // ターゲットとの距離を計算
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // 追尾ロジックの実行
        HandleMovement(distanceToTarget);

        // ターゲットの方を向く (ナビゲーションキャラ本体の回転)
        HandleRotation();

        // ? NEW: ウェイポイントの方を向く (矢印モデルの回転)
        HandleArrowDirection();

        // ? NEW: テスト用メッセージ表示ロジック
        RunTestNavigation();
    }

    /// <summary>
    /// ? NEW: 矢印を指定したウェイポイントの方向へ回転させる。
    /// </summary>
    private void HandleArrowDirection()
    {
        // 矢印オブジェクトとウェイポイントが設定されていることを確認
        if (directionArrow == null || testWaypoint == null)
        {
            return;
        }

        // ウェイポイントの方向ベクトルを計算 (Y軸の高さの差は無視)
        Vector3 targetDirection = testWaypoint.position - directionArrow.position;
        targetDirection.y = 0; // 上下方向の回転を無視

        if (targetDirection != Vector3.zero)
        {
            // ウェイポイントの方向を向くクォータニオンを作成
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // 矢印の向きをスムーズに更新
            directionArrow.rotation = Quaternion.Slerp(
                directionArrow.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime * 2.0f // キャラクター本体よりも少し速く回転させても良い
            );
        }
    }

    /// <summary>
    /// ? NEW: ナビゲーションのテストイベントを実行する。
    /// </summary>
    private void RunTestNavigation()
    {
        // Test 1: ゲーム開始から3秒後にメッセージを表示
        if (!message1Displayed && Time.time >= startTime + 3.0f)
        {
            DisplayGuidanceMessage("オペレーターAIです。聞こえますか？これが最初のナビゲーションメッセージです。");
            message1Displayed = true;
            // 3秒後に自動的にメッセージを消す
            StartCoroutine(AutoHideMessage(3.0f, 1));
            return; // 複数のメッセージが同時に表示されるのを防ぐ
        }

        // Test 4: ウェイポイントが見つかり、ゲーム開始から8秒後に矢印を表示
        if (testWaypoint != null && directionArrow != null && !message4Displayed && Time.time >= startTime + 8.0f)
        {
            directionArrow.gameObject.SetActive(true);
            DisplayGuidanceMessage("目標地点へのナビゲーションを開始します。矢印の示す方向へ進んでください。");
            message4Displayed = true;
            StartCoroutine(AutoHideMessage(4.0f, 4));
            return;
        }


        // Test 2: プレイヤーが 'I' キーを押したときにメッセージを表示/非表示
        if (Input.GetKeyDown(KeyCode.I) && !message2Displayed)
        {
            DisplayGuidanceMessage("緊急テスト！[I]キーを押しましたね。これが2番目のメッセージです。");
            message2Displayed = true;
            return;
        }
        else if (Input.GetKeyDown(KeyCode.I) && message2Displayed)
        {
            HideGuidanceMessage();
            message2Displayed = false;
            return;
        }

        // Test 3: プレイヤーがウェイポイントに近づいたときにメッセージを表示 (矢印を非表示にする)
        if (testWaypoint != null && !message3Displayed)
        {
            if (Vector3.Distance(target.position, testWaypoint.position) < 5.0f)
            {
                DisplayGuidanceMessage("目標地点に到着しました！矢印ナビゲーションを終了します。");
                message3Displayed = true;

                // 矢印を非表示にする
                if (directionArrow != null)
                {
                    directionArrow.gameObject.SetActive(false);
                }
                // HideGuidanceMessage(); // メッセージはそのまま表示し続ける
                return;
            }
        }
    }

    /// <summary>
    /// メッセージを指定時間後に非表示にするコルーチン
    /// </summary>
    private IEnumerator AutoHideMessage(float delay, int messageId)
    {
        yield return new WaitForSeconds(delay);

        // コルーチン実行時に表示されていたメッセージを非表示にする（簡単な判定）
        if (messageId == 1 && message1Displayed)
        {
            HideGuidanceMessage();
        }
        else if (messageId == 4 && message4Displayed)
        {
            HideGuidanceMessage();
        }
    }


    // ----------------------------------------------------------------------------------
    // 既存の移動・回転・UI表示ロジック (変更なし)
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// ターゲットとの距離に基づき移動を処理する。
    /// </summary>
    private void HandleMovement(float distanceToTarget)
    {
        // ターゲットが `followDistance` より遠い場合、または `stopDistance` より近い場合に追尾を制御
        if (distanceToTarget > followDistance)
        {
            // プレイヤーが離れすぎたため追尾を開始
            MoveTowardsTarget();
        }
        else if (distanceToTarget > stopDistance)
        {
            // プレイヤーとの適切な距離を保つように移動
            MoveTowardsTarget();
        }
        else
        {
            // プレイヤーに十分近いので停止
            isMoving = false;
        }
    }

    private void MoveTowardsTarget()
    {
        isMoving = true;
        // ターゲットの方向へ移動
        // Vector3 direction = (target.position - transform.position).normalized; // 現在は使われていない
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// ターゲットの方向へスムーズに回転する。（ナビゲーションキャラ本体）
    /// </summary>
    private void HandleRotation()
    {
        // Y軸方向の回転のみを考慮
        Vector3 targetDirection = target.position - transform.position;
        targetDirection.y = 0; // 上下方向の回転を無視

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // =======================================================
    // 外部連携 (メッセージ表示の例)
    // =======================================================

    /// <summary>
    /// 特定のメッセージを表示する (UI制御クラスへのアクセスを想定)
    /// </summary>
    public void DisplayGuidanceMessage(string message)
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);

            // メッセージ本文をTextコンポーネントに設定
            if (messageTextUI != null)
            {
                messageTextUI.text = message;
                Debug.Log($"[ナビゲーターメッセージ]: {message}");
            }
            else
            {
                Debug.LogWarning("messageTextUIが設定されていません。InspectorでTextコンポーネントを設定してください。");
            }
        }

        // アイコンを表示
        if (characterIconUI != null)
        {
            characterIconUI.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// メッセージを非表示にする
    /// </summary>
    public void HideGuidanceMessage()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // アイコンを非表示
        if (characterIconUI != null)
        {
            characterIconUI.gameObject.SetActive(false);
        }
    }
}