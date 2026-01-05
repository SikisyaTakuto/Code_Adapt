using UnityEngine;

public class GravityEffect : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f; // 上昇速度
    [SerializeField] private float lifeTime = 3.0f;  // 消えるまでの時間

    private Material mat;
    private Color baseColor;

    void Start()
    {
        // レンダラーからマテリアルを取得
        if (GetComponent<Renderer>() != null)
        {
            mat = GetComponent<Renderer>().material;
            baseColor = mat.color;
        }

        // 指定時間後に自分自身を削除
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 常に上方向へ移動
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);


        // 消える直前にフェードアウトさせたい場合（オプション）
        if (mat != null)
        {
            // 残り時間に連動して透明度を下げる
            float alpha = Mathf.Clamp01(mat.color.a - (Time.deltaTime / lifeTime));
            mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}