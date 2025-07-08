using System.Collections;
using UnityEngine;

public class ShotBullet : MonoBehaviour
{
    // �e�̔��ˈʒu
    [SerializeField] private GameObject bulletPoint;
    // �e��Prefab
    [SerializeField] private GameObject bullet;
    // �}�K�W���̒e��
    [SerializeField] private float bulletCount = 20f;
    // �e�̔��ˊԊu
    [SerializeField] private float shotInterval = 1.5f;
    // �����[�h����
    [SerializeField] private float reloadTime = 3.0f;
    // �e��
    [SerializeField] private float speed = 20f;
    // �����n�߂�܂ł̑ҋ@����
    [SerializeField] private float shootingStartDelay = 1.0f;
    // �G�̎��S���
    public EnemyDaed enemyDaed;

    private float initialBulletCount;
    private bool isPlayerInRange = false;
    private bool isShooting = false;
    private bool reloading = false;
    private Transform targetPlayer;

    void Start()
    {
        // �����}�K�W���̒e��
        initialBulletCount = bulletCount;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !enemyDaed.Dead)
        {
            isPlayerInRange = true;
            targetPlayer = other.transform;

            if (!isShooting)
            {
                StartCoroutine(StartShootingAfterDelay());
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private IEnumerator StartShootingAfterDelay()
    {
        isShooting = true;
        yield return new WaitForSeconds(shootingStartDelay);

        // �͈͓��ɂ܂����āA�G������ł��Ȃ���Ύˌ��J�n
        if (isPlayerInRange && !enemyDaed.Dead)
        {
            StartCoroutine(ShootingLoop());
        }
        else
        {
            isShooting = false; // ���K�v���Ȃ��Ȃ����ꍇ
        }
    }

    private IEnumerator ShootingLoop()
    {
        isShooting = true;

        while (isPlayerInRange && !enemyDaed.Dead)
        {
            if (bulletCount > 0 && !reloading)
            {
                ShootAtPlayer();
                bulletCount--;
            }
            else if (!reloading)
            {
                yield return StartCoroutine(Reload());
            }

            yield return new WaitForSeconds(shotInterval);
        }

        isShooting = false;
    }

    private void ShootAtPlayer()
    {
        if (targetPlayer == null) return;

        Vector3 bulletPosition = bulletPoint.transform.position;
        Vector3 direction = (targetPlayer.position - bulletPosition).normalized;

        GameObject newBullet = Instantiate(bullet, bulletPosition, Quaternion.LookRotation(direction));
        newBullet.GetComponent<Rigidbody>().AddForce(direction * speed, ForceMode.Impulse);
    }

    private IEnumerator Reload()
    {
        reloading = true;
        Debug.Log("�����[�h��...");
        yield return new WaitForSeconds(reloadTime);
        bulletCount = initialBulletCount;
        reloading = false;
        Debug.Log("�����[�h����");
    }
}
