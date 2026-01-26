using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Unity.Burst.Intrinsics;
using UnityEngine.SceneManagement;

public class VoxController : MonoBehaviour
{
    // ボスの動きを制御するためのフラグ
    public bool isActivated = false;

    // --- Boss本体のHP ---
    [Header("Boss HP")]
    [SerializeField] private int bossMaxHP = 10000;
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

    [SerializeField] private GameObject explosionEffect;    // 爆発エフェクト

    [SerializeField] private GameObject EXexplosionEffect;    // 爆発エフェクト

    [SerializeField] private GameObject boxPrefab;          // 落とす箱

    [Header("Movement & Game Params")]
    [SerializeField] private float dropHeight = -2f;        // アームの位置からどれくらい上に爆弾を出すか (Y座標調整)
    [SerializeField] private float moveSpeed = 20f;         // Z軸の移動速度
    [SerializeField] private float rareChance = 0.05f;      // 確率で特殊座標を選ぶ (5%)
    [SerializeField] private int maxHP = 500;                 // 各アームの初期HP

    [Header("Player Interaction")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float attackCooldown = 3f;
    private float[] lastAttackTime;

    [Header("Enraged Mode")]
    [SerializeField] private float fastMoveSpeed = 60f; // 高速移動時の速度
    private bool isEnraged = false; // HP半分以下のフラグ

    [Header("Boss Body Movement")]
    private float bodyTargetZ;
    private bool isBodyMoving = false;
    [SerializeField] private float bodyMoveSpeed = 20f; // 通常時の本体速度

    // --- プライベートな状態変数 ---
    private GameObject[] armsArray;                         // アームオブジェクトの配列
    private float[] targetZs;                               // 各アームの目標Z座標
    private bool[] isMovingArray;                           // 各アームが移動中か
    private bool[] hasReachedSpecialZ;                      // 各アームが特殊Zに過去到達済みか
    private bool[] canDropNow;                              // 特殊Z到達後、物を落とせる状態か
    private int[] armHPs;                                   // 各アームの現在のHP
    private bool[] isDestroyed;                             // HP0で動作停止したか
    private GameObject[] heldBoxes;                         // アームが保持している箱
    private Animator[] armAnimators;                        // アームアニメーション

    // ボス本体のAnimator
    private Animator bossAnimator;

    // 特殊Z座標の定義
    private const float SPECIAL_Z_FAR = -310f;
    private const float SPECIAL_Z_NEAR = -50f;

    private float totalArmDamage = 0; // アーム経由で与えた累計ダメージ
    private float maxArmDamageLimit;  // アーム経由のダメージ上限（Awake/Startで設定）

    void Start()
    {
        bossAnimator = GetComponent<Animator>(); // これが必要です

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
        lastAttackTime = new float[count]; // ← これが重要！

        // HPを初期化
        for (int i = 0; i < count; i++)
        {
            armHPs[i] = maxHP;
            isDestroyed[i] = false;
            lastAttackTime[i] = -attackCooldown; // 最初から攻撃できるように
            // 初回は物を落とせない状態からスタート
            hasReachedSpecialZ[i] = false;
            canDropNow[i] = false;
        }

        armAnimators = new Animator[armsArray.Length];
        for (int i = 0; i < armsArray.Length; i++)
        {
            armAnimators[i] = armsArray[i].GetComponent<Animator>();
        }

        maxArmDamageLimit = bossMaxHP * 0.5f; // 最大HPの半分を上限に設定

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
            for (int i = 0; i < armsArray.Length; i++)
            {
                // --- 修正ポイント1: armsArray[i] が Null でないか、破壊されていないか厳密にチェック ---
                if (armsArray[i] != null && !isDestroyed[i])
                {
                    MoveArm(i);
                    CheckForAttack(i);
                }
            }

            //if (isEnraged && isBodyMoving)
            //{
            //    MoveBossBody();
            //}

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

    // --- 修正ポイント2: 内部変数の Null チェックを強化 ---
    void CheckForAttack(int index)
    {
        // 修正ポイント: プレイヤーがInspectorでセットされていない、またはアームが欠けている場合のガード
        if (playerTransform == null || armsArray[index] == null) return;

        float distance = Vector3.Distance(this.transform.position, playerTransform.position);

        if (distance <= attackRange )
        {
            // L3(2), L4(3), R1(4), R2(5) は除外
            if (index == 2 || index == 3 || index == 4 || index == 5) return;

            PerformAttack(index);
        }
    }

    void PerformAttack(int index)
    {
        lastAttackTime[index] = Time.time;

        // ボス本体のAnimatorに対して "Attack" トリガーのみを作動させる
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Attack");
            Debug.Log($"<color=orange>ボス本体のAttackトリガーを作動: {armsArray[index].name} が検知</color>");
        }
    }

    void MoveBossBody()
    {
        Vector3 pos = transform.position;
        // 発狂時は fastMoveSpeed を使用
        float step = fastMoveSpeed * Time.deltaTime;

        pos.z = Mathf.MoveTowards(pos.z, bodyTargetZ, step);
        transform.position = pos;

        // 到達したら次の目標を決める
        if (Mathf.Approximately(pos.z, bodyTargetZ))
        {
            StartCoroutine(WaitAndRetargetBody());
        }
    }

    IEnumerator WaitAndRetargetBody()
    {
        isBodyMoving = false;
        yield return new WaitForSeconds(1f); // 本体は1秒待機して次へ

        // Z座標 +-100 の範囲などでランダムに設定
        // 現在のZ位置からではなく、特定の中心点から +-100 したい場合はその値を。
        // ここでは GetRandomTargetZ を流用するか、独自に設定します。
        bodyTargetZ = Random.Range(-200f, -150f);
        isBodyMoving = true;
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

    public void DamageBoss(int damage, bool isFromArm = false)
    {
        int finalDamage = damage;

        // アームからのダメージの場合の制限処理
        if (isFromArm)
        {
            // 既に上限に達しているならダメージ無効
            if (totalArmDamage >= maxArmDamageLimit)
            {
                return;
            }

            // 今回のダメージを加算して上限を超えるなら、超えない分だけ適用
            if (totalArmDamage + damage > maxArmDamageLimit)
            {
                finalDamage = Mathf.FloorToInt(maxArmDamageLimit - totalArmDamage);
            }

            totalArmDamage += finalDamage;
            if (finalDamage > 0)
            {
                Debug.Log($"<color=yellow>アームダメージ適用: {finalDamage} (累計: {totalArmDamage}/{maxArmDamageLimit})</color>");
            }
        }

        bossCurrentHP -= finalDamage;
        bossCurrentHP = Mathf.Max(bossCurrentHP, 0);
        bossHpBar.value = (float)bossCurrentHP / (float)bossMaxHP;

        // --- 発狂モード判定 ---
        if (!isEnraged && bossCurrentHP <= bossMaxHP / 2)
        {
            isEnraged = true;
            moveSpeed = fastMoveSpeed;
            ForceRetargetAll();
        }

        if (bossCurrentHP <= 0)
        {
            BossDefeated();
        }
    }

    // 即座に全アームに再ターゲットさせる
    private void ForceRetargetAll()
    {
        for (int i = 0; i < armsArray.Length; i++)
        {
            if (armsArray[i] == null || isDestroyed[i]) continue;
            targetZs[i] = GetRandomTargetZ();
            isMovingArray[i] = true;
        }
    }

    /// <summary>
    /// ダメージを受ける共通メソッド
    /// </summary>
    /// <param name="damageAmount">ダメージ量</param>
    public void TakeDamage(float damageAmount)
    {

        // これがコンソールに出るか確認
        Debug.Log($"<color=red>ボスがダメージを受けました！量: {damageAmount}</color>");

        // 浮動小数点のダメージをintに変換（現在のHP変数がint型のため）
        int dmg = Mathf.CeilToInt(damageAmount);

        // 基本的にはボス本体へダメージを与える
        DamageBoss(dmg);

        // もし「特定のアームに攻撃が当たった時だけアームのHPを減らしたい」場合は、
        // 攻撃側の判定（Raycastなど）からアームのインデックスを取得して 
        // DamageArm(index, dmg) を呼ぶように拡張も可能です。
    }

    // ヒットエフェクトなどのためにコライダーの中心を返すプロパティ（必要に応じて）
    public Vector3 GetCenter()
    {
        return transform.position; // もしBoxの中心がズレているなら調整
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

        // 特殊Zに行っており、箱を持っている場合 → 落とす
        if (canDropNow[index] && heldBoxes[index] != null)
        {
            // アニメーション開始        
            armAnimators[index].SetTrigger("Drop");

            // 少し待ってから箱を落とす（アニメに合わせる）
            StartCoroutine(DropAfterAnimation(index, arm));

            canDropNow[index] = false;
        }

        // Rigidbodyがあれば物理挙動を止める（現在の移動制御を上書きしないように）
        Rigidbody rb = arm.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // アームを非表示にする（オリジナルのコードではコメントアウトされているが、物理的な停止と共に非表示にすることも可能）
        StartCoroutine(ArmDelete(index, arm));

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

                // 箱がまだ無い場合のみ生成してアームに保持
                if (heldBoxes[index] == null && boxPrefab != null)
                {
                    Vector3 spawnPos = arm.transform.position + Vector3.up * dropHeight;
                    GameObject box = Instantiate(boxPrefab, spawnPos, Quaternion.identity);

                    // アニメーション開始        
                    armAnimators[index].SetTrigger("Grap");

                    // アームの子オブジェクトにして保持
                    box.transform.SetParent(arm.transform);

                    // アニメーション開始        
                    armAnimators[index].SetTrigger("Hold");

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
                    armAnimators[index].SetTrigger("Drop");

                    // 少し待ってから箱を落とす（アニメに合わせる）
                    StartCoroutine(DropAfterAnimation(index, arm));

                    canDropNow[index] = false;

                    // アニメーション開始        
                    armAnimators[index].SetTrigger("Idle");
                }
            }

            // 到達したら次の目標設定と移動再開までの待機コルーチンを開始
            StartCoroutine(WaitAndRetarget(index));
        }
    }

