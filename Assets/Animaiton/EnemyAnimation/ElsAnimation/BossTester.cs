using UnityEngine;

/// <summary>
/// ボスのデバッグ・調整用クラス
/// </summary>
public class BossTester : MonoBehaviour
{
    private TestBoss boss;

    [Header("Testing Controls")]
    public bool useKeyCommands = true;

    [Header("Beam Settings (Visual Test)")]
    [Range(0.1f, 5.0f)] public float testBeamWidth = 1.0f;
    [SerializeField] private GameObject _beamEffectPrefab; // ビームのプレハブ

    [Header("Specific Action Trigger")]
    public bool triggerBeamAttack;
    public bool triggerStabAttack;

    void Start()
    {
        boss = GetComponent<TestBoss>();
    }

    void Update()
    {
        if (boss == null) return;

        // 1. キー入力による行動テスト
        if (useKeyCommands)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) StartCoroutine(ForceBeam());
            if (Input.GetKeyDown(KeyCode.Alpha2)) StartCoroutine(ForceStab());
        }

        // 2. インスペクターからのトリガー確認
        if (triggerBeamAttack) { triggerBeamAttack = false; StartCoroutine(ForceBeam()); }
        if (triggerStabAttack) { triggerStabAttack = false; StartCoroutine(ForceStab()); }

        // 3. リアルタイムでの太さ確認（シーン上の全パーティクルに反映）
        // テスト中にスライダーを動かすと、子オブジェクトのパーティクルのサイズが変わります
        UpdateParticlesWidth(testBeamWidth);
    }

    private void UpdateParticlesWidth(float width)
    {
        // 子要素にあるすべてのParticleSystemの開始サイズを調整
        ParticleSystem[] psList = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in psList)
        {
            var main = ps.main;
            main.startSizeMultiplier = width;
        }
    }

    private System.Collections.IEnumerator ForceBeam()
    {
        Debug.Log("TestMode: Executing Beam Attack");
        // TestBoss内のコルーチンを強制実行
        yield return boss.StartCoroutine("ExecuteBeamAttack");
    }

    private System.Collections.IEnumerator ForceStab()
    {
        Debug.Log("TestMode: Executing Stabbing Attack");
        // TestBoss内のコルーチンを強制実行
        yield return boss.StartCoroutine("ExecuteStabbingAttack");
    }

    private void OnGUI()
    {
        GUI.backgroundColor = Color.black;
        GUI.Box(new Rect(10, 10, 250, 80), "Boss Test Mode");
        GUI.Label(new Rect(20, 30, 230, 20), "Current Boss: " + boss.gameObject.name);
        GUI.Label(new Rect(20, 50, 230, 20), "[1] Beam Attack / [2] Stab Attack");
        GUI.Label(new Rect(20, 65, 230, 20), "Adjust 'Test Beam Width' in Inspector");
    }
}