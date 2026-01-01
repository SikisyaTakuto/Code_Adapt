using UnityEngine;
using System.Collections;

public class DebuffPanel : MonoBehaviour
{
    [Header("デバフ設定")]
    public float speedMultiplier = 0.5f;
    public float jumpMultiplier = 0.5f;
    public float duration = 3.0f; // 解除までの時間

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 全パターンのコントローラーを探す
            var p1 = other.GetComponentInParent<BlanceController>() ?? other.transform.root.GetComponentInChildren<BlanceController>();
            var p2 = other.GetComponentInParent<BusterController>() ?? other.transform.root.GetComponentInChildren<BusterController>();
            var p3 = other.GetComponentInParent<SpeedController>() ?? other.transform.root.GetComponentInChildren<SpeedController>();

            // 見つかったものに対してデバフをかける
            if (p1 != null) p1.SetDebuff(speedMultiplier, jumpMultiplier);
            if (p2 != null) p2.SetDebuff(speedMultiplier, jumpMultiplier);
            if (p3 != null) p3.SetDebuff(speedMultiplier, jumpMultiplier);

            Debug.Log("<color=red>デバフ適用！</color>");
            StopAllCoroutines();
            StartCoroutine(ResetAfterDelay(p1, p2, p3));
        }
    }

    private IEnumerator ResetAfterDelay(BlanceController p1, BusterController p2, SpeedController p3)
    {
        yield return new WaitForSeconds(duration);

        if (p1 != null) p1.ResetDebuff();
        if (p2 != null) p2.ResetDebuff();
        if (p3 != null) p3.ResetDebuff();

        Debug.Log("<color=blue>デバフ解除</color>");
    }
    // エリアから出た時の処理は、念のため残しておくか、不要なら削除してもOKです
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<BlanceController>();
            if (player != null)
            {
                player.ResetDebuff();
                StopAllCoroutines(); // エリア外に出たらタイマーも止める
            }
        }
    }
}