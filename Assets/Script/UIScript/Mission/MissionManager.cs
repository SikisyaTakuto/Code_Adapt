using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Listを使用するために必要

public class MissionManager : MonoBehaviour
{
    // ★Singletonパターン (他のスクリプトから簡単にアクセスできるようにする)★
    public static MissionManager Instance { get; private set; }

    // === UIへの参照 ===
    [Header("UI Components")]
    [SerializeField] private Text missionText;
    [SerializeField] private Image checkmarkImage;

    // === ミッション管理変数 ===
    [Header("Mission List")]
    // 複数のミッションを格納するリスト
    [SerializeField] private List<Mission> missionList = new List<Mission>();

    private int currentMissionIndex = -1; // 現在のミッションがリストの何番目か
    private bool isMissionActive = false; // 現在ミッションが進行中か

    private void Awake()
    {
        // Singletonの初期化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // === 初期設定 ===
    void Start()
    {
        // チェックマークは最初は非表示にしておく
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(false);
        }

        // 最初のミッションを開始
        StartNextMission();

        // テスト用のUpdate()は削除しました。必要に応じて追加してください。
    }

    // --- 次のミッションを開始 ---
    public void StartNextMission()
    {
        // リストの次のミッションのインデックスを計算
        int nextIndex = currentMissionIndex + 1;

        if (nextIndex < missionList.Count)
        {
            // 次のミッションが存在する場合
            currentMissionIndex = nextIndex;
            Mission currentMission = missionList[currentMissionIndex];

            isMissionActive = true;

            // UI更新
            if (missionText != null)
            {
                missionText.text = "【現在のミッション】\n" + currentMission.title;
            }
            if (checkmarkImage != null)
            {
                checkmarkImage.gameObject.SetActive(false); // チェックマークは非表示に
            }

            Debug.Log("ミッション開始: " + currentMission.title);
        }
        else
        {
            // 全てのミッションが完了した場合
            isMissionActive = false;
            if (missionText != null)
            {
                missionText.text = "【全てのミッションを完了しました！】";
            }
            Debug.Log("全てのミッションを完了しました。");
        }
    }


    // --- 現在のミッションを完了 ---
    // 外部のスクリプトから、条件達成時にこのメソッドを呼び出します。
    public void CompleteCurrentMission()
    {
        if (isMissionActive && currentMissionIndex >= 0)
        {
            // 現在のミッションを完了状態にする
            missionList[currentMissionIndex].isCompleted = true;
            isMissionActive = false;

            // 完了時のUI表示に切り替える
            if (missionText != null)
            {
                missionText.text = "【ミッション完了！】\n" + missionList[currentMissionIndex].title + " を達成！";
            }
            if (checkmarkImage != null)
            {
                checkmarkImage.gameObject.SetActive(true);
            }

            Debug.Log("ミッション完了: " + missionList[currentMissionIndex].title);

            // ★遅延させて次のミッションを開始（例として2秒後に開始）★
            Invoke(nameof(StartNextMission), 2.0f);
        }
    }
}