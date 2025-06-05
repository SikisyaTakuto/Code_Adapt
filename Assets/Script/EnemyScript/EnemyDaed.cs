using System.Collections;
using UnityEngine;

public class EnemyDaed : MonoBehaviour
{
    // アニメーション
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(DaedDestroy());
    }

    private IEnumerator DaedDestroy()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Daed");
            yield return new WaitForSeconds(3);
            Destroy(gameObject);
        }
    }
}
