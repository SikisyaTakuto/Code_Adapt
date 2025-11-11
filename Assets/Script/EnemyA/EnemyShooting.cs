using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    
    public GameObject bulletPrefab; // �e�̃v���n�u
    public Transform firePoint;     // ���ˈʒu

    public float bulletSpeed = 10f;

    void Update()
    {
        if (ShouldShoot()) // ���˃^�C�~���O�̔���i��j
        {
            Shoot();
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = firePoint.forward * bulletSpeed;
    }

    bool ShouldShoot()
    {
        // ��F�����_���Ɍ��� or �v���C���[���������Ƃ��Ɍ���
        return false; // ���ۂ̏����������ɏ���
    }
}
