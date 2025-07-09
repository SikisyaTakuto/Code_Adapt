using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.InputSettings;

public class BossShotBullet : MonoBehaviour
{
    // 弾の発射位置
    [SerializeField] private GameObject bulletPoint;
    // 弾のPrefab
    [SerializeField] private GameObject bullet;
    // マガジンの弾数
    [SerializeField] private float bulletCount = 20f;
    // 弾の発射間隔
    [SerializeField] private float shotInterval = 1.5f;
    // リロード時間
    [SerializeField] private float reloadTime = 3.0f;
    // 弾速
    [SerializeField] private float speed = 20f;
    // 撃ち始めるまでの待機時間
    [SerializeField] private float shootingStartDelay = 1.0f;
    // Inspectorでセットする用
    [SerializeField] private LookBossCanonn lookScript; 

    // 敵の死亡状態
    public BossEnemyDead bossEnemyDaed;

    private float initialBulletCount;
    private bool isPlayerInRange = false;
    private bool isShooting = false;
    private bool reloading = false;
    private Transform targetPlayer;

    void Start()
    {
        // 初期マガジンの弾数
        initialBulletCount = bulletCount;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !bossEnemyDaed.BossDead)
        {
            isPlayerInRange = true;
            targetPlayer = other.transform;

            if (!isShooting)
            {
                StartCoroutine(StartShootingAfterDelay());
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private IEnumerator StartShootingAfterDelay()
    {
        isShooting = true;
        yield return new WaitForSeconds(shootingStartDelay);

        // 範囲外にPlayerがいて、死んでいなければ射撃開始
        if (!isPlayerInRange && !bossEnemyDaed.BossDead)
        {
            StartCoroutine(ShootingLoop());
        }
        else
        {
            // 撃つ必要がなくなった場合
            isShooting = false; 
        }
    }

    private IEnumerator ShootingLoop()
    {
        isShooting = true;

        while (!isPlayerInRange && !bossEnemyDaed.BossDead)
        {
            if (bulletCount > 0 && !reloading)
            {
                // 弾を撃つ前にLookを止める
                DisableLookTemporarily();
                // 発射の直前に少し待つ
                yield return new WaitForSeconds(0.5f);

                ShootAtPlayer();
                bulletCount--;
            }
            else if (!reloading)
            {
                yield return StartCoroutine(Reload());
            }

            yield return new WaitForSeconds(shotInterval);
        }

        isShooting = false;
    }


    // 射撃
    private void ShootAtPlayer()
    {
        if (targetPlayer == null) return;

        // Look停止
        lookScript.TemporarilyDisableLook();

        Vector3 bulletPosition = bulletPoint.transform.position;
        Vector3 direction = bulletPoint.transform.forward;

        GameObject newBullet = Instantiate(bullet, bulletPosition, Quaternion.LookRotation(direction));
        newBullet.GetComponent<Rigidbody>().AddForce(direction * speed, ForceMode.Impulse);
    }

    private void DisableLookTemporarily()
    {
        if (lookScript != null)
        {
            lookScript.TemporarilyDisableLook();
        }
    }

    // リロード
    private IEnumerator Reload()
    {
        reloading = true;
        Debug.Log("リロード中...");
        yield return new WaitForSeconds(reloadTime);
        bulletCount = initialBulletCount;
        reloading = false;
        Debug.Log("リロード完了");
    }
}
