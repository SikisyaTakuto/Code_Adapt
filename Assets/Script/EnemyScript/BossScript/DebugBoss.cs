using UnityEngine;

/// <summary>
/// 特定のキー入力でプレイヤーをボス部屋へ転送し、ボス戦を強制開始するデバッグ用スクリプト
/// </summary>
public class DebugBoss : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("このキーを押すとボス戦を開始します")]
    public KeyCode debugKey = KeyCode.L;

    [Header("参照")]
    public ElsController boss;
    public Transform player;
    [Tooltip("ボス部屋のプレイヤー出現位置（空のGameObjectを作ってアサインしてください）")]
    public Transform bossRoomSpawnPoint;

    void Update()
    {
        // 指定されたキーが押されたかチェック
        if (Input.GetKeyDown(debugKey))
        {
            StartBossFightDebug();
        }
    }

    private void StartBossFightDebug()
    {
        if (boss == null || player == null || bossRoomSpawnPoint == null)
        {
            Debug.LogError("DebugBoss: 必要な参照（Boss, Player, SpawnPoint）が足りません！");
            return;
        }

        Debug.Log("<color=yellow>【Debug】ボス戦を強制開始します</color>");

        // 1. プレイヤーをボス部屋に移動
        // CharacterControllerを使っている場合は一時的に無効化しないと座標更新が反映されないことがあります
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = bossRoomSpawnPoint.position;
        player.rotation = bossRoomSpawnPoint.rotation;

        if (cc != null) cc.enabled = true;

        // 2. ボスを起動
        boss.isActivated = true;

        // 3. ミッション・目的を更新 (BossRoomTriggerの処理を再現)
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CompleteCurrentMission();
        }

        if (TargetManager.Instance != null)
        {
            TargetManager.Instance.CompleteCurrentObjective();
        }

        // 4. BGMの再生
        if (BGMManager.instance != null)
        {
            BGMManager.instance.PlayBossBGM();
        }

        // 5. HPバーを表示（ElsController内のUpdateで自動表示されますが、念のため）
        if (boss.bossHpBar != null)
        {
            boss.bossHpBar.gameObject.SetActive(true);
        }
    }
}