using UnityEngine;

public class VoxRoomTrigger : MonoBehaviour
{
    // インスペクターから操作対象の ElsController コンポーネントをアタッチ
    public VoxController boss;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 接触したオブジェクトのタグが "Player" であるかを確認
        if (other.CompareTag("Player"))
        {
            // プレイヤーが部屋に入ったので、ElsController の動きを有効化
            boss.isActivated = true;
            Debug.Log("Bossの動きを有効化");

            // ターゲットを解除
            TargetManager.Instance.SetTarget(null);

            // 一度起動したら二度と起動しないように、このトリガーコンポーネントを無効化しても良い
            GetComponent<Collider>().enabled = false;


            if (BGMManager.instance != null)
            {
                BGMManager.instance.PlayBossBGM();
                hasTriggered = true; // 1回だけ実行
            }
        }
    }
}