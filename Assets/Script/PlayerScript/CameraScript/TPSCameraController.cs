using UnityEngine;

/// <summary>
/// ターゲットを追跡し、マウス入力で操作可能な三人称視点（TPS）カメラを制御します。
/// カメラ衝突と固定ビューモードに対応しています。
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    [Header("Target and Distance")]
    [Tooltip("カメラが追従するターゲット（通常はプレイヤー）のTransform。")]
    public Transform target; // 追従するターゲット（プレイヤーなど）

    [Tooltip("ターゲットの中心からカメラまでの理想的な距離。")]
    public float distance = 5.0f; // ターゲットからの距離

    [Tooltip("ターゲットの中心からカメラまでの相対的な高さ。")]
    public float height = 2.0f;    // ターゲットからの高さ

    // --------------------------------------------------------------------------

    [Header("Camera Control")]
    [Tooltip("マウス入力によるカメラの回転速度（感度）。")]
    public float rotationSpeed = 3.0f; // カメラの回転速度（マウス速度）

    [Tooltip("ターゲット位置と回転に移動する際のスムーズさ（値が大きいほど速い）。")]
    public float smoothSpeed = 10.0f; // カメラの移動と回転のスムーズさ

    [Tooltip("垂直方向（上下）のカメラの角度制限。Xが最小値（下）、Yが最大値（上）。")]
    public Vector2 pitchMinMax = new Vector2(-40, 85); // 垂直方向のカメラ角度制限

    // --------------------------------------------------------------------------

    [Header("Collision Settings")]
    [Tooltip("カメラが衝突をチェックするレイヤー。壁や地面などを設定します。")]
    public LayerMask collisionLayers; // カメラが衝突をチェックするレイヤー（壁や地面など）

    [Tooltip("衝突が発生した際、カメラを押し戻す距離のオフセット。")]
    public float collisionOffset = 0.2f; // 衝突時にカメラが押し戻されるオフセット

    // --------------------------------------------------------------------------

    // カメラの現在の角度
    // プライベートフィールドのため、インスペクターに表示する必要がなければTooltipは不要です。
    private float _yaw = 0.0f;    // 左右の回転角度 (Y軸)
    private float _pitch = 0.0f; // 上下の回転角度 (X軸)

    // 固定ビューモード関連
    private bool _isFixedViewMode = false;
    private Vector3 _fixedTargetPosition;
    private Quaternion _fixedTargetRotation;
    private float _fixedViewSmoothSpeed;

    // カーソルロックの初期状態を保持
    private bool _cursorLockedInitially = false;

    void Start()
    {
        InitializeCursor();
        InitializeCameraAngles();
    }

    /// <summary>
    /// カーソルを初期状態でロックし、非表示にする
    /// </summary>
    private void InitializeCursor()
    {
        // カーソルをロックして非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _cursorLockedInitially = true; // 初期状態でロックしたことを記録
    }

    /// <summary>
    /// カメラの初期角度をターゲットの向きに合わせる
    /// </summary>
    private void InitializeCameraAngles()
    {
        if (target != null)
        {
            // 初期回転を設定 (ターゲットのY軸回転を取得)
            _yaw = target.eulerAngles.y;

            // ピッチ（上下角）は初期状態で水平（0度）またはターゲットの回転から取得
            // transform.rotationから計算する方が正確
            _pitch = transform.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360; // 負の角度に対応
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (_isFixedViewMode)
        {
            HandleFixedViewMode();
        }
        else
        {
            HandleTPSViewMode();
        }
    }

    /// <summary>
    /// 固定ビューモードでのカメラ制御ロジック
    /// </summary>
    private void HandleFixedViewMode()
    {
        // Time.unscaledDeltaTime を使用して、Time.timeScale = 0 の状態でもスムーズに動くようにする
        transform.position = Vector3.Lerp(transform.position, _fixedTargetPosition, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _fixedTargetRotation, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
    }

    /// <summary>
    /// 通常のTPS追従モードでのカメラ制御ロジック
    /// </summary>
    private void HandleTPSViewMode()
    {
        // 1. マウス入力による回転の計算
        Quaternion targetRotation = CalculateRotationFromInput();

        // 2. 目標位置の計算
        Vector3 targetPosition = CalculateTargetPosition(targetRotation);

        // 3. 衝突判定と位置の調整
        Vector3 finalPosition = ApplyCollisionCheck(targetPosition);

        // 4. カメラの位置と回転をLerpでスムーズに補間
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// マウス入力に基づいてカメラの目標回転を計算します。
    /// </summary>
    private Quaternion CalculateRotationFromInput()
    {
        _yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y); // 垂直角度を制限

        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    /// <summary>
    /// 目標回転と距離に基づいて、カメラの目標ワールド位置を計算します。
    /// </summary>
    private Vector3 CalculateTargetPosition(Quaternion rotation)
    {
        return target.position + Vector3.up * height - rotation * Vector3.forward * distance;
    }

    /// <summary>
    /// カメラ衝突判定を行い、目標位置を調整します。
    /// </summary>
    /// <param name="initialPosition">初期の目標位置。</param>
    /// <returns>衝突を考慮した最終的なカメラ位置。</returns>
    private Vector3 ApplyCollisionCheck(Vector3 initialPosition)
    {
        RaycastHit hit;
        Vector3 currentTargetPos = target.position + Vector3.up * height; // ターゲットの中心を含む位置

        if (Physics.Linecast(currentTargetPos, initialPosition, out hit, collisionLayers))
        {
            // 衝突があった場合、衝突点から少し手前にカメラを配置
            return hit.point + hit.normal * collisionOffset;
        }

        return initialPosition;
    }

    /// <summary>
    /// カメラを特定の固定位置と回転に設定し、そのビューにスムーズに移動します。
    /// Time.timeScaleが0でも動作します。
    /// </summary>
    /// <param name="position">カメラの目標ワールド位置。</param>
    /// <param name="rotation">カメラの目標ワールド回転。</param>
    /// <param name="smoothSpeedValue">目標位置・回転に到達するまでのスムーズ速度。</param>
    public void SetFixedCameraView(Vector3 position, Quaternion rotation, float smoothSpeedValue)
    {
        _isFixedViewMode = true;
        _fixedTargetPosition = position;
        _fixedTargetRotation = rotation;
        _fixedViewSmoothSpeed = smoothSpeedValue; // スムーズ速度を設定

        // 固定ビューモード中はカーソルを表示し、ロックを解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// カメラを通常のTPS追従モードに戻します。
    /// </summary>
    /// <param name="smoothSpeedValue">通常の追従モードに戻るまでのスムーズ速度。</param>
    public void ResetToTPSView(float smoothSpeedValue)
    {
        // TPSモードに戻る際の最初のフレームでは、スムーズに戻るために目標位置を設定
        _fixedViewSmoothSpeed = smoothSpeedValue;
        _isFixedViewMode = false; // 通常のTPSロジックがLateUpdateで再開

        // 通常のTPSモードに戻る際にカーソルを元の状態に戻す
        if (_cursorLockedInitially)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// プレイヤーの向きをカメラの水平方向の向きに合わせるためのメソッド
    /// </summary>
    public void RotatePlayerToCameraDirection()
    {
        if (target == null || _isFixedViewMode) return;

        // プレイヤーをY軸回転のみでカメラのY軸回転に合わせる
        Quaternion playerRotation = Quaternion.Euler(0, _yaw, 0);
        target.rotation = Quaternion.Slerp(target.rotation, playerRotation, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// カメラからRayを取得するメソッド (カメラの中心からワールドへ)
    /// </summary>
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

    /// <summary>
    /// カメラの中心点を取得するメソッド (GetCameraRayのRayの原点と同じ)
    /// </summary>
    public Vector3 GetCameraCenterPoint()
    {
        return GetCameraRay().origin;
    }
}