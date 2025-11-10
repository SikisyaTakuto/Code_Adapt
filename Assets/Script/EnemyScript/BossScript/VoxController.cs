using UnityEngine;
using System.Collections;

public class VoxController : MonoBehaviour
{
    [SerializeField] private GameObject Arms1;
    [SerializeField] private GameObject Arms2;
    [SerializeField] private GameObject Arms3;
    [SerializeField] private GameObject Arms4;
    [SerializeField] private GameObject Arms5;
    [SerializeField] private GameObject Arms6;
    [SerializeField] private GameObject Arms7;
    [SerializeField] private GameObject Arms8;

    [SerializeField] private GameObject bombPrefab;   // 落とす爆弾Prefab
    [SerializeField] private GameObject enemyPrefab;  // 敵
    [SerializeField] private float dropHeight = -2f;  // アームの位置からどれくらい上に爆弾を出すか

    [SerializeField] private float moveSpeed = 20f;   // 移動速度

    private GameObject[] armsArray;
    private float[] targetZs;   // 目標Z座標
    private bool[] isMovingArray; // 移動中フラグ

    void Start()
    {
        armsArray = new GameObject[] { Arms1, Arms2, Arms3, Arms4,Arms5,Arms6,Arms7,Arms8 };
        targetZs = new float[armsArray.Length];
        isMovingArray = new bool[armsArray.Length];

        SetNewTargets(); // 開始時に全アームへランダム目標設定
    }

    void Update()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            MoveArm(i);
        }

        // Pキーで全アームの目標をリセット
        if (Input.GetKeyDown(KeyCode.P))
        {
            SetNewTargets();
        }
    }

    void SetNewTargets()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            targetZs[i] = Random.Range(-255f, -110f);
            isMovingArray[i] = true;
            Debug.Log($"{armsArray[i].name} の新しい目標Z: {targetZs[i]:F2}");
        }
    }

    // 各アームを動かす処理
    void MoveArm(int index)
    {
        GameObject arm = armsArray[index];
        if (arm == null || !isMovingArray[index]) return;

        Vector3 pos = arm.transform.position;
        float step = moveSpeed * Time.deltaTime;
        pos.z = Mathf.MoveTowards(pos.z, targetZs[index], step);
        arm.transform.position = pos;

        if (Mathf.Approximately(pos.z, targetZs[index]))
        {
            isMovingArray[index] = false;
            Debug.Log($"{arm.name} が Z={targetZs[index]:F2} に到達！");

            // 到達したらランダムに爆弾 or 敵を落とす
            DropRandomObject(arm);

            // 少し待って次の目標を再設定
            StartCoroutine(WaitAndRetarget(index));
        }
    }


    // 爆弾 or 敵をランダムで落とす処理
    void DropRandomObject(GameObject arm)
    {
        if (bombPrefab == null && enemyPrefab == null)
        {
            Debug.LogWarning("爆弾も敵も設定されていません！");
            return;
        }

        int choice = Random.Range(0, 2); // 0=爆弾, 1=敵
        GameObject prefab = (choice == 0 && bombPrefab != null) ? bombPrefab : enemyPrefab;
        string typeName = (choice == 0) ? "爆弾" : "敵";

        Vector3 spawnPos = arm.transform.position + Vector3.up * dropHeight;
        GameObject dropped = Instantiate(prefab, spawnPos, Quaternion.identity);

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb == null) rb = dropped.AddComponent<Rigidbody>();
        rb.useGravity = true;

        Debug.Log($"{arm.name} が {typeName} を投下しました！");
    }

    // 少し待って次の移動を再スタート
    IEnumerator WaitAndRetarget(int index)
    {
        yield return new WaitForSeconds(2f);
        targetZs[index] = Random.Range(-255f, -110f);
        isMovingArray[index] = true;
        Debug.Log($"{armsArray[index].name} が次の目標Z={targetZs[index]:F2} へ移動開始！");
    }
}
