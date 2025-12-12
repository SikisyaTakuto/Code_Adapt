using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class VoxController : MonoBehaviour
{
    // ボスの動きを制御するためのフラグ
    public bool isActivated = false;

    // --- Boss本体のHP ---
    [Header("Boss HP")]
    [SerializeField] private int bossMaxHP = 100;
    private int bossCurrentHP;
    public UnityEngine.UI.Slider bossHpBar;

    // --- エディタから設定する変数 ---
    [Header("Arms")]
    [SerializeField] private GameObject Arms1;
    [SerializeField] private GameObject Arms2;
    [SerializeField] private GameObject Arms3;
    [SerializeField] private GameObject Arms4;
    [SerializeField] private GameObject Arms5;
    [SerializeField] private GameObject Arms6;
    [SerializeField] private GameObject Arms7;
    [SerializeField] private GameObject Arms8;

    //[Header("Droppable Objects & Effects")]
    //[SerializeField] private GameObject bombPrefab;         // 落とす爆弾Prefab
    //[SerializeField] private GameObject[] enemyPrefabs;     // 敵プレハブを配列に変更！

    [SerializeField] private GameObject explosionEffect;    // 爆発エフェクト

    [SerializeField] private GameObject boxPrefab;          // 落とす箱

    [Header("Movement & Game Params")]
    [SerializeField] private float dropHeight = -2f;        // アームの位置からどれくらい上に爆弾を出すか (Y座標調整)
    [SerializeField] private float moveSpeed = 20f;         // Z軸の移動速度
    [SerializeField] private float rareChance = 0.05f;      // 確率で特殊座標を選ぶ (5%)
    [SerializeField] private int maxHP = 5;                 // 各アームの初期HP

    // --- プライベートな状態変数 ---
    private GameObject[] armsArray;                         // アームオブジェクトの配列
    private float[] targetZs;                               // 各アームの目標Z座標
    private bool[] isMovingArray;                           // 各アームが移動中か
    private bool[] hasReachedSpecialZ;                      // 各アームが特殊Zに過去到達済みか
    private bool[] canDropNow;                              // 特殊Z到達後、物を落とせる状態か
    private int[] armHPs;                                   // 各アームの現在のHP
    private bool[] isDestroyed;                             // HP0で動作停止したか
    private GameObject[] heldBoxes;                         // アームが保持している箱
    private Animator[] armAnimators;                        // アニメーション


    // 特殊Z座標の定義
    private const float SPECIAL_Z_FAR = -310f;
    private const float SPECIAL_Z_NEAR = -50f;

    void Start()
    {
        bossHpBar.gameObject.SetActive(false);
        bossCurrentHP = bossMaxHP;
        bossHpBar.value = 1;

        // アームの配列を初期化
        armsArray = new GameObject[] { Arms1, Arms2, Arms3, Arms4, Arms5, Arms6, Arms7, Arms8 };
        // 状態配列をアームの数に合わせて初期化
        int count = armsArray.Length;
        targetZs = new float[count];
        isMovingArray = new bool[count];
        hasReachedSpecialZ = new bool[count];
        canDropNow = new bool[count];
        armHPs = new int[count];
        isDestroyed = new bool[count];
        heldBoxes = new GameObject[count];

        // HPを初期化
        for (int i = 0; i < count; i++)
        {
            armHPs[i] = maxHP;
            isDestroyed[i] = false;
            // 初回は物を落とせない状態からスタート
            hasReachedSpecialZ[i] = false;
            canDropNow[i] = false;
        }

        armAnimators = new Animator[armsArray.Length];
        for (int i = 0; i < armsArray.Length; i++)
        {
            armAnimators[i] = armsArray[i].GetComponent<Animator>();
        }

        SetNewTargets(); // 開始時に全アームへランダム目標設定し、移動開始
    }

    void Update()
    {
        // アクティブ化されたらHPバーを表示
        if (isActivated && !bossHpBar.gameObject.activeSelf)
        {
            bossHpBar.gameObject.SetActive(true);
        }

        if (isActivated)
        {
            // 壊れていないアームだけ動かす
            for (int i = 0; i < armsArray.Length; i++)
            {
                if (!isDestroyed[i])
                {
                    MoveArm(i);
                }
            }

            // デバッグ用: Kキーで全アームにダメージを与える
            if (Input.GetKeyDown(KeyCode.K))
            {
                for (int i = 0; i < armsArray.Length; i++)
                {
                    DamageArm(i, 1);
                }
                DamageBoss(10);
            }
        }
    }

    // HPを減らす関数（外部からの呼び出し用）
    public void DamageArm(int index, int damage)
    {
        // 既に破壊されていたら処理しない
        if (isDestroyed[index] || armsArray[index] == null) return;

        armHPs[index] -= damage;
        Debug.Log($"{armsArray[index].name} が {damage} ダメージを受けた！残りHP: {armHPs[index]}");

        // HPが0以下になったら破壊処理へ
        if (armHPs[index] <= 0)
        {
            StartCoroutine(DestroyArm(index));
        }
    }

    public void DamageBoss(int damage)
    {
        bossCurrentHP -= damage;
        bossCurrentHP = Mathf.Max(bossCurrentHP, 0);

        Debug.Log($"Boss が {damage} ダメージを受けた！残りHP: {bossCurrentHP}");

        bossHpBar.value = (float)bossCurrentHP / (float)bossMaxHP;

        if (bossCurrentHP <= 0)
        {
            BossDefeated();
        }
    }


    // 爆発＆停止処理
    IEnumerator DestroyArm(int index)
    {
        isDestroyed[index] = true; // 破壊フラグを立て、Updateでの移動を停止
        GameObject arm = armsArray[index];
        if (arm == null) yield break;

        Debug.Log($"{arm.name} が破壊されました！");

        // 爆発エフェクトの生成
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, arm.transform.position, Quaternion.identity);
        }

        // Rigidbodyがあれば物理挙動を止める（現在の移動制御を上書きしないように）
        Rigidbody rb = arm.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // アームを非表示にする（オリジナルのコードではコメントアウトされているが、物理的な停止と共に非表示にすることも可能）
        //arm.SetActive(false);

        yield return null;
    }

    // 全てのアームに新しい目標Zを設定（初期化時/リトライ時などに使用）
    void SetNewTargets()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            // 破壊されたアームは移動させない
            if (isDestroyed[i]) continue;

            targetZs[i] = GetRandomTargetZ();
            isMovingArray[i] = true;
            Debug.Log($"{armsArray[i].name} の新しい目標Z: {targetZs[i]:F2}");
        }
    }

    // 各アームを目標Z座標へ動かす処理
    void MoveArm(int index)
    {
        GameObject arm = armsArray[index];
        // アームがnullか、移動中でない場合は処理しない
        if (arm == null || !isMovingArray[index]) return;

        Vector3 pos = arm.transform.position;
        float step = moveSpeed * Time.deltaTime;

        // 目標Zへスムーズに移動
        pos.z = Mathf.MoveTowards(pos.z, targetZs[index], step);
        arm.transform.position = pos;

        // 目標に到達したか判定 (Approximatelyで浮動小数点数の誤差を吸収)
        if (Mathf.Approximately(pos.z, targetZs[index]))
        {
            isMovingArray[index] = false;
            Debug.Log($"{arm.name} が Z={targetZs[index]:F2} に到達！");

            // 特殊Z座標に到達
            if (targetZs[index] == SPECIAL_Z_FAR || targetZs[index] == SPECIAL_Z_NEAR)
            {
                hasReachedSpecialZ[index] = true;
                canDropNow[index] = true;

                // ★箱がまだ無い場合のみ生成してアームに保持★
                if (heldBoxes[index] == null && boxPrefab != null)
                {
                    Vector3 spawnPos = arm.transform.position + Vector3.up * dropHeight;
                    GameObject box = Instantiate(boxPrefab, spawnPos, Quaternion.identity);

                    // アームの子オブジェクトにして保持
                    box.transform.SetParent(arm.transform);

                    // 落ちないようにつける
                    Rigidbody rb = box.GetComponent<Rigidbody>();
                    if (rb == null) rb = box.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = true;

                    heldBoxes[index] = box;

                    Debug.Log($"{arm.name} は特殊Zで箱を生成して保持しました。");
                }
            }

            // 通常Z座標に到達
            else
            {
                // 特殊Zに行っており、箱を持っている場合 → 落とす
                if (canDropNow[index] && heldBoxes[index] != null)
                {
                    // アニメーション開始
                    if (armAnimators[index] != null)
                    {
                        armAnimators[index].SetTrigger("Drop");
                    }

                    // 少し待ってから箱を落とす（アニメに合わせる）
                    StartCoroutine(DropAfterAnimation(index, arm));

                    canDropNow[index] = false;
                }
            }

            // 到達したら次の目標設定と移動再開までの待機コルーチンを開始
            StartCoroutine(WaitAndRetarget(index));
        }
    }

    private void BossDefeated()
    {
        Debug.Log("Boss 撃破！");
        // エフェクト・停止処理など自由に追加
    }

    // 目標に到達後、少し待機してから次の移動目標を設定
    IEnumerator WaitAndRetarget(int index)
    {
        yield return new WaitForSeconds(2f); // 2秒待機

        // 待機中に破壊された場合はスキップ
        if (isDestroyed[index]) yield break;

        targetZs[index] = GetRandomTargetZ();  // 特殊確率付きの関数で次の目標Zを決定
        isMovingArray[index] = true;
        Debug.Log($"{armsArray[index].name} が次の目標Z={targetZs[index]:F2} へ移動開始！");
    }

    // 低確率 (rareChance) で特殊Zを返す関数
    float GetRandomTargetZ()
    {
        float roll = Random.value; // 0〜1の乱数

        if (roll < rareChance)
        {
            // 特殊Zのどちらかを選択 (-310f または -50f)
            return (Random.value < 0.5f) ? SPECIAL_Z_FAR : SPECIAL_Z_NEAR;
        }
        else
        {
            // 通常の範囲からランダム (-255fから-110f)
            // Random.Range(min, max) はfloatの場合、min以上、max以下を返す
            return Random.Range(-255f, -110f);
        }
    }

    void DropHeldBox(int index, GameObject arm)
    {
        GameObject box = heldBoxes[index];
        if (box == null) return;

        // 親子関係を解除
        box.transform.SetParent(null);

        // 落下できるように設定
        Rigidbody rb = box.GetComponent<Rigidbody>();
        if (rb == null) rb = box.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        heldBoxes[index] = null;
    }

    IEnumerator DropAfterAnimation(int index, GameObject arm)
    {
        yield return new WaitForSeconds(1f); // アニメに合わせて調整

        DropHeldBox(index, arm);
    }
}