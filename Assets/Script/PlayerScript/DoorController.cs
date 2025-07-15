using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    // 扉の開閉方法を選択 (常にTranslateUp)
    // public enum DoorMovementType { TranslateUp, RotateY } // 回転オプションを削除
    // public DoorMovementType movementType = DoorMovementType.TranslateUp; // 固定するため不要

    public float openDistance = 5.0f; // 上に開く距離
    public float openSpeed = 1.0f; // 開閉速度

    private Vector3 initialPosition;
    // private Quaternion initialRotation; // 回転しないため不要
    private bool isOpen = false;
    private Coroutine currentDoorCoroutine;

    void Start()
    {
        initialPosition = transform.position;
        // initialRotation = transform.rotation; // 回転しないため不要
    }

    /// <summary>
    /// 扉を開く
    /// </summary>
    /// <param name="duration">開くアニメーションにかかる時間</param>
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
    /// 扉を閉じる
    /// </summary>
    /// <param name="duration">閉じるアニメーションにかかる時間</param>
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
        // Quaternion startRot = transform.rotation; // 回転しないため不要

        Vector3 endPos = initialPosition;
        // Quaternion endRot = initialRotation; // 回転しないため不要

        if (opening)
        {
            // movementType == DoorMovementType.TranslateUp の場合のみを考慮
            endPos = initialPosition + Vector3.up * openDistance;
        }
        // else: closingの場合はinitialPositionが目標

        while (elapsedTime < duration)
        {
            // TranslateUp の動作のみを実行
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 最終的な位置に設定
        transform.position = endPos;

        isOpen = opening;
        currentDoorCoroutine = null;
    }
}
