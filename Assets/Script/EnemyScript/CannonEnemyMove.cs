using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class CannonEnemyMove : MonoBehaviour
{
    // Player�̕����Ɍ����ϐ�
    public Transform target;
    // Player��ǂ�������ړ����x
    public float moveSpeed;
    // Player�ƕۂ���
    public float stopDistance;
    // Player�����G����͈�
    public float moveDistance;
    // NavMeshAgent
    private NavMeshAgent navMeshAgent;
    // �ړI�n�̔z��
    [SerializeField] private Transform[] waypointArray;
    // ���݂̖ړI�n
    private int currentWaypointIndex = 0;
    // ���S�����ꍇ�̃X�N���v�g
    public EnemyDaed enemyDaed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();
        // �ŏ��̖ړI�n
        navMeshAgent.SetDestination(waypointArray[currentWaypointIndex].position);

        enemyDaed = GetComponent<EnemyDaed>();
    }

    void Update()
    {
        // �ϐ� targetPos ���쐬���ă^�[�Q�b�g�I�u�W�F�N�g�̍��W���i�[
        Vector3 targetPos = target.position;

        // �������Ă���ꍇ
        if (!enemyDaed.Dead)
        {
            // �������g��Y���W��ϐ� target ��Y���W�Ɋi�[
            //�i�^�[�Q�b�g�I�u�W�F�N�g��X�AZ���W�̂ݎQ�Ɓj
            targetPos.y = transform.position.y;

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                // �ړI�n�̔ԍ����P�X�V
                currentWaypointIndex = (currentWaypointIndex + 1) % waypointArray.Length;
                // �ړI�n�����̏ꏊ�ɐݒ�
                navMeshAgent.SetDestination(waypointArray[currentWaypointIndex].position);
            }

            // �ϐ� distance ���쐬���ăI�u�W�F�N�g�̈ʒu�ƃ^�[�Q�b�g�I�u�W�F�N�g�̋������i�[
            float distance = Vector3.Distance(transform.position, target.position);
            // �I�u�W�F�N�g�ƃ^�[�Q�b�g�I�u�W�F�N�g�̋�������
            // �ϐ� distance�i�^�[�Q�b�g�I�u�W�F�N�g�ƃI�u�W�F�N�g�̋����j���ϐ� moveDistance �̒l��菬�������
            // ����ɕϐ� distance ���ϐ� stopDistance �̒l�����傫���ꍇ
            if (distance < moveDistance && distance > stopDistance)
            {
                // �I�u�W�F�N�g��ϐ� targetPos �̍��W�����Ɍ�������
                transform.LookAt(targetPos);
                // �ϐ� moveSpeed ����Z�������x�ŃI�u�W�F�N�g��O�����Ɉړ�����
                transform.position = transform.position + transform.forward * moveSpeed * Time.deltaTime;
            }
        }
        else
        {
            // ���̏�Ŏ~�܂�
            ZeroSpeed();
        }
    }

    // Player���߂Â����ꍇ
    public void OnDetectObject(Collider collider)
    {
        if (!enemyDaed.Dead)
        {
            // Player���͈͓��ɓ������Ƃ�
            if (collider.gameObject.tag == "Player")
            {
                // Player���U��
                transform.LookAt(target);
                navMeshAgent.speed = 0f;
            }
        }
    }

    // Player�����ꂽ�ꍇ
    public void OnLoseObject(Collider collider)
    {
        // Player���͈͊O�ɏo���Ƃ�
        if (collider.gameObject.tag == "Player")
        {
            // ���̏�Ŏ~�܂�
            navMeshAgent.speed = 50f;
        }
    }

    private void ZeroSpeed()
    {
        navMeshAgent.speed = 0f;
        moveSpeed = 0f;
    }
}
