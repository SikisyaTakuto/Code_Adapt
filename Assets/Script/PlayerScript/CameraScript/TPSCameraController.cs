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

    // ★ここから追加・変更★
    private bool isFixedViewMode = false; // カメラが固定ビューモードかどうか
    private Vector3 fixedTargetPosition; // 固定ビューモードでの目標位置
    private Quaternion fixedTargetRotation; // 固定ビューモードでの目標回転
    private float fixedViewSmoothTime; // 固定ビューモードでのスムーズ時間

    // チュートリアルなどでカーソルを一時的に表示・非表示するためのフラグ
    private bool _cursorLockedInitially = false;

    void Start()
    {
        // カーソルをロックして非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _cursorLockedInitially = true; // 初期状態でロックしたことを記録

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

        // 固定ビューモードの場合は、設定された位置と回転にスムーズに移動
        if (isFixedViewMode)
        {
            // Time.unscaledDeltaTime を使用して、Time.timeScale = 0 の状態でもスムーズに動くようにする
            transform.position = Vector3.Lerp(transform.position, fixedTargetPosition, Time.unscaledDeltaTime * fixedViewSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, fixedTargetRotation, Time.unscaledDeltaTime * fixedViewSmoothTime);
            return; // 固定ビューモード中は通常のカメラロジックをスキップ
        }

        // 通常のカメラ追従ロジック
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

    /// <summary>
    /// カメラを特定の固定位置と回転に設定し、そのビューにスムーズに移動します。
    /// Time.timeScaleが0でも動作します。
    /// </summary>
    /// <param name="position">カメラの目標ワールド位置。</param>
    /// <param name="rotation">カメラの目標ワールド回転。</param>
    /// <param name="smoothTime">目標位置・回転に到達するまでのスムーズ時間。</param>
    public void SetFixedCameraView(Vector3 position, Quaternion rotation, float smoothTime)
    {
        isFixedViewMode = true;
        fixedTargetPosition = position;
        fixedTargetRotation = rotation;
        fixedViewSmoothTime = smoothTime;

        // 固定ビューモード中はカーソルを表示し、ロックを解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// カメラを通常のTPS追従モードに戻します。
    /// Time.timeScaleが0でも動作します。
    /// </summary>
    /// <param name="smoothTime">通常の追従モードに戻るまでのスムーズ時間。</param>
    public void ResetToTPSView(float smoothTime)
    {
        isFixedViewMode = false;
        // fixedViewSmoothTimeを更新して、スムーズに戻るようにする
        fixedViewSmoothTime = smoothTime;

        // 通常のTPSモードに戻る際にカーソルを元の状態に戻す
        if (_cursorLockedInitially)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // プレイヤーの向きをカメラの水平方向の向きに合わせるためのメソッド
    public void RotatePlayerToCameraDirection()
    {
        if (target == null || isFixedViewMode) return; // 固定ビューモード中はプレイヤーの回転を制御しない

        // プレイヤーをY軸回転のみでカメラのY軸回転に合わせる
        Quaternion playerRotation = Quaternion.Euler(0, yaw, 0);
        target.rotation = Quaternion.Slerp(target.rotation, playerRotation, Time.deltaTime * smoothSpeed);
    }

    // カメラの中心点を取得するメソッド
    public Vector3 GetCameraCenterPoint()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera not found! Make sure your camera is tagged 'MainCamera'.");
            return transform.position;
        }
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return ray.origin;
    }

    // カメラからRayを取得するメソッド
    public Ray GetCameraRay()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera not found! Make sure your camera is tagged 'MainCamera'.");
            return new Ray(transform.position, transform.forward);
        }
        return mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    }
}