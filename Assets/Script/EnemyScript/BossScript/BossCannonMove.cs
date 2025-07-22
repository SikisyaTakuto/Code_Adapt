using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossCannonMove : MonoBehaviour
{
    // X��
    public float jumpPowerX;
    // Y��
    public float jumpPowerY;
    // Z��
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
            Debug.Log("�W�����v");
            rb.linearVelocity = new Vector3(jumpPowerX, jumpPowerY, jumpPowerZ);
            jump = true;
        }
    }

    public void OnLoseObject(Collider other)
    {
        // Player���͈͊O�ɏo���Ƃ�
        if (other.CompareTag("Stage"))
        {
            StartCoroutine(Jump());
        }
    }

    private IEnumerator Jump()
    {
        // �W�����v
        yield return new WaitForSeconds(jumpCoolTime);
        jump = false;
    }
}
