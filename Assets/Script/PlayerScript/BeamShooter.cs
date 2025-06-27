using UnityEngine;

public class BeamShooter : MonoBehaviour
{
    public GameObject beamPrefab;       // �r�[���̃v���n�u
    public Transform firePoint;         // ���ˈʒu
    public float beamDuration = 1f;     // �r�[���̎���
    public Animator animator;           // Animator �Q��
    public float aimDuration = 1.0f;    // �\�����ԁi�ˌ��O�j
    public float shootDuration = 0.5f;  // ���A�j���[�V�����Đ�����

    private bool isShooting = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isShooting)
        {
            StartCoroutine(AimAndShootSequence());
        }
    }

    private System.Collections.IEnumerator AimAndShootSequence()
    {
        isShooting = true;

        // 1. �\���iisAiming = true�j
        animator.SetBool("isAiming", true);
        yield return new WaitForSeconds(aimDuration);

        // 2. ����
        animator.SetTrigger("Shoot");
        ShootBeam();
        yield return new WaitForSeconds(shootDuration);

        // 3. �߂��iisAiming = false�j
        animator.SetBool("isAiming", false);
        yield return new WaitForSeconds(aimDuration);

        isShooting = false;
    }

    private void ShootBeam()
    {
        GameObject beam = Instantiate(beamPrefab, firePoint.position, firePoint.rotation);
        Destroy(beam, beamDuration);
    }
}
