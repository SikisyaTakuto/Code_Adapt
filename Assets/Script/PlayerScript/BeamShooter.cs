using UnityEngine;

public class BeamShooter : MonoBehaviour
{
    public GameObject beamPrefab;     // ビームのプレハブ
    public Transform firePoint;       // 発射位置（TransformをInspectorで指定）
    public float beamDuration = 1f;   // ビームの持続時間（秒）

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 左クリック
        {
            ShootBeam();
        }
    }

    void ShootBeam()
    {
        GameObject beam = Instantiate(beamPrefab, firePoint.position, firePoint.rotation);
        Destroy(beam, beamDuration); // 1秒後にビームを削除
    }
}
