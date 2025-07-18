using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossCannonMove : MonoBehaviour
{
    // X軸
    public float jumpPowerX;
    // Y軸
    public float jumpPowerY;
    // Z軸
    public float jumpPowerZ;

    public float jumpCoolTime;
    private Rigidbody rb;
    public bool jump = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && !jump)
        {
            Debug.Log("ジャンプ");
            rb.linearVelocity = new Vector3(jumpPowerX, jumpPowerY, jumpPowerZ);
            jump = true;
        }
    }

    public void OnLoseObject(Collider other)
    {
        // Playerが範囲外に出たとき
        if (other.CompareTag("Stage"))
        {
            StartCoroutine(Jump());
        }
    }

    private IEnumerator Jump()
    {
        // ジャンプ
        yield return new WaitForSeconds(jumpCoolTime);
        jump = false;
    }
}
