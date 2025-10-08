using UnityEngine;

public class TpsCamera : MonoBehaviour
{
    [Header("ターゲットと設定")]
    [Tooltip("追従させるプレイヤーオブジェクトを設定")]
    public Transform target;        // 追従するターゲット（プレイヤー）

    public float distance = 10.0f;   // プレイヤーからの距離
    public float sensitivity = 3.0f; // マウス感度
    public float smoothSpeed = 10.0f; // カメラの追従速度（滑らかさ）

    // === 追従のガタつき修正のための設定を追加 ===
    [Tooltip("プレイヤーのY軸追従の遅延/滑らかさ。値を小さくすると追従が遅れ、ガタつきが減る。")]
    public float heightSmoothSpeed = 2.0f;

    [Header("角度制限")]
    public float yMinLimit = -40f;  // 垂直方向の最小角度（下限）
    public float yMaxLimit = 80f;   // 垂直方向の最大角度（上限）

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    void Start()
    {
        // マウスカーソルを非表示にし、画面中央にロックする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            // 初期角度を設定（プレイヤーの初期回転を利用）
            currentX = target.eulerAngles.y;
            currentY = target.eulerAngles.x;
        }
    }

    void Update()
    {
        if (target == null) return;

        // マウス入力を取得し、現在の角度を更新
        currentX += Input.GetAxis("Mouse X") * sensitivity;
        currentY -= Input.GetAxis("Mouse Y") * sensitivity;

        // 垂直方向の角度を制限
        currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 角度から回転を計算
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // 2. プレイヤーを回転させる (前回のコードから変更なし)
        Quaternion targetRotation = Quaternion.Euler(0, currentX, 0);
        target.rotation = Quaternion.Slerp(target.rotation, targetRotation, smoothSpeed * Time.deltaTime);

        // 3. プレイヤーの目標位置を計算
        Vector3 direction = rotation * Vector3.back;

        // 4. 最終的な目標位置を決定
        Vector3 targetPosition = target.position;
        Vector3 desiredPosition = targetPosition + direction * distance;

        // ==========================================================
        // 🛠️ 【修正箇所】Y軸の追従を滑らかにする
        // ==========================================================

        // Y軸（高さ）を目標の高さに滑らかに近づける
        float newY = Mathf.Lerp(transform.position.y, desiredPosition.y, heightSmoothSpeed * Time.deltaTime);

        // カメラの目標位置を、滑らかに追従するY軸と、即座に追従するX-Z軸で構成
        Vector3 smoothedPosition = new Vector3(
            desiredPosition.x, // X軸は即座に追従
            newY,              // Y軸は滑らかに追従 (Mathf.Lerp)
            desiredPosition.z  // Z軸は即座に追従
        );

        // 5. カメラを目標位置に移動させる
        // X-Z軸は既に目標位置へ移動させるロジックを使っているため、ここではY軸のスムージングのみを反映
        transform.position = smoothedPosition;

        // 6. プレイヤーの方向を見るようにカメラを回転させる
        // ただし、LookAtはTargetのY軸を見るため、カメラがTargetの頭部を追従しすぎる可能性を避けるために
        // プレイヤーの位置よりも少し上（プレイヤーの頭付近）を見るように調整することが一般的です。
        // ここではシンプルに、プレイヤーのX-Z位置を基準とし、Y軸はカメラ自身のY座標を使用します。

        Vector3 lookAtPoint = targetPosition;

        // 追従ポイントをプレイヤーのY座標よりも少し上（例：0.5m）に設定すると自然になります
        // lookAtPoint.y += 0.5f; 

        transform.LookAt(lookAtPoint);
    }

    /// <summary>
    /// 角度を制限するためのヘルパー関数
    /// </summary>
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}