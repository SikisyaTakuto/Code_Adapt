using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProvidenceBoss : MonoBehaviour
{
    // === 公開変数 (共通・プロヴィデンス) ===
    public float moveSpeed = 5f; // 移動速度
    public Transform target; // ターゲット（プレイヤー）

    // GNビームライフル (プロヴィデンスのビームライフルに相当)
    public float beamRifleFireRate = 0.5f; // 発射間隔
    public GameObject beamRifleBulletPrefab; // 弾Prefab
    public Transform beamRifleMuzzle; // 銃口

    // === 追加変数 (GNホルスタービット/シールドビット - サバーニャ) ===
    [Header("GN Holster Bit Settings (Sabanya Attack)")]
    public GameObject gnHolsterBitPrefab; // ホルスタービットのPrefab (DragoonPrefabを流用可能)
    public Transform[] holsterBitSpawnPoints; // ホルスタービットの射出位置
    public float holsterBitCooldown = 12f; // ホルスタービットのクールダウン
    public float bitAttackDuration = 8f; // ビットを展開して攻撃する時間

    [Header("GN Shield Bit Settings (Sabanya Defense)")]
    public GameObject shieldBitPrefab; // シールドビットのPrefab
    public Transform[] shieldBitSpawnPoints; // シールドビットの射出位置（トリケロス部分を想定）
    public float shieldDuration = 5f; // シールド展開時間
    public int maxShieldCount = 6; // ★★★ シールドビットの最大展開数 ★★★

    // === プライベート変数 ===
    private float nextHolsterBitTime;
    private float nextBeamRifleTime;
    private int bitCount = 8; // ビットの数
    private bool isShieldActive = false;

    // シールドビットのインスタンスを保持するためのリスト (デバッグとクリーンアップ用)
    private List<GameObject> activeShields = new List<GameObject>();

    // ボスのHP管理 (防御のために追加)
    [Header("Health")]
    public float maxHP = 1000f;
    private float currentHP;

    void Start()
    {
        currentHP = maxHP;

        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        nextHolsterBitTime = Time.time;
    }

    void Update()
    {
        if (target == null) return;

        // ターゲットに顔を向ける
        Vector3 lookDir = target.position - transform.position;
        lookDir.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

        HandleMovement();
        HandleAttacks();
        // HandleDefenseLogic() は削除され、TakeDamageに統合されました。

        // ?? DEBUG MODE: Jキーの機能強化 (Unity Editor内でのみ動作)
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.J))
        {
            // Shift + J でシールドを強制展開/終了
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (!isShieldActive)
                {
                    // 強制発動
                    StartCoroutine(DeployShieldBits());
                    Debug.Log("[DEBUG] Shift+J: GNシールドビットを強制展開しました。");
                }
                else
                {
                    // 強制終了
                    StopAllCoroutines();
                    CleanupShields();
                    isShieldActive = false;
                    Debug.Log("[DEBUG] Shift+J: シールドを強制解除しました。");
                }
            }
            else
            {
                // J のみで100ダメージを与える
                float debugDamage = 100f;
                TakeDamage(debugDamage);
                // ダメージを受けたことでシールド発動ロジックが動くかテスト
                Debug.Log($"[DEBUG] Jキーが押されました。ボスに {debugDamage} ダメージを与えました。残りHP: {currentHP}");
            }
        }
#endif
    }

    // --- 移動処理（プロヴィデンスと同じ） ---
    void HandleMovement()
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float preferredDistance = 15f;

        if (distanceToTarget > preferredDistance + 2f)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
        else if (distanceToTarget < preferredDistance - 2f)
        {
            transform.position -= transform.forward * moveSpeed * Time.deltaTime;
        }
    }

    // --- 攻撃処理 ---
    void HandleAttacks()
    {
        // 1. GNホルスタービット (ドラグーン・システムに相当)
        if (Time.time >= nextHolsterBitTime)
        {
            StartCoroutine(DeployHolsterBits());
            nextHolsterBitTime = Time.time + holsterBitCooldown;
        }

        // 2. GNビームライフル (プロヴィデンスのビームライフルに相当)
        if (Time.time >= nextBeamRifleTime)
        {
            FireBeamRifle();
            nextBeamRifleTime = Time.time + beamRifleFireRate;
        }
    }

    // --- ドラグーン/ビット射出コルーチン ---
    IEnumerator DeployHolsterBits()
    {
        for (int i = 0; i < bitCount; i++)
        {
            Transform spawnPoint = holsterBitSpawnPoints[i % holsterBitSpawnPoints.Length];
            GameObject bitGO = Instantiate(gnHolsterBitPrefab, spawnPoint.position, Quaternion.identity);
            // Dragoonスクリプトがある前提
            Dragoon bitScript = bitGO.GetComponent<Dragoon>();

            if (bitScript != null)
            {
                bitScript.Initialize(target, i);
                Destroy(bitGO, bitAttackDuration);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // --- ビームライフル発射（プロヴィデンスと同じ） ---
    void FireBeamRifle()
    {
        if (beamRifleMuzzle != null && beamRifleBulletPrefab != null)
        {
            Instantiate(beamRifleBulletPrefab, beamRifleMuzzle.position, beamRifleMuzzle.rotation);
        }
    }

    // --- シールドビットのクリーンアップ ---
    void CleanupShields()
    {
        foreach (var shield in activeShields)
        {
            if (shield != null)
            {
                Destroy(shield);
            }
        }
        activeShields.Clear();
    }


    // --- シールドビット展開コルーチン ---
    IEnumerator DeployShieldBits()
    {
        // 既にアクティブな場合は何もしない
        if (isShieldActive) yield break;

        isShieldActive = true;
        Debug.Log("GNシールドビット展開開始！");

        CleanupShields(); // 念のため古いシールドを削除

        // 1. シールドビットを最大数まで射出
        int count = Mathf.Min(maxShieldCount, shieldBitSpawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            // 射出位置を循環使用
            Transform spawnPoint = shieldBitSpawnPoints[i % shieldBitSpawnPoints.Length];

            if (shieldBitPrefab == null || spawnPoint == null)
            {
                Debug.LogError("シールドビットのPrefabまたはSpawn Pointが設定されていません！");
                isShieldActive = false;
                yield break;
            }

            GameObject shieldGO = Instantiate(shieldBitPrefab, spawnPoint.position, Quaternion.identity);
            shieldGO.transform.parent = transform;

            activeShields.Add(shieldGO);
            yield return new WaitForSeconds(0.1f);
        }

        // 2. シールド展開時間待機
        yield return new WaitForSeconds(shieldDuration);

        // 3. シールドを解除
        CleanupShields();
        isShieldActive = false;
        Debug.Log("GNシールドビット格納。");
    }

    // --- ダメージ受付 ---
    public void TakeDamage(float damage)
    {
        if (target == null) return;

        // ★★★ ダメージを受けた際に、シールドが展開中でなければ発動 ★★★
        if (!isShieldActive)
        {
            StartCoroutine(DeployShieldBits());
        }

        float finalDamage = damage;
        if (isShieldActive)
        {
            // シールド展開中はダメージを軽減
            finalDamage *= 0.1f;
        }

        currentHP -= finalDamage;
        currentHP = Mathf.Max(0, currentHP);

        Debug.Log($"ボスが {finalDamage} のダメージを受けました。残りHP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Debug.Log("プロヴィデンス/サバーニャ撃破！");
            Destroy(gameObject);
        }
    }
}