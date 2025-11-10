using UnityEngine;
using System.Collections;

public class BombBehavior : MonoBehaviour
{
    private bool hasLanded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            hasLanded = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log($"{gameObject.name} が消滅しました。");
        Destroy(gameObject);
    }
}
