using UnityEngine;

public class FlyEnemyLooks : MonoBehaviour
{
    // Player�̕����Ɍ����ϐ�
    public Transform target;

    // ��]���x
    public float rotationSpeed;

    // ���S�����ꍇ�̃X�N���v�g
    public EnemyDaed enemyDaed;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player" && !enemyDaed.Dead)
        {
            // Player�̕����Ɍ���
            // �Ώە��Ǝ������g�̍��W����x�N�g�����Z�o���ĉ�]�l���擾
            Vector3 vector3 = target.transform.position - this.transform.position;
            // ��]�l���擾
            Quaternion quaternion = Quaternion.LookRotation(vector3);
            // �擾������]�l�����̃Q�[���I�u�W�F�N�g��rotation�ɑ��
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, quaternion, rotationSpeed);
        }
    }

    public void OnTriggerExit(Collider collider)
    {
        // Player���͈͊O�ɏo���Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // �i�s�����Ɍ���
            transform.rotation = Quaternion.identity;

        }
    }
}
