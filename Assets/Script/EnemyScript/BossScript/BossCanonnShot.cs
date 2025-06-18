using UnityEngine;
using System.Collections;

public class BossCanonnShot : MonoBehaviour
{
    // 弾の発射場所
    [SerializeField] private GameObject bulletPoint;
    // 弾
    [SerializeField] private GameObject bullet;
    // 残弾数
    public float bulletCount;
    // 初期弾数の保存
    private float bulletAs;
    // 弾の速さ
    public float Speed;
    // リロード
    bool reloading = false;
    // クールタイム
    bool coolTime = false;
    // EnemyDaedアニメーション
    public BossEnemyDead bossEnemyDead;

    void Start()
    {
        bossEnemyDead = GetComponent<BossEnemyDead>();

        bulletAs = bulletCount;
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (!bossEnemyDead.BossDead)
        {
            // Playerが範囲内に入ったとき
            if (collider.gameObject.tag == "Player" && !reloading && !coolTime)
            {
                // 弾の発射場所を取得
                Vector3 bulletPosition = bulletPoint.transform.position;
                // 弾のPrefabを作成
                GameObject newBullet = Instantiate(bullet, bulletPosition, this.gameObject.transform.rotation);
                // 弾の発射軸を取得（Z軸）
                Vector3 direction = newBullet.transform.forward;
                // 弾を発射（Z軸）
                newBullet.GetComponent<Rigidbody>().AddForce(direction * Speed, ForceMode.Impulse);
                // 残弾数を減らす
                bulletCount = bulletCount - 1;
                // リロード
                StartCoroutine(Shot());
            }
        }
    }

    private IEnumerator Shot()
    {
        if (bulletCount <= 0)
        {
            reloading = true;
            Debug.Log("リロード");

            // リロード時間
            yield return new WaitForSeconds(10);

            bulletCount = bulletAs;
            reloading = false;
        }
        else
        {
            Debug.Log("クールタイム");
            coolTime = true;
            yield return new WaitForSeconds(3);
            coolTime = false;
        }
    }
}
