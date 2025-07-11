using UnityEngine;

public class BossCannonMove : MonoBehaviour
{
    public float jumpPower;
    private Rigidbody rb;
    bool jump = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            // ジャンプ
            Debug.Log("ジャンプ");
            rb.linearVelocity = Vector3.up * jumpPower;
            jump = true;
        }
    }
}
