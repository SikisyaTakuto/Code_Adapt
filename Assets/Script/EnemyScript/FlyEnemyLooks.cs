using UnityEngine;

public class FlyEnemyLooks : MonoBehaviour
{
    // Playerの方向に向く変数
    public Transform target;

    // 回転速度
    public float rotationSpeed;

    // 死亡した場合のスクリプト
    public EnemyDaed enemyDaed;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDetectObject(Collider collider)
    {
        // Playerが範囲内に入ったとき
        if (collider.gameObject.tag == "Player" && !enemyDaed.Dead)
        {
            // Playerの方向に向く
            // 対象物と自分自身の座標からベクトルを算出して回転値を取得
            Vector3 vector3 = target.transform.position - this.transform.position;
            // 回転値を取得
            Quaternion quaternion = Quaternion.LookRotation(vector3);
            // 取得した回転値をこのゲームオブジェクトのrotationに代入
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, quaternion, rotationSpeed);
        }
    }

    public void OnTriggerExit(Collider collider)
    {
        // Playerが範囲外に出たとき
        if (collider.gameObject.tag == "Player")
        {
            // 進行方向に向く
            transform.rotation = Quaternion.identity;

        }
    }
}
