using UnityEngine;

public class FlyEnemyLooks : MonoBehaviour
{
    // Player�̕����Ɍ����ϐ�
    public Transform target;

    // ���S�����ꍇ�̃X�N���v�g
    public EnemyDaed enemyDaed;

    void Start()
    {
        enemyDaed = GetComponent<EnemyDaed>();
    }

    // Update is called once per frame
    void Update()
    {
        // �ϐ� targetPos ���쐬���ă^�[�Q�b�g�I�u�W�F�N�g�̍��W���i�[
        Vector3 targetPos = target.position;
        // �������g��Y���W��ϐ� target ��Y���W�Ɋi�[
        //�i�^�[�Q�b�g�I�u�W�F�N�g��X�AZ���W�̂ݎQ�Ɓj
        targetPos.y = transform.position.y;
    }

    public void OnDetectObject(Collider collider)
    {
        // Player���͈͓��ɓ������Ƃ�
        if (collider.gameObject.tag == "Player" && !enemyDaed.Dead)
        {
            // Player�̕����Ɍ���
            transform.LookAt(target);

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
