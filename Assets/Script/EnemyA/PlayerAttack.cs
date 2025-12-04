using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float launchForce = 50f;   // 💡 発射する力 (低めに設定し、衝突バグを防ぐ)
    public Transform muzzlePoint;

    [System.Obsolete]
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            FireProjectile();
        }
    }

    [System.Obsolete]
    void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefabが設定されていません！**Projectウィンドウからドラッグ＆ドロップしてください**");
            return;
        }

        Transform spawnPoint = (muzzlePoint != null) ? muzzlePoint : transform;
        Vector3 spawnPosition = spawnPoint.position + spawnPoint.forward * 0.5f;

        GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, spawnPoint.rotation);

        Rigidbody rb = newProjectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 💡 物理演算のバグを避けるため、直接速度を代入します
            rb.velocity = spawnPoint.forward * launchForce;
        }

        // 弾の寿命設定は ProjectileDamage.cs で行っています。
    }
}