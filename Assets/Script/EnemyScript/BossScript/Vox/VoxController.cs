using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // シーン遷移に必要

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

    [Header("VFX & Prefabs")]
    [SerializeField] private GameObject explosionEffect;    // アーム破壊用エフェクト
    [SerializeField] private GameObject EXexplosionEffect;  // ボス撃破用エフェクト
    [SerializeField] private GameObject boxPrefab;          // 落とす箱

    [Header("Movement & Game Params")]
    [SerializeField] private float dropHeight = -2f;        // 箱生成の高さ調整
    [SerializeField] private float moveSpeed = 20f;          // Z軸の移動速度
    [SerializeField] private float rareChance = 0.05f;      // 特殊座標を選ぶ確率
    [SerializeField] private int maxHP = 5;                  // 各アームの初期HP
    [SerializeField] private float sceneTransitionDelay = 3.0f; // 撃破からシーン移動までの待ち時間

    // --- プライベートな状態変数 ---
    private GameObject[] armsArray;
    private float[] targetZs;
    private bool[] isMovingArray;
    private bool[] hasReachedSpecialZ;
    private bool[] canDropNow;
    private int[] armHPs;
    private bool[] isDestroyed;
    private GameObject[] heldBoxes;
    private Animator[] armAnimators;

    // 特殊Z座標の定義
    private const float SPECIAL_Z_FAR = -310f;
    private const float SPECIAL_Z_NEAR = -50f;

    void Start()
    {
        bossHpBar.gameObject.SetActive(false);
        bossCurrentHP = bossMaxHP;
        bossHpBar.value = 1f;

        // アームの配列を初期化
        armsArray = new GameObject[] { Arms1, Arms2, Arms3, Arms4, Arms5, Arms6, Arms7, Arms8 };

        int count = armsArray.Length;
        targetZs = new float[count];
        isMovingArray = new bool[count];
        hasReachedSpecialZ = new bool[count];
        canDropNow = new bool[count];
        armHPs = new int[count];
        isDestroyed = new bool[count];
        heldBoxes = new GameObject[count];
        armAnimators = new Animator[count];

        for (int i = 0; i < count; i++)
        {
            armHPs[i] = maxHP;
            isDestroyed[i] = false;
            hasReachedSpecialZ[i] = false;
            canDropNow[i] = false;
            if (armsArray[i] != null)
            {
                armAnimators[i] = armsArray[i].GetComponent<Animator>();
            }
        }

        SetNewTargets();
    }

    void Update()
    {
        if (isActivated && !bossHpBar.gameObject.activeSelf)
        {
            bossHpBar.gameObject.SetActive(true);
        }

        if (isActivated)
        {
            for (int i = 0; i < armsArray.Length; i++)
            {
                if (!isDestroyed[i])
                {
                    MoveArm(i);
                }
            }

            // デバッグ用: Kキーでダメージ
            if (Input.GetKeyDown(KeyCode.K))
            {
                for (int i = 0; i < armsArray.Length; i++) DamageArm(i, 1);
                DamageBoss(10);
            }
        }
    }

    public void DamageArm(int index, int damage)
    {
        if (isDestroyed[index] || armsArray[index] == null) return;

        armHPs[index] -= damage;
        Debug.Log($"{armsArray[index].name} HP: {armHPs[index]}");

        if (armHPs[index] <= 0)
        {
            StartCoroutine(DestroyArm(index));
        }
    }

    public void DamageBoss(int damage)
    {
        if (bossCurrentHP <= 0) return; // すでに死亡していたら何もしない

        bossCurrentHP -= damage;
        bossCurrentHP = Mathf.Max(bossCurrentHP, 0);
        bossHpBar.value = (float)bossCurrentHP / (float)bossMaxHP;

        Debug.Log($"Boss HP: {bossCurrentHP}");

        if (bossCurrentHP <= 0)
        {
            BossDefeated();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (damageAmount <= 0) return;
        Debug.Log($"<color=red>ボス本体ダメージ: {damageAmount}</color>");
        DamageBoss(Mathf.CeilToInt(damageAmount));
    }

    private void BossDefeated()
    {
        Debug.Log("Boss 撃破！数秒後にシーンを切り替えます。");

        // 撃破エフェクト
        if (EXexplosionEffect != null)
        {
            Instantiate(EXexplosionEffect, transform.position, Quaternion.identity);
        }

        // 全アームの動きを止める
        isActivated = false;

        // シーン遷移コルーチン開始
        StartCoroutine(WaitAndLoadScene(sceneTransitionDelay));
    }

    IEnumerator WaitAndLoadScene(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("ClearScene1");
    }

    IEnumerator DestroyArm(int index)
    {
        isDestroyed[index] = true;
        GameObject arm = armsArray[index];
        if (arm == null) yield break;

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, arm.transform.position, Quaternion.identity);
        }

        Rigidbody rb = arm.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        yield return null;
    }

    void SetNewTargets()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            if (isDestroyed[i]) continue;
            targetZs[i] = GetRandomTargetZ();
            isMovingArray[i] = true;
        }
    }

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

            if (targetZs[index] == SPECIAL_Z_FAR || targetZs[index] == SPECIAL_Z_NEAR)
            {
                hasReachedSpecialZ[index] = true;
                canDropNow[index] = true;

                if (heldBoxes[index] == null && boxPrefab != null)
                {
                    Vector3 spawnPos = arm.transform.position + Vector3.up * dropHeight;
                    GameObject box = Instantiate(boxPrefab, spawnPos, Quaternion.identity);

                    if (armAnimators[index] != null) armAnimators[index].SetTrigger("Grap");

                    box.transform.SetParent(arm.transform);

                    if (armAnimators[index] != null) armAnimators[index].SetTrigger("Hold");

                    Rigidbody rb = box.GetComponent<Rigidbody>();
                    if (rb == null) rb = box.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = true;

                    heldBoxes[index] = box;
                }
            }
            else
            {
                if (canDropNow[index] && heldBoxes[index] != null)
                {
                    if (armAnimators[index] != null) armAnimators[index].SetTrigger("Drop");
                    StartCoroutine(DropAfterAnimation(index, arm));
                    canDropNow[index] = false;
                    if (armAnimators[index] != null) armAnimators[index].SetTrigger("Idle");
                }
            }
            StartCoroutine(WaitAndRetarget(index));
        }
    }

    IEnumerator WaitAndRetarget(int index)
    {
        yield return new WaitForSeconds(2f);
        if (!isDestroyed[index] && isActivated)
        {
            targetZs[index] = GetRandomTargetZ();
            isMovingArray[index] = true;
        }
    }

    float GetRandomTargetZ()
    {
        if (Random.value < rareChance)
        {
            return (Random.value < 0.5f) ? SPECIAL_Z_FAR : SPECIAL_Z_NEAR;
        }
        return Random.Range(-255f, -110f);
    }

    void DropHeldBox(int index, GameObject arm)
    {
        GameObject box = heldBoxes[index];
        if (box == null) return;

        box.transform.SetParent(null);
        Rigidbody rb = box.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }
        heldBoxes[index] = null;
    }

    IEnumerator DropAfterAnimation(int index, GameObject arm)
    {
        yield return new WaitForSeconds(0.1f);
        DropHeldBox(index, arm);
    }

    public Vector3 GetCenter()
    {
        return transform.position;
    }
}