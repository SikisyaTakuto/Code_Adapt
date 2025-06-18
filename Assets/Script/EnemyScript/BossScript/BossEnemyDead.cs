using UnityEngine;
using System.Collections;

public class BossEnemyDead : MonoBehaviour
{
    // アニメーション
    //Animator animator;
    // HP0
    public bool BossDead = false;

    void Start()
    {
        //animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(DaedDestroy());
    }

    private IEnumerator DaedDestroy()
    {
        // スペースキーを押したとき(HPが0になったとき)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Death");
            BossDead = true;
            //animator.SetTrigger("Dead");
            yield return new WaitForSeconds(3);
            Destroy(gameObject);
        }
    }
}
