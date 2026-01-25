using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("操作対象のボス")]
    public ElsController boss;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 物理的な接触のデバッグ（ログが出ない場合はRigidbodyを確認）
        Debug.Log($"トリガー接触確認: {other.name}");

        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            if (boss != null)
            {
                // ボスを起動
                boss.isActivated = true;
                Debug.Log("<color=red>Bossの動きを有効化しました</color>");

                // --- 2. ここが重要：ミッションを完了させる ---
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.CompleteCurrentMission();
                }

                // 3. ターゲットマネージャー（矢印など）の更新
                if (TargetManager.Instance != null)
                {
                    TargetManager.Instance.CompleteCurrentObjective();
                }

                // 4. BGMの再生
                if (BGMManager.instance != null)
                {
                    BGMManager.instance.PlayBossBGM();
                }

                hasTriggered = true;

                // トリガーのコライダーをオフにして重複を防止
                GetComponent<Collider>().enabled = false;
            }
            else
            {
                Debug.LogError("BossRoomTrigger: ボスがアサインされていません！");
            }
        }
    }
}