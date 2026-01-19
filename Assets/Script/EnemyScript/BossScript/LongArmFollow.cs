using UnityEngine;

public class LongArmFollow : MonoBehaviour
{
    public Transform player;
    public float followSpeed = 2f;
    public float rotationSpeed = 5f;
    public Vector3 offset = new Vector3(0, 1, 2);

    [Header("めり込み・制限設定")]
    public float minDistance = 1.5f;
    public bool lockZRotation = true;

    [Header("Y軸回転制限")]
    public float minYAngle = -60f; // 左への限界角度
    public float maxYAngle = 60f;  // 右への限界角度

    private Quaternion initialRotation;

    void Start()
    {
        // ゲーム開始時の回転を「正面」の基準として保存
        initialRotation = transform.rotation;
    }

    void Update()
    {
        if (player == null) return;

        // --- 1. 位置の計算 ---
        Vector3 targetPosition = player.position + offset;
        float dist = Vector3.Distance(targetPosition, player.position);
        if (dist < minDistance)
        {
            Vector3 dirFromPlayer = (targetPosition - player.position).normalized;
            targetPosition = player.position + dirFromPlayer * minDistance;
        }
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // --- 2. 回転の計算（Y軸制限付き） ---
        Vector3 direction = (player.position - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            // プレイヤーへのターゲット回転
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 角度制限の処理
            Vector3 targetEuler = targetRotation.eulerAngles;

            // Y軸の角度を -180 ? 180 の範囲に変換して制限しやすくする
            float angleY = Mathf.DeltaAngle(initialRotation.eulerAngles.y, targetEuler.y);
            angleY = Mathf.Clamp(angleY, minYAngle, maxYAngle);

            // 制限した角度を再構成
            float finalY = initialRotation.eulerAngles.y + angleY;
            float finalX = targetEuler.x; // 上下（X軸）はそのまま
            float finalZ = lockZRotation ? 0 : targetEuler.z;

            Quaternion clampedRotation = Quaternion.Euler(finalX, finalY, finalZ);

            // スムーズに回転
            transform.rotation = Quaternion.Slerp(transform.rotation, clampedRotation, Time.deltaTime * rotationSpeed);
        }
    }
}