using UnityEngine;

public class BeamShooter : MonoBehaviour
{
    public GameObject beamPrefab;       // ビームのプレハブ
    public Transform firePoint;         // 発射位置
    public float beamDuration = 1f;     // ビームの寿命
    public Animator animator;           // Animator 参照
    public float aimDuration = 1.0f;    // 構え時間（射撃前）
    public float shootDuration = 0.5f;  // 撃つアニメーション再生時間

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

        // 1. 構え（isAiming = true）
        animator.SetBool("isAiming", true);
        yield return new WaitForSeconds(aimDuration);

        // 2. 撃つ
        animator.SetTrigger("Shoot");
        ShootBeam();
        yield return new WaitForSeconds(shootDuration);

        // 3. 戻す（isAiming = false）
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
