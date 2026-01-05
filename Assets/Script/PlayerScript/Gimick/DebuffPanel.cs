using UnityEngine;
using System.Collections;

public class DebuffPanel : MonoBehaviour
{
    [Header("エフェクト設定")]
    [SerializeField] private GameObject gravityPrefab; // 重力枠のプレハブをセット
    [SerializeField] private float spawnRate = 1.0f;    // 何秒に1回出すか
    [SerializeField] private Vector3 spawnRange = new Vector3(0.0f, 0, 0.0f); // 生成する横幅の広さ

    [SerializeField] private Vector3 spawnRotation = new Vector3(90, 0, 0);

    [Header("デバフ設定")]
    public float speedMultiplier = 0.5f;
    public float jumpMultiplier = 0.5f;
    public float duration = 3.0f;

    private void Start()
    {
        // エフェクト生成ループを開始
        StartCoroutine(SpawnGravityLoop());
    }

    private IEnumerator SpawnGravityLoop()
    {
        while (true)
        {
            SpawnEffect();
            yield return new WaitForSeconds(spawnRate);
        }
    }

    private void SpawnEffect()
    {
        if (gravityPrefab == null) return;

        // パネル上のランダムな位置を計算
        Vector3 randomPos = new Vector3(
            Random.Range(-spawnRange.x, spawnRange.x),
            0,
            Random.Range(-spawnRange.z, spawnRange.z)
        );

        // インスペクターで設定した spawnRotation (Vector3) を Quaternion に変換
        Quaternion rotation = Quaternion.Euler(spawnRotation);

        // 第3引数に算出した rotation を入れる
        GameObject effect = Instantiate(gravityPrefab, transform.position + randomPos, rotation);

        // パネルの子にせず独立させる（パネルが動いてもエフェクトは上に直進するため）
        effect.transform.SetParent(null);
    }

    // --- 以下、既存のデバフ処理 ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var p1 = other.GetComponentInParent<BlanceController>() ?? other.transform.root.GetComponentInChildren<BlanceController>();
            var p2 = other.GetComponentInParent<BusterController>() ?? other.transform.root.GetComponentInChildren<BusterController>();
            var p3 = other.GetComponentInParent<SpeedController>() ?? other.transform.root.GetComponentInChildren<SpeedController>();

            if (p1 != null) p1.SetDebuff(speedMultiplier, jumpMultiplier);
            if (p2 != null) p2.SetDebuff(speedMultiplier, jumpMultiplier);
            if (p3 != null) p3.SetDebuff(speedMultiplier, jumpMultiplier);

            // 既存のStopAllCoroutinesは生成ループも止めてしまうため
            // デバフ解除用コルーチンだけ個別に管理するのが理想的です
            StartCoroutine(ResetAfterDelay(p1, p2, p3));
        }
    }

    private IEnumerator ResetAfterDelay(BlanceController p1, BusterController p2, SpeedController p3)
    {
        yield return new WaitForSeconds(duration);
        if (p1 != null) p1.ResetDebuff();
        if (p2 != null) p2.ResetDebuff();
        if (p3 != null) p3.ResetDebuff();
    }
}