using UnityEngine;

public class PlayerBitController : MonoBehaviour
{
    // �r�b�g�̃v���n�u�i�C���X�y�N�^�[�Őݒ�j
    public GameObject bitPrefab;

    // �v���C���[����ɔz�u����r�b�g�̑ҋ@�ʒu�iTransform�z��A�T�C�Y��4�j
    public Transform[] bitPositions = new Transform[4];

    // �������ꂽ�r�b�g�̃X�N���v�g�Q�Ƃ��i�[����z��i�T�C�Y��4�j
    private BitBehavior[] bits = new BitBehavior[4];

    // ���Ɏˏo����r�b�g�̃C���f�b�N�X���Ǘ�
    private int nextBitIndex = 0;

    void Start()
    {
        // �r�b�g�𐶐����A���ꂼ��̑ҋ@�ʒu�ɃZ�b�g���郋�[�v
        for (int i = 0; i < bitPositions.Length; i++)
        {
            // bitPrefab����r�b�g�I�u�W�F�N�g�𐶐����A�ҋ@�ʒu�̍��W�ɔz�u
            GameObject bitObj = Instantiate(bitPrefab, bitPositions[i].position, Quaternion.identity);

            // ���������I�u�W�F�N�g����BitBehavior�R���|�[�l���g���擾
            BitBehavior bit = bitObj.GetComponent<BitBehavior>();

            // BitBehavior�����݂���Ώ������������s���A�z��ɕێ�
            if (bit != null)
            {
                // �r�b�g�ɂ��̃R���g���[���[�Ƒҋ@�ʒu��n���ď�����
                bit.Initialize(this, bitPositions[i]);

                // bits�z��ɓo�^
                bits[i] = bit;
            }
        }
    }

    void Update()
    {
        // ���t���[���AG�L�[�������ꂽ�����`�F�b�N
        if (Input.GetKeyDown(KeyCode.G))
        {
            // G�L�[�������ꂽ�玟�̃r�b�g���ˏo����
            LaunchNextBit();
        }
    }

    // �ˏo�\�Ȏ��̃r�b�g��T���Ďˏo�������s�����\�b�h
    void LaunchNextBit()
    {
        // �r�b�g�z������Ԃɒ��ׂ郋�[�v
        // nextBitIndex����n�߂ď��Ƀ`�F�b�N���A�ҋ@��Ԃ̃r�b�g��������
        for (int i = 0; i < bits.Length; i++)
        {
            // �z��̃C���f�b�N�X�����[�v�����邽�߂̌v�Z
            int index = (nextBitIndex + i) % bits.Length;

            // �r�b�g�����݂��Ă��āA���ҋ@���i�ˏo�\�j�ł����
            if (bits[index] != null && bits[index].IsIdle())
            {
                // �Y���r�b�g���ˏo����
                bits[index].Launch();

                // ����͂��̎��̃r�b�g����T���悤�ɃC���f�b�N�X���X�V
                nextBitIndex = (index + 1) % bits.Length;

                // �ˏo���������̂Ń��[�v�𔲂���
                break;
            }
        }
    }
}
