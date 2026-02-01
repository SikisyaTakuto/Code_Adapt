using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("操作対象のボス")]
    public ElsController boss;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 既に発動済みなら何もしない
        if (hasTriggered) return;

        // まずタグをチェックする
        if (other.CompareTag("Player"))
        {
            // Playerだった場合のみログを出し、処理を開始する
            Debug.Log($"<color=green>Playerがエリアに侵入しました: {other.name}</color>");

            if (boss != null)
            {
                ActivateBossBattle();
            }
            else
            {
                Debug.LogError("BossRoomTrigger: ボスがアサインされていません！");
            }
        }
        else
        {
            // Player以外（弾やギミック）が当たった場合のデバッグ用
            // 不要なら削除してOKです
            // Debug.Log($"Player以外が接触: {other.name}");
        }
    }

    // 処理を見やすくするためにメソッド化
    private void ActivateBossBattle()
    {
        boss.isActivated = true;

        if (MissionManager.Instance != null) MissionManager.Instance.CompleteCurrentMission();
        if (TargetManager.Instance != null) TargetManager.Instance.CompleteCurrentObjective();
        if (BGMManager.instance != null) BGMManager.instance.PlayBossBGM();

        hasTriggered = true;
        GetComponent<Collider>().enabled = false;
    }
}