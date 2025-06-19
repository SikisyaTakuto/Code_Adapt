using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PanjanExplosion : MonoBehaviour
{
    // ��������
    public float BombTime;
    // EnemyDaed�A�j���[�V����
    public EnemyDaed enemyDaed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyDaed = GetComponent<EnemyDaed>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDetectObject(Collider collider)
    {
        if (enemyDaed.Dead)
        {
            // Player���͈͓��ɓ������Ƃ�
            if (collider.gameObject.tag == "Player")
            {
                StartCoroutine(Explos());
            }
        }
    }

    private IEnumerator Explos()
    {
        yield return new WaitForSeconds(3);
    }
}
