using UnityEngine;

public class PlayerBitController : MonoBehaviour
{
    public GameObject bitPrefab;
    public Transform[] bitPositions = new Transform[4];  // �v���C���[����̃r�b�g�ҋ@�ʒu
    private BitBehavior[] bits = new BitBehavior[4];
    private int nextBitIndex = 0;

    void Start()
    {
        // �r�b�g�𐶐����đҋ@�ʒu�ɃZ�b�g�A�r�b�g�z��ɕێ�
        for (int i = 0; i < bitPositions.Length; i++)
        {
            GameObject bitObj = Instantiate(bitPrefab, bitPositions[i].position, Quaternion.identity);
            BitBehavior bit = bitObj.GetComponent<BitBehavior>();
            if (bit != null)
            {
                bit.Initialize(this, bitPositions[i]);
                bits[i] = bit;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            LaunchNextBit();
        }
    }

    void LaunchNextBit()
    {
        // �ˏo�\�ȃr�b�g��T���i�ҋ@��Ԃ̃r�b�g�̂݁j
        for (int i = 0; i < bits.Length; i++)
        {
            int index = (nextBitIndex + i) % bits.Length;
            if (bits[index] != null && bits[index].IsIdle())
            {
                bits[index].Launch();
                nextBitIndex = (index + 1) % bits.Length;
                break;
            }
        }
    }
}
