using UnityEngine;

public class BeamShooter : MonoBehaviour
{
    public GameObject beamPrefab;     // �r�[���̃v���n�u
    public Transform firePoint;       // ���ˈʒu�iTransform��Inspector�Ŏw��j
    public float beamDuration = 1f;   // �r�[���̎������ԁi�b�j

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // ���N���b�N
        {
            ShootBeam();
        }
    }

    void ShootBeam()
    {
        GameObject beam = Instantiate(beamPrefab, firePoint.position, firePoint.rotation);
        Destroy(beam, beamDuration); // 1�b��Ƀr�[�����폜
    }
}
