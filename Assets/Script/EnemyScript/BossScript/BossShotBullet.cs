using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.InputSettings;

public class BossShotBullet : MonoBehaviour
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
    // Inspector�ŃZ�b�g����p
    [SerializeField] private LookBossCanonn lookScript; 

    // �G�̎��S���
    public BossEnemyDead bossEnemyDaed;

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
        if (other.CompareTag("Player") && !bossEnemyDaed.BossDead)
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

        // �͈͊O��Player�����āA����ł��Ȃ���Ύˌ��J�n
        if (!isPlayerInRange && !bossEnemyDaed.BossDead)
        {
            StartCoroutine(ShootingLoop());
        }
        else
        {
            // ���K�v���Ȃ��Ȃ����ꍇ
            isShooting = false; 
        }
    }

    private IEnumerator ShootingLoop()
    {
        isShooting = true;

        while (!isPlayerInRange && !bossEnemyDaed.BossDead)
        {
            if (bulletCount > 0 && !reloading)
            {
                // �e�����O��Look���~�߂�
                DisableLookTemporarily();
                // ���˂̒��O�ɏ����҂�
                yield return new WaitForSeconds(0.5f);

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


    // �ˌ�
    private void ShootAtPlayer()
    {
        if (targetPlayer == null) return;

        // Look��~
        lookScript.TemporarilyDisableLook();

        Vector3 bulletPosition = bulletPoint.transform.position;
        Vector3 direction = bulletPoint.transform.forward;

        GameObject newBullet = Instantiate(bullet, bulletPosition, Quaternion.LookRotation(direction));
        newBullet.GetComponent<Rigidbody>().AddForce(direction * speed, ForceMode.Impulse);
    }

    private void DisableLookTemporarily()
    {
        if (lookScript != null)
        {
            lookScript.TemporarilyDisableLook();
        }
    }

    // �����[�h
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
