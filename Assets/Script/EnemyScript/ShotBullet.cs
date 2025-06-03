using UnityEngine;

public class ShotBullet : MonoBehaviour
{
    // ’e‚Ì”­ËêŠ
    [SerializeField] private GameObject bulletPoint;
    // ’e
    [SerializeField] private GameObject bullet;
    // ’e‚Ì‘¬‚³
    public float Speed;

    public void OnDetectObject(Collider collider)
    {
        // Player‚ª”ÍˆÍ“à‚É“ü‚Á‚½‚Æ‚«
        if (collider.gameObject.tag == "Player")
        {
            // ’e‚Ì”­ËêŠ‚ğæ“¾
            Vector3 bulletPosition = bulletPoint.transform.position;
            // ’e‚ÌPrefab‚ğì¬
            GameObject newBullet = Instantiate(bullet, bulletPosition, this.gameObject.transform.rotation);
            // ’e‚Ì”­Ë²‚ğæ“¾iZ²j
            Vector3 direction = newBullet.transform.forward;
            // ’e‚ğ”­ËiZ²j
            newBullet.GetComponent<Rigidbody>().AddForce(direction * Speed, ForceMode.Impulse);
            // ”­Ë‚µ‚½’e‚ğíœ
            //Destroy(newBullet, 0.8f);
            //Debug.Log("Œ‚‚Â");
        }
    }
}
