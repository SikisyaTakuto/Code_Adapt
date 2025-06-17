using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class EnemyDaed : MonoBehaviour
{
    // アニメーション
    //Animator animator;
    // HP0
    public bool Dead = false;

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
            Dead = true;
            //animator.SetTrigger("Dead");
            yield return new WaitForSeconds(3);
            Destroy(gameObject);
        }
    }
}
