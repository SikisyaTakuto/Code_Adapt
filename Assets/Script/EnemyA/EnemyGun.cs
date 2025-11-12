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
    public void Shoot()
    {
        if (muzzlePoint != null && bulletPrefab != null)
        {
            Debug.Log("弾丸の生成を試みます: Time = " + Time.time); // ★ 追加

            GameObject bulletInstance = Instantiate(
                bulletPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation
            );

            Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Debug.Log("弾丸に速度を設定しました。"); // ★ 追加
                rb.velocity = muzzlePoint.forward * shotSpeed;
            }
            else
            {
                Debug.LogError("弾丸プレハブにRigidbodyがありません！"); // ★ 追加
            }
        }
    }


}