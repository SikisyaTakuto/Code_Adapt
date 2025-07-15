using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    // ���̊J���@��I�� (���TranslateUp)
    // public enum DoorMovementType { TranslateUp, RotateY } // ��]�I�v�V�������폜
    // public DoorMovementType movementType = DoorMovementType.TranslateUp; // �Œ肷�邽�ߕs�v

    public float openDistance = 5.0f; // ��ɊJ������
    public float openSpeed = 1.0f; // �J���x

    private Vector3 initialPosition;
    // private Quaternion initialRotation; // ��]���Ȃ����ߕs�v
    private bool isOpen = false;
    private Coroutine currentDoorCoroutine;

    void Start()
    {
        initialPosition = transform.position;
        // initialRotation = transform.rotation; // ��]���Ȃ����ߕs�v
    }

    /// <summary>
    /// �����J��
    /// </summary>
    /// <param name="duration">�J���A�j���[�V�����ɂ����鎞��</param>
    public void OpenDoor(float duration)
    {
        if (!isOpen)
        {
            if (currentDoorCoroutine != null)
            {
                StopCoroutine(currentDoorCoroutine);
            }
            currentDoorCoroutine = StartCoroutine(AnimateDoor(true, duration));
        }
    }

    /// <summary>
    /// �������
    /// </summary>
    /// <param name="duration">����A�j���[�V�����ɂ����鎞��</param>
    public void CloseDoor(float duration)
    {
        if (isOpen)
        {
            if (currentDoorCoroutine != null)
            {
                StopCoroutine(currentDoorCoroutine);
            }
            currentDoorCoroutine = StartCoroutine(AnimateDoor(false, duration));
        }
    }

    IEnumerator AnimateDoor(bool opening, float duration)
    {
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        // Quaternion startRot = transform.rotation; // ��]���Ȃ����ߕs�v

        Vector3 endPos = initialPosition;
        // Quaternion endRot = initialRotation; // ��]���Ȃ����ߕs�v

        if (opening)
        {
            // movementType == DoorMovementType.TranslateUp �̏ꍇ�݂̂��l��
            endPos = initialPosition + Vector3.up * openDistance;
        }
        // else: closing�̏ꍇ��initialPosition���ڕW

        while (elapsedTime < duration)
        {
            // TranslateUp �̓���݂̂����s
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // �ŏI�I�Ȉʒu�ɐݒ�
        transform.position = endPos;

        isOpen = opening;
        currentDoorCoroutine = null;
    }
}
