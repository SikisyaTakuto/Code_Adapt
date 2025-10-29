using UnityEngine;

/// <summary>
/// ミニマップ用のカメラを特定のターゲット（プレイヤー）に追従させる。
/// </summary>
public class MinimapCameraFollow : MonoBehaviour
{
    // 追従させるターゲットのTransform (InspectorでPlayerオブジェクトを割り当てる)
    [Header("ターゲット")]
    public Transform target;

    // カメラの高さ（Y軸のオフセット）
    [Header("カメラの高さ")]
    public float heightOffset = 20f;

    // カメラが追従時にプレイヤーの回転を無視するかどうか
    [Header("プレイヤーの回転を反映するか")]
    public bool followRotation = false;

    // カメラの初期回転（通常、真上から見るために設定済み）
    // private Quaternion initialRotation;


    void Start()
    {
        // エディタでターゲットが設定されているか確認
        if (target == null)
        {
            Debug.LogError("ターゲット（Player）が設定されていません。Inspectorで設定してください。", this);
            enabled = false;
            return;
        }

        // カメラの初期回転（必要に応じて）
        // initialRotation = transform.rotation;
    }

    // カメラの追従は、動きが完了した後に実行されるLateUpdateで行うのが一般的です。
    void LateUpdate()
    {
        // ターゲットがなければ処理しない
        if (target == null) return;

        // 1. 位置の追従（X, Z座標のみ）
        Vector3 newPosition = target.position;
        newPosition.y = target.position.y + heightOffset; // プレイヤーの位置+設定された高さ

        transform.position = newPosition;

        // 2. 回転の追従（オプション）
        if (followRotation)
        {
            // プレイヤーのY軸回転のみをコピー
            Quaternion targetRotation = Quaternion.Euler(
                transform.eulerAngles.x, // カメラの現在のX回転を維持（真下を向いたまま）
                target.eulerAngles.y,    // プレイヤーのY回転をコピー
                transform.eulerAngles.z  // カメラの現在のZ回転を維持
            );
            transform.rotation = targetRotation;
        }
        // else
        // {
        //     // プレイヤーの回転に関わらず、カメラは固定された角度を維持
        //     transform.rotation = initialRotation;
        // }
    }
}