using UnityEngine;

public class Bit : MonoBehaviour
{
    private Transform target; // ターゲットとなる敵
    private float moveSpeed;  // ビットの移動速度

    // 上昇軌道のための追加変数
    private Vector3 initialLaunchPosition; // 最初にInstantiateされた位置 (PlayerのSpawnPoint)
    private Vector3 peakLaunchPosition;    // 上昇軌道のピーク地点
    private float currentLaunchTime;       // 上昇中の経過時間
    private float totalLaunchDuration;     // 上昇にかかる合計時間
    private float arcHeight;               // 上昇軌道のアーチの高さ

    private bool isLaunching = true;       // 上昇中かどうかのフラグ

    // Bitが生成された時にPlayerに衝突しないようにする処理 (例: Playerレイヤーとの衝突を無視)
    public LayerMask playerLayer; // Playerのレイヤー (Inspectorで設定)
    private LayerMask enemyLayerInternal; // Bitスクリプト内で使用するEnemyLayer

    public float destroyOnHitDelay = 0.1f; // 敵を破壊した後、ビットが消滅するまでの短い遅延

    void Awake()
    {
        // Bit自身のレイヤーとPlayerレイヤーの衝突を無視する
        // BitのGameObjectにBitレイヤーが設定されていることを前提とします。

        // PlayerLayerが有効なレイヤー範囲内にあるかチェック
        // (1 << playerLayer) の形でPhysics.IgnoreLayerCollisionに渡すため、
        // playerLayer自体はレイヤー番号（0-31）である必要があります。
        // InspectorでLayerMaskとして設定されている場合、その内部値は単一のレイヤーを示すことがあります。
        // もしLayerMaskが複数のレイヤーを含む場合は、正しいレイヤー番号を取得する必要があります。
        // 通常は単一のレイヤーを指定するので、ここではplayerLayer.valueからレイヤー番号を取得します。

        // playerLayerが単一のレイヤーとして設定されていることを想定
        int playerLayerNumber = -1;
        // LayerMaskからレイヤー番号を取得する
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & playerLayer.value) != 0)
            {
                playerLayerNumber = i;
                break;
            }
        }

        if (playerLayerNumber >= 0 && playerLayerNumber <= 31)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, playerLayerNumber, true);
        }
        else
        {
            Debug.LogWarning("Bit: 無効なPlayer Layerが設定されているため、衝突無視設定をスキップしました。Player Layerが0から31の範囲であることを確認してください。");
        }
    }

    /// <summary>
    /// ビットを初期化し、上昇軌道を開始する
    /// </summary>
    /// <param name="playerSpawnPos">ビットの初期ワールド座標</param>
    /// <param name="newTarget">ターゲットとなる敵</param>
    /// <param name="launchHeight">上昇高さ</param>
    /// <param name="launchDuration">上昇時間</param>
    /// <param name="speed">ターゲットへ向かう速度</param>
    /// <param name="arc">上昇のアーチの高さ</param>
    /// <param name="eLayer">敵のレイヤー</param>
    public void InitializeBit(Vector3 initialSpawnPos, Transform newTarget, float launchHeight, float launchDuration, float speed, float arc, LayerMask eLayer)
    {
        initialLaunchPosition = initialSpawnPos;
        // ピーク位置は、初期位置から真上に指定の高さまで
        peakLaunchPosition = initialLaunchPosition + Vector3.up * launchHeight;
        target = newTarget;
        moveSpeed = speed;
        totalLaunchDuration = launchDuration;
        arcHeight = arc;
        currentLaunchTime = 0f;
        isLaunching = true;
        enemyLayerInternal = eLayer; // EnemyLayerを内部変数に保存

        // Bitの初期位置をInitializeBitの引数で受け取った位置に設定
        transform.position = initialLaunchPosition;

        // Bitの向きをターゲットに向ける（初期段階で）
        if (target != null)
        {
            transform.LookAt(target.position);
        }
    }

    void Update()
    {
        if (target == null)
        {
            // ターゲットが消滅した場合など、一定時間後に自身を消滅させる
            Destroy(gameObject, 1.0f); // ターゲット喪失時はすぐに消滅
            return;
        }

        if (isLaunching)
        {
            currentLaunchTime += Time.deltaTime;
            float t = currentLaunchTime / totalLaunchDuration;

            if (t < 1.0f)
            {
                // 3点間のベジェ曲線 (放物線)
                Vector3 p0 = initialLaunchPosition;
                // ピーク位置は、初期位置とターゲットの中間点にアーチの高さを加える
                // これにより、ビットがプレイヤーの後ろから上昇し、ターゲットの方向へ弧を描く
                Vector3 midPoint = Vector3.Lerp(initialLaunchPosition, target.position, 0.5f);
                Vector3 p1 = new Vector3(midPoint.x, Mathf.Max(initialLaunchPosition.y, target.position.y) + arcHeight, midPoint.z);

                transform.position = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * target.position;

                // 上昇中はターゲットの方を向く
                transform.LookAt(target.position);
            }
            else
            {
                isLaunching = false; // 上昇完了
            }
        }
        else // 上昇完了後、ターゲットに向かって移動
        {
            // ターゲットに向かって移動
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            // ターゲットの方向を常に追従 (剣の先が敵に向かうように)
            transform.LookAt(target.position);
        }
    }

    // 敵に当たった場合の処理
    void OnTriggerEnter(Collider other)
    {
        // 衝突したのが敵レイヤーのオブジェクトかチェック
        // enemyLayerInternal を使用
        if (((1 << other.gameObject.layer) & enemyLayerInternal) != 0)
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = other.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                Debug.Log($"Bit hit and destroying {other.name}!");
                enemyHealth.TakeDamage(100); // 例: 100ダメージを与える（即死想定）
                Destroy(gameObject, destroyOnHitDelay); // ビット自身は少し遅れて消滅
            }
        }
    }
}