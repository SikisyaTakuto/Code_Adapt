using UnityEngine;
using System.Collections;

public class EnemyControllerTest : MonoBehaviour
{
    [Header("Status")]
    public float maxHp = 999999f; // Single.MaxValue に近い耐久力
    private float _currentHp;
    private float _totalDamageTaken = 0f;

    [Header("Visual Response")]
    [SerializeField] private Color _damageColor = Color.red;
    private Color _originalColor;
    private Renderer _renderer;
    private Coroutine _flashCoroutine;

    void Start()
    {
        _currentHp = maxHp;
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;

        Debug.Log($"<color=green>[Sandbag Initialized]</color> HP: {_currentHp}");
    }

    /// <summary>
    /// 各コントローラーから呼ばれるダメージ処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        // ダメージ計算（floatの精度を保ちつつ加算）
        _totalDamageTaken += damage;
        _currentHp -= damage;

        // ログ表示
        Debug.Log($"<color=yellow>Hit!</color> Damage: {damage} / Total: {_totalDamageTaken}");

        // 死なないようにHPを維持
        if (_currentHp <= 0)
        {
            _currentHp = maxHp;
            Debug.Log("<color=cyan>Sandbag Revived!</color>");
        }

        // ヒット時の演出（赤く光る）
        if (_renderer != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashEffect());
        }
    }

    private IEnumerator FlashEffect()
    {
        _renderer.material.color = _damageColor;
        yield return new WaitForSeconds(0.1f);
        _renderer.material.color = _originalColor;
    }

    // デバッグ用に現在の累計ダメージを頭上に表示（任意）
    void OnGUI()
    {
        //Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        //if (screenPos.z > 0)
        //{
        //    GUI.color = Color.white;
        //    GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 200, 20), $"Total Damage: {_totalDamageTaken:F1}");
        //}
    }
}