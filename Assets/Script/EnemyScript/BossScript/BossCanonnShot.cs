using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class BossCanonnShot : MonoBehaviour
{
    // Playerの方向に向く変数
    public Transform target;
    // 弾の発射場所
    [SerializeField] private GameObject bulletPoint;
    // 弾
    [SerializeField] private GameObject bullet;
    // 大砲
    [SerializeField] private GameObject taiho;
    // 残弾数
    public float bulletCount;
    // 初期弾数
    private float bulletAs;
    // 弾の速さ
    public float Speed;
    // リロード
    bool reloading = false;
    // クールタイム
    bool coolTime = false;
    // 近接攻撃
    bool meleeAttack = false;
    // EnemyDaedアニメーション
    public BossEnemyDead bossEnemyDead;

    public BossCannonMove bossCannonMove;

    void Start()
    {
        // 初期弾数の保存
        bulletAs = bulletCount;
    }

    void Update()
    {
        Vector3 targetPos = target.position;

        // 大砲がPlayerの方向に向く
        taiho.transform.LookAt(targetPos);

        if (!bossEnemyDead.BossDead)
        {
            // 射撃
            Shot();

        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        // Playerが範囲内に入ったとき
        if (collider.gameObject.tag == "Player")
        {
            meleeAttack = true;
        }
    }

    public void OnLoseObject(Collider collider)
    {
        // Playerが範囲外に出たとき
        if (collider.gameObject.tag == "Player")
        {
            meleeAttack = false;
        }
    }

    public void Shot()
    {
        // Playerが範囲外にいるとき
        if (!reloading && !coolTime && !meleeAttack)
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
            StartCoroutine(ShotTime());
        }
    }

    private IEnumerator ShotTime()
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
            coolTime = true;
            Debug.Log("クールタイム");

            // クールタイム
            yield return new WaitForSeconds(3);

            coolTime = false;
        }
    }
}
