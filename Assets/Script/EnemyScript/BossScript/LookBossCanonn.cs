using UnityEngine;

public class LookBossCanonn : MonoBehaviour
{
    public Transform target;                 // ターゲット
    public float rotationSpeed = 5f;
    public BossEnemyDead bossEnemyDaed;
    public BossShotBullet bossShotBullet;

    // Look制御用
    public float disableLookDuration = 2.0f; // 撃つ前後何秒Lookを止めるか
    private float lookDisableTimer = 0f;     // 残り停止時間

    void Update()
    {
        if (bossEnemyDaed.BossDead) return;

        // Lookの一時停止時間が残っていれば、減らすだけ
        if (lookDisableTimer > 0f)
        {
            lookDisableTimer -= Time.deltaTime;
            return; // このフレームはLookをスキップ
        }

        // プレイヤーの方向を向く処理
        Vector3 direction = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // 弾を撃つ直前・直後に呼び出す
    public void TemporarilyDisableLook()
    {
        lookDisableTimer = disableLookDuration;
    }

    public void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            // 初期向きに戻す
            transform.rotation = Quaternion.identity;
        }
    }
}
