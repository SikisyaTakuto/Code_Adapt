using UnityEngine;

public class BeamRifle : MonoBehaviour
{
    public float range = 100f;            // ビームの射程距離
    public float damage = 25f;            // ダメージ量
    public Camera fpsCamera;              // プレイヤーのカメラ（射線の起点）
    public ParticleSystem muzzleFlash;   // 発射時のエフェクト（任意）
    public GameObject impactEffectPrefab; // ヒット時のエフェクト（任意）

    void Update()
    {
        if (Input.GetButtonDown("Fire1")) // マウス左クリックやコントローラのトリガーなど
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // エフェクト再生
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        RaycastHit hit;
        Vector3 origin = fpsCamera.transform.position;
        Vector3 direction = fpsCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);

            // ヒットエフェクトを生成
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            //// ヒットした相手にダメージを与える（Healthスクリプトなどにアクセス）
            //var health = hit.transform.GetComponent<Health>();
            //if (health != null)
            //{
            //    health.TakeDamage(damage);
            //}
        }
    }
}
