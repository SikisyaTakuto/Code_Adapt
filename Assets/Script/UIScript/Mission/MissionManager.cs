using UnityEngine;
using UnityEngine.UI; // Text, Imageを使用する場合

public class MissionManager : MonoBehaviour
{
    // === UIへの参照 ===
    // TextMeshProUGUIを使う場合は、public TMPro.TextMeshProUGUI missionText; のように変更
    [Header("UI Components")]
    [SerializeField] private Text missionText; // ミッション内容を表示するTextコンポーネント
    [SerializeField] private Image checkmarkImage; // クリア時に表示するImageコンポーネント

    // === ミッション管理変数 ===
    private bool isMissionActive = false;
    private bool isMissionComplete = false;

    // === 初期設定 ===
    void Start()
    {
        // チェックマークは最初は非表示にしておく
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(false);
        }

        // ミッションを開始
        StartMission("敵を5体倒す");
    }

    // --- ミッション開始 ---
    public void StartMission(string missionDescription)
    {
        isMissionActive = true;
        isMissionComplete = false;

        if (missionText != null)
        {
            missionText.text = "【現在のミッション】\n" + missionDescription;
        }

        // チェックマークは非表示に
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(false);
        }

        Debug.Log("ミッション開始: " + missionDescription);
    }

    // --- ミッション完了 ---
    // 外部のスクリプトから、条件達成時にこのメソッドを呼び出します。
    public void CompleteCurrentMission()
    {
        if (isMissionActive && !isMissionComplete)
        {
            isMissionComplete = true;
            isMissionActive = false;

            // ミッションテキストを「完了」表示に更新
            if (missionText != null)
            {
                missionText.text = "【ミッション完了！】\nよくやった！";
            }

            // チェックマークを表示
            if (checkmarkImage != null)
            {
                checkmarkImage.gameObject.SetActive(true);
            }

            Debug.Log("ミッション完了！");
        }
    }

    // --- テスト用 ---
    // エディタ実行中に 'C' キーを押すとミッションを完了させる
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CompleteCurrentMission();
        }
    }
}