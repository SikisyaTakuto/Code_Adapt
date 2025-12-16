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
    public Text distanceText;        // 距離を表示するTextコンポーネント

    // === 設定 ===
    [Header("Settings")]
    [Tooltip("マーカーを画面端に固定するためのマージン (ピクセル)")]
    public float edgeMargin = 50f;
    [Tooltip("マーカーが画面内にいるときに表示するスケーリング")]
    public float insideScreenScale = 1.0f;
    [Tooltip("マーカーが画面外にいるときに表示するスケーリング")]
    public float outsideScreenScale = 1.5f;

    // ★★★ テキスト位置調整用の設定を新規追加 ★★★
    [Header("Text Positioning")]
    [Tooltip("テキストをマーカーの中心からどれだけローカルY軸方向にオフセットするか")]
    public float textLocalOffset = 30f;

    private Transform targetTransform;
    private Camera mainCamera;

    // ★★★ 距離テキストのRectTransformの参照を新規追加 ★★★
    private RectTransform distanceRectTransform;

    void Start()
    {
        // メインカメラのキャッシュ
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("メインカメラがシーンに見つかりません。タグが 'MainCamera' であることを確認してください。");
            return;
        }

        // ★★★ TextコンポーネントからRectTransformを取得 ★★★
        if (distanceText != null)
        {
            distanceRectTransform = distanceText.GetComponent<RectTransform>();
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

        float finalRotationAngle = 0f; // マーカーの回転角度を保持

        if (isInsideScreen)
        {
            // 画面内にいる場合: マーカーをターゲットの真上に配置
            markerRect.position = screenPos;
            markerRect.localRotation = Quaternion.identity; // 回転なし
            markerRect.localScale = Vector3.one * insideScreenScale;

            // ★★★ テキストの位置と回転を画面内に合わせて設定 ★★★
            if (distanceRectTransform != null)
            {
                distanceRectTransform.anchoredPosition = new Vector2(0, textLocalOffset);
                distanceRectTransform.localRotation = Quaternion.identity; // 回転なし
            }
        }
        else
        {
            // 画面外にいる、または背後にある場合: マーカーを画面端にクランプ

            if (isBehind)
            {
                screenPos *= -1;
            }

            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Vector3 relativePos = screenPos - screenCenter;

            float angle = Mathf.Atan2(relativePos.y, relativePos.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float angleDeg = angle * Mathf.Rad2Deg; // 角度を計算

            float halfWidth = Screen.width / 2 - edgeMargin;
            float halfHeight = Screen.height / 2 - edgeMargin;

            float m = Mathf.Min(Mathf.Abs(halfWidth / cos), Mathf.Abs(halfHeight / sin));

            Vector3 clampedPos = new Vector3(cos, sin, 0) * m;
            markerRect.position = clampedPos + screenCenter;

            // 矢印をターゲットの方向に向ける (マーカーの回転ロジックは維持)
            finalRotationAngle = angleDeg - 90;
            markerRect.localRotation = Quaternion.Euler(0, 0, finalRotationAngle);
            markerRect.localScale = Vector3.one * outsideScreenScale;

            // ★★★ テキストの回転相殺と位置調整 ★★★
            if (distanceRectTransform != null)
            {
                // 親 (markerRect) の回転をマイナスで打ち消し、テキストを常に水平に保つ
                distanceRectTransform.localRotation = Quaternion.Euler(0, 0, -finalRotationAngle);

                // 親（マーカー）の中心からローカルY軸方向へオフセットして配置
                distanceRectTransform.anchoredPosition = new Vector2(0, textLocalOffset);
            }
        }

        // 距離を計算してUIを更新
        UpdateDistanceUI();
    }

    // (SetTargetメソッドとUpdateDistanceUIメソッドは変更なし)
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
        if (targetTransform != null && distanceText != null && mainCamera != null)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, targetTransform.position);
            distanceText.text = $"{Mathf.CeilToInt(distance)}m";
        }
    }
}