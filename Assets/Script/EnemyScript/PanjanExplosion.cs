using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PanjanExplosion : MonoBehaviour
{
    // 自爆時間
    public float BombTime;
    // EnemyDaedアニメーション
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
            // Playerが範囲内に入ったとき
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
