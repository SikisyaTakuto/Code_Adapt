using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    public Transform target; // �Ǐ]����Ώہi�v���C���[�Ȃǁj
    public Vector3 offset = new Vector3(0, 50, 0); // �����ƈʒu�̒���

    void LateUpdate()
    {
        if (target != null)
        {
            // �v���C���[�̈ʒu�ɒǏ]�i��]�Ȃ��j
            Vector3 newPos = target.position + offset;
            transform.position = newPos;

            // �^���������i�^�ォ��̎��_�j
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
