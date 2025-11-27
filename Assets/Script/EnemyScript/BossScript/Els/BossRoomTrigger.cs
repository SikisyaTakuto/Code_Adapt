using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    // インスペクターから操作対象の ElsController コンポーネントをアタッチ
    public ElsController boss;

    private void OnTriggerEnter(Collider other)
    {
        // 接触したオブジェクトのタグが "Player" であるかを確認
        if (other.CompareTag("Player"))
        {
            // プレイヤーが部屋に入ったので、ElsController の動きを有効化
            boss.isActivated = true;
            Debug.Log("Bossの動きを有効化");

            // 一度起動したら二度と起動しないように、このトリガーコンポーネントを無効化しても良い
            GetComponent<Collider>().enabled = false;
        }
    }
}