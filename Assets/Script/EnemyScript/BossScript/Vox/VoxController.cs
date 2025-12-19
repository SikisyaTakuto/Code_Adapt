using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VoxController : MonoBehaviour
{
    public bool isActivated = false;

    [Header("Boss HP")]
    [SerializeField] private int bossMaxHP = 100;
    private int bossCurrentHP;
    public UnityEngine.UI.Slider bossHpBar;

    [Header("Arms (Flying)")]
    [SerializeField] private GameObject Arms1;
    [SerializeField] private GameObject Arms2;
    [SerializeField] private GameObject Arms3;
    [SerializeField] private GameObject Arms4;
    [SerializeField] private GameObject Arms5;
    [SerializeField] private GameObject Arms6;
    [SerializeField] private GameObject Arms7;
    [SerializeField] private GameObject Arms8;

    [Header("VFX & Prefabs")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private GameObject EXexplosionEffect;
    [SerializeField] private GameObject boxPrefab;

    [Header("Movement & Game Params")]
    [SerializeField] private float dropHeight = -2f;
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float rareChance = 0.05f;
    [SerializeField] private int maxHP = 5;
    [SerializeField] private float sceneTransitionDelay = 3.0f;

    private GameObject[] armsArray;
    private float[] targetZs;
    private bool[] isMovingArray;
    private bool[] hasReachedSpecialZ;
    private bool[] canDropNow;
    private int[] armHPs;
    private bool[] isDestroyed;
    private GameObject[] heldBoxes;
    private Animator[] armAnimators;

    private const float SPECIAL_Z_FAR = -310f;
    private const float SPECIAL_Z_NEAR = -50f;

    void Start()
    {
        if (bossHpBar != null)
        {
            bossHpBar.gameObject.SetActive(false);
            bossHpBar.value = 1f;
        }
        bossCurrentHP = bossMaxHP;

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
            if (armsArray[i] != null) armAnimators[i] = armsArray[i].GetComponent<Animator>();
        }
        SetNewTargets();
    }

    void Update()
    {
        if (isActivated && bossHpBar != null && !bossHpBar.gameObject.activeSelf) bossHpBar.gameObject.SetActive(true);

        if (isActivated)
        {
            for (int i = 0; i < armsArray.Length; i++)
            {
                if (!isDestroyed[i]) MoveArm(i);
            }
            if (Input.GetKeyDown(KeyCode.K)) DamageBoss(10);
        }
    }

    // --- アニメーションイベントから呼ばれる関数 ---

    public void EnableArmDamage()
    {
        // 子オブジェクト全てのVoxBoneDamageをONにする
        VoxBoneDamage[] bones = GetComponentsInChildren<VoxBoneDamage>();
        foreach (var b in bones) b.isAttacking = true;
    }

    public void DisableArmDamage()
    {
        // 子オブジェクト全てのVoxBoneDamageをOFFにする
        VoxBoneDamage[] bones = GetComponentsInChildren<VoxBoneDamage>();
        foreach (var b in bones) b.isAttacking = false;
    }

    // --- ダメージ・移動処理 ---

    void SetNewTargets()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            if (isDestroyed[i] || armsArray[i] == null) continue;
            targetZs[i] = GetRandomTargetZ();
            isMovingArray[i] = true;
        }
    }

    public void DamageBoss(int damage)
    {
        if (bossCurrentHP <= 0) return;
        bossCurrentHP -= damage;
        if (bossHpBar != null) bossHpBar.value = (float)bossCurrentHP / (float)bossMaxHP;
        if (bossCurrentHP <= 0) BossDefeated();
    }

    public void TakeDamage(float damageAmount) => DamageBoss(Mathf.CeilToInt(damageAmount));

    private void BossDefeated()
    {
        if (MissionManager.Instance != null) MissionManager.Instance.CompleteCurrentMission();
        if (EXexplosionEffect != null) Instantiate(EXexplosionEffect, transform.position, Quaternion.identity);
        isActivated = false;
        StartCoroutine(WaitAndLoadScene(sceneTransitionDelay));
    }

    IEnumerator WaitAndLoadScene(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("ClearScene1");
    }

    void MoveArm(int index)
    {
        GameObject arm = armsArray[index];
        if (arm == null || !isMovingArray[index]) return;
        Vector3 pos = arm.transform.position;
        pos.z = Mathf.MoveTowards(pos.z, targetZs[index], moveSpeed * Time.deltaTime);
        arm.transform.position = pos;
        if (Mathf.Approximately(pos.z, targetZs[index]))
        {
            isMovingArray[index] = false;
            StartCoroutine(WaitAndRetarget(index));
        }
    }

    IEnumerator WaitAndRetarget(int index)
    {
        yield return new WaitForSeconds(2f);
        if (isActivated)
        {
            targetZs[index] = GetRandomTargetZ();
            isMovingArray[index] = true;
        }
    }

    float GetRandomTargetZ() => Random.Range(-255f, -110f);

    public Vector3 GetCenter() => transform.position;

    // --- VoxPart から呼ばれるアームへのダメージ処理 ---
    public void DamageArm(int index, int damage)
    {
        // 配列の範囲外チェックと、既に破壊されていないかのチェック
        if (index < 0 || index >= armHPs.Length || isDestroyed[index] || armsArray[index] == null)
            return;

        armHPs[index] -= damage;
        Debug.Log($"Arm {index} HP: {armHPs[index]}");

        if (armHPs[index] <= 0)
        {
            StartCoroutine(DestroyArm(index));
        }
    }

    // --- 腕の破壊演出 ---
    IEnumerator DestroyArm(int index)
    {
        isDestroyed[index] = true;
        GameObject arm = armsArray[index];

        if (arm != null)
        {
            if (explosionEffect != null)
                Instantiate(explosionEffect, arm.transform.position, Quaternion.identity);

            // 腕を非表示にする、または物理で落とすなどの処理
            arm.SetActive(false);
        }
        yield return null;
    }
}