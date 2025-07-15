using UnityEngine;

public class TPSCameraController : MonoBehaviour
{
    public Transform target; // 追従するターゲット（プレイヤーなど）
    public float distance = 5.0f; // ターゲットからの距離
    public float height = 2.0f;    // ターゲットからの高さ
    public float rotationSpeed = 3.0f; // カメラの回転速度（マウス速度）
    public float smoothSpeed = 10.0f; // カメラの移動と回転のスムーズさ

    public Vector2 pitchMinMax = new Vector2(-40, 85); // 垂直方向のカメラ角度制限

    public LayerMask collisionLayers; // カメラが衝突をチェックするレイヤー（壁や地面など）
    public float collisionOffset = 0.2f; // 衝突時にカメラが押し戻されるオフセット

    private float yaw = 0.0f;    // 左右の回転角度 (Y軸)
    private float pitch = 0.0f; // 上下の回転角度 (X軸)

    void Start()
    {
        // カーソルをロックして非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 初期角度を設定 (プレイヤーの向きに合わせる)
        if (target != null)
        {
            Vector3 relativePos = transform.position - target.position;
            Quaternion rotation = Quaternion.LookRotation(relativePos);
            yaw = rotation.eulerAngles.y;
            pitch = rotation.eulerAngles.x;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // マウス入力でカメラの角度を更新
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y); // 垂直角度を制限

        // カメラの目標回転 (Euler角度からQuaternionに変換)
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);

        // カメラの目標位置を計算
        Vector3 targetPosition = target.position + Vector3.up * height - targetRotation * Vector3.forward * distance;

        // カメラの衝突判定
        RaycastHit hit;
        Vector3 currentTargetPos = target.position + Vector3.up * height; // ターゲットの中心を含む位置
        if (Physics.Linecast(currentTargetPos, targetPosition, out hit, collisionLayers))
        {
            // 衝突があった場合、衝突点から少し手前にカメラを配置
            targetPosition = hit.point + hit.normal * collisionOffset;
        }

        // カメラの位置と回転をLerpでスムーズに補間
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    // プレイヤーの向きをカメラの水平方向の向きに合わせるためのメソッド
    public void RotatePlayerToCameraDirection()
    {
        if (target == null) return;

        // プレイヤーをY軸回転のみでカメラのY軸回転に合わせる
        Quaternion playerRotation = Quaternion.Euler(0, yaw, 0);
        target.rotation = Quaternion.Slerp(target.rotation, playerRotation, Time.deltaTime * smoothSpeed);
    }

    // カメラの中心点を取得するメソッド
    public Vector3 GetCameraCenterPoint()
    {
        // メインカメラを取得
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera not found! Make sure your camera is tagged 'MainCamera'.");
            return transform.position; // フォールバックとしてコントローラーの位置を返す
        }

        // カメラのビューポートの中心からRayを飛ばす
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return ray.origin;
    }

    // カメラからRayを取得するメソッド
    public Ray GetCameraRay()
    {
        // メインカメラを取得
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera not found! Make sure your camera is tagged 'MainCamera'.");
            return new Ray(transform.position, transform.forward); // フォールバックとして前方を向くRayを返す
        }

        // カメラのビューポートの中心からRayを飛ばす
        return mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    }
}
