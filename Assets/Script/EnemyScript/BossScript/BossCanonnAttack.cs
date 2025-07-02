using UnityEngine;
using System.Collections;

public class BossCanonnAttack : MonoBehaviour
{
    // Player�̕����Ɍ����ϐ�
    public Transform target;
    // �e�̔��ˏꏊ
    [SerializeField] private GameObject bulletPoint;
    // �e
    [SerializeField] private GameObject bullet;
    // ��C
    [SerializeField] private GameObject taiho;
    // �c�e��
    public float bulletCount;
    // �����e��
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
        // �����e���̕ۑ�
        bulletAs = bulletCount;
    }

    void Update()
    {
        Vector3 targetPos = target.position;

        // ��C��Player�̕����Ɍ���
        taiho.transform.LookAt(targetPos);

        if (!bossEnemyDead.BossDead)
        {
            // �ˌ�
            Shot();

        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player")
        { 

        }
    }

    public void Shot()
    {
        // Player���͈͓��ɓ������Ƃ�
        if (!reloading && !coolTime)
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
            StartCoroutine(ShotTime());
        }
    }

    private IEnumerator ShotTime()
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