    private void BossDefeated()
    {
        Debug.Log("<color=red>Boss 撃破！全機能停止します。</color>");

        // 1. 全体のアクティブフラグをオフにする（Update内の処理が止まる）
        isActivated = false;
        isBodyMoving = false; // 本体の移動停止

        // 2. 全アームの移動とアニメーションを停止
        for (int i = 0; i < armsArray.Length; i++)
        {
            isMovingArray[i] = false;

            // アニメーションをIdleにするか、Speedを0にして固める
            if (armAnimators[i] != null)
            {
                armAnimators[i].speed = 0; // その場で動きを凍結させる場合
                                           // または armAnimators[i].SetTrigger("Idle");
            }

            // もし箱を持っていたら物理的に切り離す（お好みで）
            if (heldBoxes[i] != null)
            {
                DropHeldBox(i, armsArray[i]);
            }
        }

        // 3. ボス本体のアニメーターも停止
        if (bossAnimator != null)
        {
            bossAnimator.speed = 0; // 撃破時のポーズで固める場合
        }

        // 4. ミッション完了通知
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CompleteCurrentMission();
        }

        // 5. 撃破エフェクト
        if (EXexplosionEffect != null)
        {
            Instantiate(EXexplosionEffect, transform.position, Quaternion.identity);
        }

        // 6. クリアシーンへ
        StartCoroutine(WaitAndLoadScene());
    }

    IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(2.0f); // 2秒待つ
        SceneManager.LoadScene("ClearScene1");
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
        yield return new WaitForSeconds(0f); // アニメに合わせて調整

        DropHeldBox(index, arm);
    }

    IEnumerator ArmDelete(int index, GameObject arm)
    {
        yield return new WaitForSeconds(1f);

        // アームを非表示にする（オリジナルのコードではコメントアウトされているが、物理的な停止と共に非表示にすることも可能）
        arm.SetActive(false);
    }
}