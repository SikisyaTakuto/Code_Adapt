using System.Collections;
using UnityEngine;

public class ShotBullet : MonoBehaviour
{
    // íeÇÃî≠éÀà íu
    [SerializeField] private GameObject bulletPoint;
    // íeÇÃPrefab
    [SerializeField] private GameObject bullet;
    // É}ÉKÉWÉìÇÃíeêî
    [SerializeField] private float bulletCount = 20f;
    // íeÇÃî≠éÀä‘äu
    [SerializeField] private float shotInterval = 1.5f;
    // ÉäÉçÅ[Éhéûä‘
    [SerializeField] private float reloadTime = 3.0f;
    // íeë¨
    [SerializeField] private float speed = 20f;
    // åÇÇøénÇﬂÇÈÇ‹Ç≈ÇÃë“ã@éûä‘
    [SerializeField] private float shootingStartDelay = 1.0f;
    // ìGÇÃéÄñSèÛë‘
    public EnemyDaed enemyDaed;

    private float initialBulletCount;
    private bool isPlayerInRange = false;
    private bool isShooting = false;
    private bool reloading = false;
    private Transform targetPlayer;

    void Start()
    {
        // èâä˙É}ÉKÉWÉìÇÃíeêî
        initialBulletCount = bulletCount;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !enemyDaed.Dead)
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

        // îÕàÕì‡Ç…Ç‹ÇæÇ¢ÇƒÅAìGÇ™éÄÇÒÇ≈Ç¢Ç»ÇØÇÍÇŒéÀåÇäJén
        if (isPlayerInRange && !enemyDaed.Dead)
        {
            StartCoroutine(ShootingLoop());
        }
        else
        {
            isShooting = false; // åÇÇ¬ïKóvÇ™Ç»Ç≠Ç»Ç¡ÇΩèÍçá
        }
    }

    private IEnumerator ShootingLoop()
    {
        isShooting = true;

        while (isPlayerInRange && !enemyDaed.Dead)
        {
            if (bulletCount > 0 && !reloading)
            {
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

    private void ShootAtPlayer()
    {
        if (targetPlayer == null) return;

        Vector3 bulletPosition = bulletPoint.transform.position;
        Vector3 direction = (targetPlayer.position - bulletPosition).normalized;

        GameObject newBullet = Instantiate(bullet, bulletPosition, Quaternion.LookRotation(direction));
        newBullet.GetComponent<Rigidbody>().AddForce(direction * speed, ForceMode.Impulse);
    }

    private IEnumerator Reload()
    {
        reloading = true;
        Debug.Log("ÉäÉçÅ[ÉhíÜ...");
        yield return new WaitForSeconds(reloadTime);
        bulletCount = initialBulletCount;
        reloading = false;
        Debug.Log("ÉäÉçÅ[ÉhäÆóπ");
    }
}
