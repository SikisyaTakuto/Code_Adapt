using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform player;               // プレイヤーの位置（カメラの回転中心）
    public float mouseSensitivity = 2f;   // マウス感度
    public float distanceFromPlayer = 5f; // プレイヤーからカメラまでの距離
    public float verticalAngleMin = -30f; // カメラ上下回転の最小角度（下方向制限）
    public float verticalAngleMax = 60f;  // カメラ上下回転の最大角度（上方向制限）

    public float lockOnRange = 15f;       // ロックオン対象検出距離
    public string enemyTag = "Enemy";     // 敵のタグ名

    private float rotationX = 0f;         // 垂直回転角度（上下）
    private float rotationY = 0f;         // 水平回転角度（左右）

    private Transform lockedTarget = null;    // 現在ロックオン中の敵のTransform
    private int lockOnIndex = -1;              // ロックオン中の敵のインデックス
    private Transform[] targets;               // ロックオン可能な敵一覧

    void Start()
    {
        // カメラの初期回転角度を取得
        Vector3 angles = transform.eulerAngles;
        rotationY = angles.y;
        rotationX = angles.x;
    }

    void Update()
    {
        if (lockedTarget != null)
        {
            // ロックオン中の敵がカメラから遮られていないか確認
            Vector3 targetHeadPos = lockedTarget.position + Vector3.up * 1.5f; // 敵の頭あたりを狙う
            Vector3 camToTarget = targetHeadPos - transform.position;

            RaycastHit hit;
            // カメラ位置から敵方向へRaycast（遮蔽物判定）
            if (Physics.Raycast(transform.position, camToTarget.normalized, out hit, lockOnRange))
            {
                if (hit.transform != lockedTarget)
                {
                    // 敵以外のオブジェクトに遮られている場合はロックオン解除
                    lockedTarget = null;
                    lockOnIndex = -1;
                }
            }
            else
            {
                // Raycastで敵に当たらなかった場合もロック解除
                lockedTarget = null;
                lockOnIndex = -1;
            }
        }

        if (lockedTarget == null)
        {
            // ロックオンしていなければマウス操作で視点回転＆カメラ位置更新
            LookAround();
            UpdateCameraPosition();
        }
        else
        {
            // ロックオン中は敵を追従してカメラ位置・向きを更新
            LockOnLook();
        }

        // 右クリックでロックオン切り替え
        if (Input.GetMouseButtonDown(1))
        {
            LockOnNextTarget();
        }
    }

    // マウス入力で自由に視点回転させる処理
    void LookAround()
    {
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity; // 水平方向回転
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity; // 垂直方向回転（上下反転注意）
        rotationX = Mathf.Clamp(rotationX, verticalAngleMin, verticalAngleMax); // 回転角度制限
    }

    // プレイヤーを中心にカメラ位置を更新する処理
    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distanceFromPlayer); // プレイヤーの後ろに配置
        transform.position = player.position + offset;
        transform.LookAt(player.position + Vector3.up * 1.5f); // プレイヤーの頭付近を注視
    }

    // ロックオン対象の敵を検出し、カメラから遮られていない敵だけを抽出する処理
    void UpdateTargets()
    {
        // シーン内の全ての敵を取得
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        var closeEnemies = new System.Collections.Generic.List<Transform>();
        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(player.position, e.transform.position);

            if (dist <= lockOnRange) // ロックオン範囲内なら
            {
                Vector3 targetHeadPos = e.transform.position + Vector3.up * 1.5f; // 敵の頭付近
                Vector3 camToTarget = targetHeadPos - transform.position;

                RaycastHit hit;
                // カメラから敵の頭へRaycastを飛ばし、遮蔽物がないかチェック
                if (Physics.Raycast(transform.position, camToTarget.normalized, out hit, lockOnRange))
                {
                    if (hit.transform == e.transform)
                    {
                        Debug.DrawLine(transform.position, targetHeadPos, Color.green, 0.1f); // 成功：緑
                        closeEnemies.Add(e.transform);
                    }
                    else
                    {
                        Debug.DrawLine(transform.position, hit.point, Color.yellow, 0.1f); // 遮られてる：黄色
                    }
                }
                else
                {
                    Debug.DrawRay(transform.position, camToTarget.normalized * lockOnRange, Color.gray, 0.1f); // 当たらない：灰色
                }
            }
        }

        // 距離が近い順にソート
        closeEnemies.Sort((a, b) =>
            Vector3.Distance(player.position, a.position).CompareTo(
            Vector3.Distance(player.position, b.position)));

        targets = closeEnemies.ToArray();
    }

    // ロックオン可能な敵の中で次の敵にロックオン切り替えを行う処理
    void LockOnNextTarget()
    {
        UpdateTargets();

        if (targets.Length == 0)
        {
            // ロックオン可能な敵がいなければロック解除
            lockedTarget = null;
            lockOnIndex = -1;
            return;
        }

        lockOnIndex++;
        if (lockOnIndex >= targets.Length)
        {
            // すべての敵を切り替えたらロック解除
            lockedTarget = null;
            lockOnIndex = -1;
            return;
        }

        // ロックオン対象を更新
        lockedTarget = targets[lockOnIndex];

        // 【ここからロックオン開始時のコメントと処理】
        Debug.Log($"ロックオン対象を変更しました: {lockedTarget.name}");  // コンソールにロックオン対象の名前を表示
                                                            // ここでロックオン時のエフェクト再生やUI更新なども追加可能
    }

    // ロックオン中の敵にカメラを向ける処理
    void LockOnLook()
    {
        if (lockedTarget == null) return;

        // 敵の位置を中心に、プレイヤーとの距離を保つようにカメラを配置
        Vector3 targetHeadPos = lockedTarget.position + Vector3.up * 1.5f;
        Vector3 toEnemy = (targetHeadPos - player.position).normalized;

        // プレイヤーの背面ではなく、敵を中心にした角度から追尾するようにカメラを配置
        Vector3 cameraPos = player.position - toEnemy * distanceFromPlayer + Vector3.up * 2f;
        transform.position = cameraPos;

        // カメラは常に敵を注視
        transform.LookAt(targetHeadPos);

        // プレイヤーも敵の方向に回転させる
        Vector3 lookDir = (lockedTarget.position - player.position).normalized;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(lookDir), 0.2f);
    }
}
