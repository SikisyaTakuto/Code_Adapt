using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class EnemyDaed : MonoBehaviour
{
    // �A�j���[�V����
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
        // K�L�[���������Ƃ�(HP��0�ɂȂ����Ƃ�)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Death");
            Dead = true;
            //animator.SetTrigger("Dead");
            yield return new WaitForSeconds(0);
        }
    }
}
