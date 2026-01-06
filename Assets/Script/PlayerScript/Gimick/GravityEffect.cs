using UnityEngine;

public class GravityEffect : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f; // 下降速度
    [SerializeField] private float lifeTime = 3.0f;  // 消えるまでの時間

    private Material mat;
    private Color baseColor;

    void Start()
    {
        if (GetComponent<Renderer>() != null)
        {
            mat = GetComponent<Renderer>().material;
            baseColor = mat.color;
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // --- 修正箇所：Vector3.up から Vector3.down に変更 ---
        // これで世界座標（Space.World）に対して真下に移動します
        transform.Translate(Vector3.down * moveSpeed * Time.deltaTime, Space.World);

        if (mat != null)
        {
            float alpha = Mathf.Clamp01(mat.color.a - (Time.deltaTime / lifeTime));
            mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}