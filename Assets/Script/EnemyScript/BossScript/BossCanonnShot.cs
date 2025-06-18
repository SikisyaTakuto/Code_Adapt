using UnityEngine;
using System.Collections;

public class BossCanonnShot : MonoBehaviour
{
    // �e�̔��ˏꏊ
    [SerializeField] private GameObject bulletPoint;
    // �e
    [SerializeField] private GameObject bullet;
    // �c�e��
    public float bulletCount;
    // �����e���̕ۑ�
    private float bulletAs;
    // �e�̑���
    public float Speed;
    // �����[�h
    bool reloading = false;
    // �N�[���^�C��
    bool coolTime = false;
    // EnemyDaed�A�j���[�V����
    public BossEnemyDead bossEnemyDead;

    void Start()
    {
        bossEnemyDead = GetComponent<BossEnemyDead>();

        bulletAs = bulletCount;
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (!bossEnemyDead.BossDead)
        {
            // Player���͈͓��ɓ������Ƃ�
            if (collider.gameObject.tag == "Player" && !reloading && !coolTime)
            {
                // �e�̔��ˏꏊ���擾
                Vector3 bulletPosition = bulletPoint.transform.position;
                // �e��Prefab���쐬
                GameObject newBullet = Instantiate(bullet, bulletPosition, this.gameObject.transform.rotation);
                // �e�̔��ˎ����擾�iZ���j
                Vector3 direction = newBullet.transform.forward;
                // �e�𔭎ˁiZ���j
                newBullet.GetComponent<Rigidbody>().AddForce(direction * Speed, ForceMode.Impulse);
                // �c�e�������炷
                bulletCount = bulletCount - 1;
                // �����[�h
                StartCoroutine(Shot());
            }
        }
    }

    private IEnumerator Shot()
    {
        if (bulletCount <= 0)
        {
            reloading = true;
            Debug.Log("�����[�h");

            // �����[�h����
            yield return new WaitForSeconds(10);

            bulletCount = bulletAs;
            reloading = false;
        }
        else
        {
            Debug.Log("�N�[���^�C��");
            coolTime = true;
            yield return new WaitForSeconds(3);
            coolTime = false;
        }
    }
}
