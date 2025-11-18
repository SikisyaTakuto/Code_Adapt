using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロックオン対象の敵のワールド座標を追跡し、UI Imageを敵の画面位置に表示・追従させる。
/// </summary>
public class EnemyLockOnUI : MonoBehaviour
{
    // ロックオンUIとして機能するImageコンポーネント
    private Image lockOnImage;

    // 追跡するターゲット (敵のTransform)
    private Transform target;

    // 敵のワールド座標を画面に変換するためのCamera
    private Camera mainCamera;

    // 画面外に出ていないかを判定するマージン
    private const float ScreenMargin = 50f;

    void Awake()
    {
        lockOnImage = GetComponent<Image>();
        mainCamera = Camera.main;

        if (lockOnImage == null || mainCamera == null)
        {
            Debug.LogError("EnemyLockOnUI: 必要なコンポーネントが見つかりません。");
            enabled = false;
        }

        lockOnImage.enabled = false;
    }

    /// <summary>
    /// 追跡するターゲットを設定し、UIをアクティブにする。
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        lockOnImage.enabled = true; // UIを表示開始
    }

    void Update()
    {
        if (target == null)
        {
            // ターゲットがいなくなったらUIを非表示にして破棄
            Destroy(gameObject);
            return;
        }

        if (mainCamera == null) return;

        // ターゲットのワールド座標をスクリーン座標に変換
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

        // Z座標（カメラからの距離）が負の場合、ターゲットはカメラの後ろにある
        if (screenPos.z < 0)
        {
            lockOnImage.enabled = false;
            return;
        }

        // ターゲットが画面外にいるかチェック
        Rect screenRect = new Rect(ScreenMargin, ScreenMargin, Screen.width - ScreenMargin * 2, Screen.height - ScreenMargin * 2);

        if (!screenRect.Contains(screenPos))
        {
            lockOnImage.enabled = false;
            return;
        }

        // 画面内にいる場合はUIを表示し、位置を更新
        lockOnImage.enabled = true;

        // UIの位置をスクリーン座標に設定
        transform.position = screenPos;
    }
}