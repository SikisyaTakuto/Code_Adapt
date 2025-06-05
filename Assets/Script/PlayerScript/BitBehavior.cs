using UnityEngine;

public class BitBehavior : MonoBehaviour
{
    // ビットの移動速度
    public float speed = 10f;
    // ビットの回転速度（度/秒）
    public float rotationSpeed = 720f;
    // ビットが攻撃可能な範囲
    public float attackRange = 15f;
    // 攻撃力
    public int damage = 20;
    // ビットの寿命（秒）
    public float lifeTime = 10f;

    // 上昇状態の継続時間
    public float ascendDuration = 2.0f;
    // 上昇速度
    public float ascendSpeed = 20f;
    // 帰還時の待機位置の上の高さ
    public float returnAscendHeight = 10f;

    // 所有者プレイヤーのTransform参照
    private Transform ownerTransform;
    // 待機位置のTransform参照
    private Transform idlePosition;
    // Rigidbodyコンポーネントの参照（物理制御用）
    private Rigidbody rb;
    // 所有者のPlayerBitController参照
    private PlayerBitController owner;

    // ビットの状態を管理する列挙型
    private enum State
    {
        Idle,               // 待機中
        Ascending,          // 上昇中
        Seeking,            // 敵を追尾中
        ReturningAscending,  // 帰還のため上昇中
        ReturningToIdle     // 待機位置へ戻り中
    }

    // 現在の状態
    private State currentState = State.Idle;

    // 追尾対象の敵のTransform
    private Transform targetEnemy;
    // 状態ごとの経過時間計測用タイマー
    private float stateTimer;
    // 帰還時に一旦移動する待機位置の上の座標
    private Vector3 returnTargetAboveIdle;

    // 初期化メソッド（PlayerBitControllerから呼ばれる）
    public void Initialize(PlayerBitController ownerController, Transform idlePos)
    {
        owner = ownerController;                      // 所有者コントローラー設定
        ownerTransform = owner.transform;             // 所有者のTransform取得
        idlePosition = idlePos;                        // 待機位置設定
        rb = GetComponent<Rigidbody>();               // Rigidbodyコンポーネント取得

        // 待機位置に初期配置・回転
        transform.position = idlePosition.position;
        transform.rotation = idlePosition.rotation;

        currentState = State.Idle;                     // 状態を待機に設定
        stateTimer = 0f;                               // タイマーリセット
    }

    // ビットが待機状態かどうかを返すメソッド
    public bool IsIdle()
    {
        return currentState == State.Idle;
    }

    // ビットを射出（発射）するメソッド
    public void Launch()
    {
        // 待機状態のみ射出可能
        if (currentState == State.Idle)
        {
            currentState = State.Ascending;            // 状態を上昇中に変更
            stateTimer = 0f;                           // タイマーリセット
        }
    }

    // 毎フレーム呼ばれる更新処理
    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                // 待機中は常に待機位置に固定し、回転も固定
                transform.position = idlePosition.position;
                transform.rotation = idlePosition.rotation;
                rb.linearVelocity = Vector3.zero;              // 速度を0に
                break;

            case State.Ascending:
                // 上昇時間を加算
                stateTimer += Time.deltaTime;
                // Rigidbodyに上方向の速度をセット（物理移動）
                rb.linearVelocity = Vector3.up * ascendSpeed;

                // 上昇時間が経過したら追尾状態へ移行
                if (stateTimer >= ascendDuration)
                {
                    currentState = State.Seeking;
                    stateTimer = 0f;
                }
                break;

            case State.Seeking:
                stateTimer += Time.deltaTime;

                if (stateTimer > lifeTime)
                {
                    StartReturn();
                    break;
                }

                if (currentState != State.Seeking) break;  // 状態変更後は追尾処理スキップ

                FindClosestEnemy();

                if (targetEnemy != null)
                {
                    Vector3 dir = (targetEnemy.position - transform.position).normalized;
                    rb.linearVelocity = dir * speed;
                }
                else
                {
                    rb.linearVelocity = transform.forward * (speed * 0.5f);
                }
                break;

            case State.ReturningAscending:
                // 帰還のため一旦待機位置の上空へ向かう
                Vector3 toAbove = returnTargetAboveIdle - transform.position;
                
                // 十分に近ければ次の状態へ
                if (toAbove.sqrMagnitude < 0.05f)
                {
                    currentState = State.ReturningToIdle;
                }
                else
                {
                    // 上空への方向に上昇速度で移動
                    rb.linearVelocity = toAbove.normalized * ascendSpeed;
                }
                break;

            case State.ReturningToIdle:
                // 待機位置に戻るための方向を計算
                Vector3 returnDir = idlePosition.position - transform.position;

                // 待機位置に十分近ければ待機状態へ復帰
                if (returnDir.sqrMagnitude < 0.05f)
                {
                    currentState = State.Idle;
                    rb.linearVelocity = Vector3.zero;              // 速度リセット
                    transform.position = idlePosition.position;
                    transform.rotation = idlePosition.rotation;
                    targetEnemy = null;                       // 追尾対象リセット
                }
                else
                {
                    // 待機位置へ速度セット
                    rb.linearVelocity = returnDir.normalized * speed;
                }
                break;
        }
    }

    // 最も近い敵を探してtargetEnemyに設定するメソッド
    void FindClosestEnemy()
    {
        float closestDist = attackRange;  // 攻撃可能範囲内で探索
        targetEnemy = null;

        // "Enemy"タグのついたゲームオブジェクトすべてをチェック
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            // 自身と敵との距離を計算
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            
            // 最短距離を更新し、対象の敵を記憶
            if (dist < closestDist)
            {
                closestDist = dist;
                targetEnemy = enemy.transform;
            }
        }
    }

    // 他オブジェクトとの衝突検知時に呼ばれる
    void OnTriggerEnter(Collider other)
    {
        // 追尾状態でない場合は何もしない
        if (currentState != State.Seeking) return;

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            StartReturn();

            // 速度を即座に切り替え(念のため)
            rb.linearVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;

            return;  // 念のため早期リターン
        }
        else if (!other.CompareTag("Player"))
        {
            StartReturn();

            rb.linearVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;

            return;
        }
    }

    // 帰還動作を開始するメソッド
    void StartReturn()
    {
        targetEnemy = null;   // 追尾対象リセット
                              // 待機位置の上空へ戻るための座標を設定
        returnTargetAboveIdle = idlePosition.position + Vector3.up * returnAscendHeight;
        currentState = State.ReturningAscending;   // 状態を帰還上昇に変更
        stateTimer = 0f;                            // タイマーリセット

        // Rigidbodyの速度を即座に帰還上昇方向へ切り替え（これを追加）
        Vector3 toAbove = returnTargetAboveIdle - transform.position;
        rb.linearVelocity = toAbove.normalized * ascendSpeed;
    }
}
