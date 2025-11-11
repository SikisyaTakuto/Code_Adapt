using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    //// ★修正点1: 外部からナビゲーターの実体とNavMeshAgentへの参照を設定
    //[Header("ナビゲーターの実体")]
    //public NavMeshAgent navigatorAgent; // ナビゲーターモデルにアタッチされているNavMeshAgent
    //public Transform navigatorModel;    // ナビゲーターモデルのTransform（ナビゲーターを停止させるためなど）

    [Header("UI要素")]
    public GameObject descriptionPanel;
    public Text descriptionText;
    public Image navigatorIcon;

    [Header("ナビゲーション設定")]
    public Transform[] tutorialPoints; // 複数のチュートリアルポイント
    private int currentPointIndex = 0;
    public float stoppingDistance = 1.0f;

    [Header("プレイヤーへのメッセージ")]
    public string initialMessage = "ようこそ！私がチュートリアルをガイドします。";
    public string reachedPointMessage = "次のステップはこちらです！";
    public string tutorialCompleteMessage = "チュートリアルを完了しました！";

    void Start()
    {
        //if (navigatorAgent == null)
        //{
        //    Debug.LogError("Navigator Agentが設定されていません。Inspectorを確認してください。");
        //    enabled = false;
        //    return;
        //}

        // UIを初期状態で非表示にする
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }

        // 初期メッセージを表示し、最初の目標地点へ移動を開始
        ShowDescription(initialMessage);

        if (tutorialPoints.Length > 0)
        {
            // 少し遅延させてから移動を開始する例（初期メッセージを見せるため）
            Invoke("StartNavigation", 3.0f);
        }
    }

    void Update()
    {
        //// ★修正点2: navigatorAgent（実体のNavMeshAgent）の状態を監視
        //if (navigatorAgent != null && navigatorAgent.isActiveAndEnabled && navigatorAgent.hasPath)
        //{
        //    // 目標地点に十分に近づいたかを判定
        //    if (!navigatorAgent.pathPending && navigatorAgent.remainingDistance <= stoppingDistance)
        //    {
        //        if (!navigatorAgent.isStopped)
        //        {
        //            navigatorAgent.isStopped = true; // 実体のナビゲーターを停止
        //            HandleReachedPoint();
        //        }
        //    }
        //    else
        //    {
        //        // 移動中はUIを非表示にする (ナビゲーターが動き出したとき)
        //        if (descriptionPanel != null && descriptionPanel.activeSelf && !navigatorAgent.isStopped)
        //        {
        //            descriptionPanel.SetActive(false);
        //        }
        //    }
        //}
    }

    void StartNavigation()
    {
        //// ナビゲーターの移動を開始
        //SetNextDestination(tutorialPoints[currentPointIndex]);
        HideDescription(); // 移動開始時はUIを非表示に
    }

    //// 目標地点に到達した際の処理
    //void HandleReachedPoint()
    //{
    //    ShowDescription(reachedPointMessage);

    //    // ★ここでプレイヤーからのアクション待ちを実装することが多い
    //    // 例: プレイヤーがキーを押したら次のポイントへ
    //    // 例: Invoke("GoToNextPoint", 5f); // 5秒後に自動で次のポイントへ
    //}

    //// 次のチュートリアルポイントへ移動する
    //public void GoToNextPoint()
    //{
    //    currentPointIndex++;
    //    if (currentPointIndex < tutorialPoints.Length)
    //    {
    //        SetNextDestination(tutorialPoints[currentPointIndex]);
    //        HideDescription(); // 移動開始時はUIを非表示に
    //    }
    //    else
    //    {
    //        ShowDescription(tutorialCompleteMessage);
    //        navigatorAgent.isStopped = true; // ナビゲーターを完全に停止
    //    }
    //}

    //// 新しい目標地点を設定し、移動を開始
    //void SetNextDestination(Transform newTarget)
    //{
    //    if (navigatorAgent != null && newTarget != null)
    //    {
    //        navigatorAgent.SetDestination(newTarget.position);
    //        navigatorAgent.isStopped = false; // 移動を開始
    //    }
    //}

    // 説明UIを表示する
    void ShowDescription(string message)
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(true);
            if (descriptionText != null)
            {
                descriptionText.text = message;
            }
        }
    }

    // 説明UIを非表示にする
    void HideDescription()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
    }
}