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

    [SerializeField] private GameObject bombPrefab;    // 落とす爆弾Prefab
    [SerializeField] private GameObject enemyPrefab;   // 敵
    [SerializeField] private float dropHeight = -2f;   // アームの位置からどれくらい上に爆弾を出すか

    [SerializeField] private float moveSpeed = 20f;    // 移動速度
    [SerializeField] private float rareChance = 0.05f; // 確率で特殊座標を選ぶ

    private GameObject[] armsArray;
    private float[] targetZs;                          // 目標Z座標
    private bool[] isMovingArray;                      // 移動中フラグ
    private bool[] hasReachedSpecialZ;                 // 各アームが特殊Zに到達済みか
    private bool[] canDropNow;                         // 各アームが「現在物を落とせる状態」か

    void Start()
    {
        armsArray = new GameObject[] { Arms1, Arms2, Arms3, Arms4,Arms5,Arms6,Arms7,Arms8 };
        targetZs = new float[armsArray.Length];
        isMovingArray = new bool[armsArray.Length];
        hasReachedSpecialZ = new bool[armsArray.Length];
        canDropNow = new bool[armsArray.Length];

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

    // 新しい目標Zを設定（低確率で特殊Z）
    void SetNewTargets()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            targetZs[i] = GetRandomTargetZ();
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

            // 特殊Zに到達した場合
            if (targetZs[index] == -310f || targetZs[index] == -50f)
            {
                hasReachedSpecialZ[index] = true;
                canDropNow[index] = true;
                Debug.Log($"{arm.name} が特殊座標Z={targetZs[index]}に到達 → 物を落とせるようになりました。");
            }

            // 通常Z到達時
            else if (hasReachedSpecialZ[index])
            {
                if (canDropNow[index])
                {
                    DropRandomObject(arm);
                    canDropNow[index] = false;
                    Debug.Log($"{arm.name} は物を落としました。再び特殊Zに行くまで落とせません。");
                }
                else
                {
                    Debug.Log($"{arm.name} は特殊Zを再訪していないため、まだ落とせません。");
                }
            }

            // 特殊Zにも到達したことがない場合
            else
            {
                Debug.Log($"{arm.name} はまだ特殊Zに到達していないため、何も落としません。");
            }

            StartCoroutine(WaitAndRetarget(index));
        }
    }

    // 爆弾 or 敵をランダムに落とす
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
        targetZs[index] = GetRandomTargetZ();  // 特殊確率付きの関数を使用
        isMovingArray[index] = true;
        Debug.Log($"{armsArray[index].name} が次の目標Z={targetZs[index]:F2} へ移動開始！");
    }

    // 低確率で特殊Zを返す関数
    float GetRandomTargetZ()
    {
        float roll = Random.value; // 0〜1の乱数

        if (roll < rareChance)
        {
            // 特殊Zのどちらかを選択
            return (Random.value < 0.5f) ? -310f : -50f;
        }
        else
        {
            // 通常の範囲からランダム
            return Random.Range(-255f, -110f);
        }
    }
}
