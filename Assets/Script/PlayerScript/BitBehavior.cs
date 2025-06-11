using UnityEngine;

public class BitBehavior : MonoBehaviour
{
    // ビットの状態を表す列挙型
    // Idle：待機中、Orbiting：プレイヤーの周囲を回転中、
    // Launching：敵に向かって移動中、Returning：プレイヤーに戻る途中
    enum BitState { Idle, Orbiting, Launching, Returning }
    BitState state = BitState.Idle;

    // プレイヤーのTransform
    private Transform player;

    // ビットの待機位置（プレイヤーの背後など）
    private Transform standbyPosition;

    // プレイヤーのコントローラー参照（管理や発射指令を受け取る）
    private PlayerBitController controller;

    // ビットの移動速度（Launch中に使用）
    private float speed = 15f;

    // 発射状態の持続時間（秒）
    private float forwardDuration = 2f;

    // プレイヤーに戻るのにかかる時間（ベジェ曲線の補間時間）
    private float returnDuration = 1.5f;

    // Launch中の残り時間を管理するタイマー
    private float timer;

    // 発射方向（ターゲットの方向ベクトルを保持）
    private Vector3 launchDirection;

    // 戻る際の開始位置（ベジェ曲線の始点）
    private Vector3 returnStartPos;

    // 戻る際の中間制御点（ベジェ曲線の曲がり具合を制御）
    private Vector3 returnControlPoint;

    // ベジェ曲線補間の進行度（0～1）
    private float returnProgress;

    // ランダムな軌道でプレイヤー周囲を回る時間（秒）
    private float orbitDuration = 1.5f;

    // 残りの周回時間
    private float orbitTimer;

    // プレイヤーの周囲を回る半径
    private float orbitRadius = 5f;

    // ビットがプレイヤーよりどれだけ下に行けるか（負の値で下方向に制限）
    private float minOrbitHeightOffset = 1.5f;

    // 周回に使う回転軸（ランダムに決定される）
    private Vector3 orbitAxis;

    // 現在の回転角度（度数）
    private float orbitAngle;

    // 回転後に発射する対象の敵
    private Transform targetAfterOrbit;

    // 毎フレーム呼ばれる更新処理（状態に応じて処理分岐）
    void Update()
    {
        switch (state)
        {
            case BitState.Idle:
                // 待機位置へ滑らかに追従
                FollowStandbyPosition();
                break;

            case BitState.Orbiting:
                // プレイヤーの周囲をランダムな軌道で回転
                OrbitAroundPlayerRandom();
                break;

            case BitState.Launching:
                // 発射方向に直進
                // ターゲットが消えていたら戻る
                if (targetAfterOrbit == null)
                {
                    StartReturning();
                    break;
                }

                // 発射方向に直進
                transform.position += launchDirection * speed * Time.deltaTime;

                // 発射時間が終了したら戻り状態へ
                timer -= Time.deltaTime;
                if (timer <= 0f) StartReturning();
                break;

            case BitState.Returning:
                // ベジェ曲線でプレイヤーに戻る
                ReturnToPlayer();
                break;
        }
    }

    // ビットの初期化（プレイヤーと待機位置を登録）
    public void Initialize(PlayerBitController controller, Transform standby)
    {
        this.controller = controller;
        this.standbyPosition = standby;
        this.player = controller.transform;

        // 初期位置を待機位置に設定し、状態をIdleに
        transform.position = standby.position;
        state = BitState.Idle;
    }

    // ビットが待機状態かどうかを返す（外部から発射可能か判定するため）
    public bool IsIdle() => state == BitState.Idle;

    // ビットを発射（まず周回開始 → 一定時間後に敵に向かって射出）
    public void Launch()
    {
        if (state != BitState.Idle) return;

        // 発射先の敵を検索
        targetAfterOrbit = FindNearestEnemy();

        if (targetAfterOrbit != null)
        {
            // ランダム軌道の初期化
            orbitTimer = orbitDuration;
            orbitAngle = Random.Range(0f, 360f);        // ランダムな角度から開始
            orbitAxis = Random.onUnitSphere;            // ランダムな回転軸

            // 状態を周回モードに
            state = BitState.Orbiting;
        }
    }

    // プレイヤーの周囲をランダムな軌道で回転させる処理
    private void OrbitAroundPlayerRandom()
    {
        if (player == null) return;

        // 時間をカウントダウン
        orbitTimer -= Time.deltaTime;

        // 回転角度を毎フレーム加算（回転スピード調整）
        orbitAngle += 180f * Time.deltaTime;

        // 指定軸で回転を生成し、プレイヤー中心の位置を計算
        Quaternion rotation = Quaternion.AngleAxis(orbitAngle, orbitAxis);
        Vector3 offset = rotation * (Vector3.forward * orbitRadius);

        // プレイヤーの位置に対してオフセット位置に配置
        transform.position = player.position + offset;

        // 高さ制限：プレイヤーより下がりすぎないようにする
        Vector3 pos = transform.position;
        float minY = player.position.y + minOrbitHeightOffset;
        if (pos.y < minY)
        {
            pos.y = minY;
            transform.position = pos;
        }

        // 周回が終了したら発射状態へ遷移
        if (orbitTimer <= 0f && targetAfterOrbit != null)
        {
            launchDirection = (targetAfterOrbit.position - transform.position).normalized;
            timer = forwardDuration;
            state = BitState.Launching;
        }
    }

    // 最も近い敵のTransformを検索して返す
    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    // 発射が終わったらプレイヤーに戻る処理を開始
    private void StartReturning()
    {
        state = BitState.Returning;

        returnStartPos = transform.position;

        // プレイヤーの背中方向（forwardの反対）へ向けたオフセット
        Vector3 playerBack = -player.forward;
        Vector3 sideOffset = Vector3.Cross(player.up, playerBack).normalized * 2f; // 横に回り込むオフセット（右または左）

        // 50%の確率で左か右に避ける
        if (Random.value < 0.5f) sideOffset = -sideOffset;

        // 制御点はプレイヤーの背中方向 + 横 + 少し上
        Vector3 mid = (returnStartPos + standbyPosition.position) * 0.5f;
        returnControlPoint = mid + playerBack * 10f + sideOffset + Vector3.up * 10f;

        returnProgress = 0f;
    }

    // ベジェ曲線を使って待機位置へ戻る
    private void ReturnToPlayer()
    {
        // 時間に応じて補間値を進行
        returnProgress += Time.deltaTime / returnDuration;

        // 最終到達したら完全に戻す
        if (returnProgress >= 1f)
        {
            transform.position = standbyPosition.position;
            state = BitState.Idle;
            return;
        }

        // 2次ベジェ曲線を使って滑らかに移動
        float t = returnProgress;
        Vector3 curvedPos = Mathf.Pow(1 - t, 2) * returnStartPos +
                            2 * (1 - t) * t * returnControlPoint +
                            Mathf.Pow(t, 2) * standbyPosition.position;

        transform.position = curvedPos;
    }

    // 敵と接触したときの処理
    private void OnTriggerEnter(Collider other)
    {
        // Launch中に敵と衝突したら
        if (state == BitState.Launching && other.CompareTag("Enemy"))
        {
            // 敵を削除（例：Destroy）
            Destroy(other.gameObject);

            // すぐに戻る処理へ遷移
            StartReturning();
        }
    }

    // アイドル状態中にプレイヤーの待機位置へ滑らかに移動
    private void FollowStandbyPosition()
    {
        float followSpeed = 5f;

        // 線形補間で位置を滑らかに追従
        transform.position = Vector3.Lerp(transform.position, standbyPosition.position, followSpeed);
    }
}
