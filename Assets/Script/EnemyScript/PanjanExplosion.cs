using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

public class PanjanExplosion : MonoBehaviour
{
    // ��������
    public float BombTime;

    public bool Explos = false;
    // EnemyDaed�A�j���[�V����
    public EnemyDaed enemyDaed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDetectObject(Collider collider)
    {
        if (!enemyDaed.Dead)
        {
            // Player���͈͓��ɓ������Ƃ�
            if (collider.gameObject.tag == "Player")
            {
                StartCoroutine(ExplosDamage());
            }
        }
    }

    // ����
    private IEnumerator ExplosDamage()
    {
        yield return new WaitForSeconds(BombTime);
        Debug.Log("uuu");
        Explos = true;
        //Destroy(gameObject);
    }
}
