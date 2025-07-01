using UnityEngine;
using System.Collections;

public class BossCanonnShot : MonoBehaviour
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

    private void Update()
    {
        Vector3 targetPos = target.position;
        //targetPos.y = transform.position.y;
        taiho.transform.LookAt(targetPos);
    }

    public void OnTriggerEnter(Collider collider)
    {
        // ��C��Player�̕����Ɍ���

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
