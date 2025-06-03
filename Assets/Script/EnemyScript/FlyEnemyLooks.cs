using UnityEngine;

public class FlyEnemyLooks : MonoBehaviour
{
    // Playerの方向に向く変数
    public Transform target;

    // Update is called once per frame
    void Update()
    {
        // 変数 targetPos を作成してターゲットオブジェクトの座標を格納
        Vector3 targetPos = target.position;
        // 自分自身のY座標を変数 target のY座標に格納
        //（ターゲットオブジェクトのX、Z座標のみ参照）
        targetPos.y = transform.position.y;
    }

    public void OnDetectObject(Collider collider)
    {
        // Playerが範囲内に入ったとき
        if (collider.gameObject.tag == "Player")
        {
            // Playerの方向に向く
            transform.LookAt(target);

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
