using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ワールド内のターゲットの位置に基づいて、UIマーカーの位置と回転を制御します。
/// </summary>
public class TargetMarkerUI : MonoBehaviour
{
    // === 依存関係 ===
    [Header("Dependencies")]
    public RectTransform markerRect; // マーカーのRectTransform (矢印画像など)
    public Text distanceText;       // 距離を表示するTextコンポーネント

    // === 設定 ===
    [Header("Settings")]
    [Tooltip("マーカーを画面端に固定するためのマージン (ピクセル)")]
    public float edgeMargin = 50f;
    [Tooltip("マーカーが画面内にいるときに表示するスケーリング")]
    public float insideScreenScale = 1.0f;
    [Tooltip("マーカーが画面外にいるときに表示するスケーリング")]
    public float outsideScreenScale = 1.5f;

    private Transform targetTransform;
    private Camera mainCamera;

    void Start()
    {
        // メインカメラのキャッシュ
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("メインカメラがシーンに見つかりません。タグが 'MainCamera' であることを確認してください。");
            return;
        }

        // TargetManagerから現在の目標を取得
        TargetManager.Instance.OnTargetChanged += SetTarget;
        SetTarget(TargetManager.Instance.CurrentTarget);
    }

    void Update()
    {
        if (targetTransform == null || mainCamera == null)
        {
            // 目標がない場合は非表示
            markerRect.gameObject.SetActive(false);
            return;
        }

        markerRect.gameObject.SetActive(true);

        // ターゲットの位置を画面座標に変換
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetTransform.position);

        // ターゲットがカメラの前方にあるかチェック
        bool isBehind = screenPos.z < 0;

        // ターゲットが画面の境界内にあるかチェック
        bool isInsideScreen = screenPos.x > edgeMargin &&
                              screenPos.x < Screen.width - edgeMargin &&
                              screenPos.y > edgeMargin &&
                              screenPos.y < Screen.height - edgeMargin &&
                              !isBehind;

        if (isInsideScreen)
        {
            // 画面内にいる場合: マーカーをターゲットの真上に配置
            markerRect.position = screenPos;
            markerRect.localRotation = Quaternion.identity; // 回転なし
            markerRect.localScale = Vector3.one * insideScreenScale;
        }
        else
        {
            // 画面外にいる、または背後にある場合: マーカーを画面端にクランプ

            // 背後にある場合、画面中央を基準に反転させる（3D座標を2D平面に引き戻す）
            if (isBehind)
            {
                screenPos *= -1;
            }

            // 画面中心を原点 (0, 0) として座標を扱う
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            screenPos -= screenCenter;

            // 画面のアスペクト比を考慮した傾き計算
            float angle = Mathf.Atan2(screenPos.y, screenPos.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // マーカーを画面端に制限する矩形 (画面中央基準)
            float halfWidth = Screen.width / 2 - edgeMargin;
            float halfHeight = Screen.height / 2 - edgeMargin;

            // 画面端へのクランプ計算
            float m = Mathf.Min(Mathf.Abs(halfWidth / cos), Mathf.Abs(halfHeight / sin));

            // 新しい画面上の位置
            Vector3 clampedPos = new Vector3(cos, sin, 0) * m;
            markerRect.position = clampedPos + screenCenter;

            // 矢印をターゲットの方向に向ける
            markerRect.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90);
            markerRect.localScale = Vector3.one * outsideScreenScale;
        }

        // 距離を計算してUIを更新
        UpdateDistanceUI();
    }

    /// <summary>
    /// 目標Transformを設定します。
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        if (newTarget == null)
        {
            markerRect.gameObject.SetActive(false);
        }
        else
        {
            markerRect.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 距離UIを更新します。
    /// </summary>
    private void UpdateDistanceUI()
    {
        if (targetTransform != null && distanceText != null)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, targetTransform.position);
            distanceText.text = $"{Mathf.CeilToInt(distance)}m";
        }
    }
}