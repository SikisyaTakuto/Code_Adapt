using UnityEngine;
using System.Collections;

public class BossEnemyDead : MonoBehaviour
{
    // �A�j���[�V����
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
        // �X�y�[�X�L�[���������Ƃ�(HP��0�ɂȂ����Ƃ�)
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
