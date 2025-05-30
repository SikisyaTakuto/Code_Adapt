using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    public Transform target; // 追従する対象（プレイヤーなど）
    public Vector3 offset = new Vector3(0, 50, 0); // 高さと位置の調整

    void LateUpdate()
    {
        if (target != null)
        {
            // プレイヤーの位置に追従（回転なし）
            Vector3 newPos = target.position + offset;
            transform.position = newPos;

            // 真下を向く（真上からの視点）
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
