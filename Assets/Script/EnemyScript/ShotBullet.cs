using UnityEngine;

public class ShotBullet : MonoBehaviour
{
    // �e�̔��ˏꏊ
    [SerializeField] private GameObject bulletPoint;
    // �e
    [SerializeField] private GameObject bullet;
    // �e�̑���
    public float Speed;

    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // �e�̔��ˏꏊ���擾
            Vector3 bulletPosition = bulletPoint.transform.position;
            // �e��Prefab���쐬
            GameObject newBullet = Instantiate(bullet, bulletPosition, this.gameObject.transform.rotation);
            // �e�̔��ˎ����擾�iZ���j
            Vector3 direction = newBullet.transform.forward;
            // �e�𔭎ˁiZ���j
            newBullet.GetComponent<Rigidbody>().AddForce(direction * Speed, ForceMode.Impulse);
            // ���˂����e���폜
            //Destroy(newBullet, 0.8f);
            //Debug.Log("����");
        }
    }
}
