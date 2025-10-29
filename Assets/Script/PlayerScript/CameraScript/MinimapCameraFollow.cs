using UnityEngine;

/// <summary>
/// �~�j�}�b�v�p�̃J���������̃^�[�Q�b�g�i�v���C���[�j�ɒǏ]������B
/// </summary>
public class MinimapCameraFollow : MonoBehaviour
{
    // �Ǐ]������^�[�Q�b�g��Transform (Inspector��Player�I�u�W�F�N�g�����蓖�Ă�)
    [Header("�^�[�Q�b�g")]
    public Transform target;

    // �J�����̍����iY���̃I�t�Z�b�g�j
    [Header("�J�����̍���")]
    public float heightOffset = 20f;

    // �J�������Ǐ]���Ƀv���C���[�̉�]�𖳎����邩�ǂ���
    [Header("�v���C���[�̉�]�𔽉f���邩")]
    public bool followRotation = false;

    // �J�����̏�����]�i�ʏ�A�^�ォ�猩�邽�߂ɐݒ�ς݁j
    // private Quaternion initialRotation;


    void Start()
    {
        // �G�f�B�^�Ń^�[�Q�b�g���ݒ肳��Ă��邩�m�F
        if (target == null)
        {
            Debug.LogError("�^�[�Q�b�g�iPlayer�j���ݒ肳��Ă��܂���BInspector�Őݒ肵�Ă��������B", this);
            enabled = false;
            return;
        }

        // �J�����̏�����]�i�K�v�ɉ����āj
        // initialRotation = transform.rotation;
    }

    // �J�����̒Ǐ]�́A����������������Ɏ��s�����LateUpdate�ōs���̂���ʓI�ł��B
    void LateUpdate()
    {
        // �^�[�Q�b�g���Ȃ���Ώ������Ȃ�
        if (target == null) return;

        // 1. �ʒu�̒Ǐ]�iX, Z���W�̂݁j
        Vector3 newPosition = target.position;
        newPosition.y = target.position.y + heightOffset; // �v���C���[�̈ʒu+�ݒ肳�ꂽ����

        transform.position = newPosition;

        // 2. ��]�̒Ǐ]�i�I�v�V�����j
        if (followRotation)
        {
            // �v���C���[��Y����]�݂̂��R�s�[
            Quaternion targetRotation = Quaternion.Euler(
                transform.eulerAngles.x, // �J�����̌��݂�X��]���ێ��i�^�����������܂܁j
                target.eulerAngles.y,    // �v���C���[��Y��]���R�s�[
                transform.eulerAngles.z  // �J�����̌��݂�Z��]���ێ�
            );
            transform.rotation = targetRotation;
        }
        // else
        // {
        //     // �v���C���[�̉�]�Ɋւ�炸�A�J�����͌Œ肳�ꂽ�p�x���ێ�
        //     transform.rotation = initialRotation;
        // }
    }
}