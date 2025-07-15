using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

public class PanjanExplosion : MonoBehaviour
{
    // 自爆時間
    public float BombTime;

    public bool Explos = false;
    // EnemyDaedアニメーション
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
            // Playerが範囲内に入ったとき
            if (collider.gameObject.tag == "Player")
            {
                StartCoroutine(ExplosDamage());
            }
        }
    }

    // 自爆
    private IEnumerator ExplosDamage()
    {
        yield return new WaitForSeconds(BombTime);
        Debug.Log("uuu");
        Explos = true;
        //Destroy(gameObject);
    }
}
