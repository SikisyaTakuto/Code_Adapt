using UnityEngine;
using System.Collections;

public class DebuffPanel : MonoBehaviour
{
    [Header("エフェクト設定")]
    [SerializeField] private GameObject gravityPrefab;
    [SerializeField] private float spawnRate = 1.0f;
    [SerializeField] private Vector3 spawnRange = new Vector3(0.0f, 0, 0.0f);
    [SerializeField] private float spawnHeight = 10.0f;
    [SerializeField] private Vector3 spawnRotation = new Vector3(90, 0, 0);

    [Header("デバフ設定")]
    public float speedMultiplier = 0.5f;
    public float jumpMultiplier = 0.5f;
    // ※ duration（持続時間）は「エリア内にいる間」なので不要になりますが、念のため残しています

    private void Start()
    {
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

        Vector3 spawnPos = new Vector3(
            transform.position.x + Random.Range(-spawnRange.x, spawnRange.x),
            transform.position.y + spawnHeight,
            transform.position.z + Random.Range(-spawnRange.z, spawnRange.z)
        );

        Quaternion rotation = Quaternion.Euler(spawnRotation);
        GameObject effect = Instantiate(gravityPrefab, spawnPos, rotation);
        effect.transform.SetParent(null);
    }

    // --- エリアに入った時：デバフをかける ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyDebuff(other, true);
        }
    }

    // --- エリアから出た時：デバフを解除する ---
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyDebuff(other, false);
        }
    }

    // デバフ適用・解除の共通処理
    private void ApplyDebuff(Collider other, bool isActive)
    {
        var p1 = other.GetComponentInParent<BlanceController>() ?? other.transform.root.GetComponentInChildren<BlanceController>();
        var p2 = other.GetComponentInParent<BusterController>() ?? other.transform.root.GetComponentInChildren<BusterController>();
        var p3 = other.GetComponentInParent<SpeedController>() ?? other.transform.root.GetComponentInChildren<SpeedController>();

        if (isActive)
        {
            if (p1 != null) p1.SetDebuff(speedMultiplier, jumpMultiplier);
            if (p2 != null) p2.SetDebuff(speedMultiplier, jumpMultiplier);
            if (p3 != null) p3.SetDebuff(speedMultiplier, jumpMultiplier);
        }
        else
        {
            if (p1 != null) p1.ResetDebuff();
            if (p2 != null) p2.ResetDebuff();
            if (p3 != null) p3.ResetDebuff();
        }
    }
}