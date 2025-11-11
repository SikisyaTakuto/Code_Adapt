using UnityEngine;

public class EnemyGun : MonoBehaviour
{
    // インスペクターから設定: 弾丸のプレハブ
    [SerializeField] private GameObject bulletPrefab;

    // インスペクターから設定: 弾丸が発射される位置 (銃口の空のGameObject)
    [SerializeField] private Transform muzzlePoint;

    [SerializeField] private float shotSpeed = 15f; // 弾速
    [SerializeField] private float fireRate = 1.5f; // 発射間隔 (秒)

    private float nextFireTime = 0f;

    void Update()
    {
        // 発射時間になったら
        if (Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        // 発射に必要なオブジェクトが有効であることを確認
        if (muzzlePoint != null && bulletPrefab != null)
        {
            // 1. 弾丸を生成する
            GameObject bulletInstance = Instantiate(
                bulletPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation
            );

            // 2. 弾丸に推進力を加える
            Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();

            if (rb != null)
            {
                // MuzzlePointの向いている前方 (forward) に速度を加える
                rb.velocity = muzzlePoint.forward * shotSpeed; // ★ ここを修正
            }
        }
        // else の Debug.Log は、エラー回避の観点からコメントアウトを推奨します
    }
}