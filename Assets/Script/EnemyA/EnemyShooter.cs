using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    // 弾のプレハブをInspectorから設定するための変数
    public GameObject bulletPrefab;

    // プレイヤーのTransform（位置情報など）
    public Transform playerTarget;

    // 何秒ごとに弾を発射するか
    public float fireRate = 1.0f;

    // 次に発射するまでの時間
    private float nextFireTime;

    void Start()
    {
        // プレイヤーオブジェクトを検索してTargetに設定する（タグが"Player"と仮定）
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        // 初回発射までの時間を設定
        nextFireTime = Time.time + fireRate;
    }

    void Update()
    {
        // プレイヤーが存在し、発射時間になったら
        if (playerTarget != null && Time.time >= nextFireTime)
        {
            // プレイヤーの方を向く
            LookAtPlayer();
            // 弾を発射する
            ShootBullet();

            // 次の発射時間を更新
            nextFireTime = Time.time + fireRate;
        }
    }

    // プレイヤーの方を向く処理
    void LookAtPlayer()
    {
        // プレイヤーの方向を計算
        Vector3 direction = playerTarget.position - transform.position;
        // Y軸の回転のみを考慮（敵が地面に立っている場合）
        direction.y = 0;

        // プレイヤーの方向を向くクォータニオンを計算し、敵の回転に適用
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }

    // 弾を発射する処理
    void ShootBullet()
    {
        // 敵の位置と現在の回転（プレイヤーを向いている）で弾を生成
        // transform.position + transform.forward * 1f のように、少し前方で生成すると敵の体に埋まるのを防げます。
        GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);

        // 弾の移動処理は、アタッチされているBullet.csに任せる
    }

    public void ShootBulletByAnimationEvent()
    {
        // ... (銃口の位置から弾をInstantiateする処理) ...
    }
}