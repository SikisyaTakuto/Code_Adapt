using UnityEngine;
using System.Collections;

public class VoxPart : MonoBehaviour
{
    public VoxController mainController;
    public int armIndex;

    [Header("ダメージ演出設定")]
    [SerializeField] private Renderer targetRenderer; // 色を変えるメッシュ
    [SerializeField] private Color flashColor = Color.red; // 点滅時の色
    [SerializeField] private float flashDuration = 0.1f; // 点滅時間

    private Color originalColor;
    private Material targetMaterial;

    void Start()
    {
        // Rendererが未設定なら自分から取得
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
            originalColor = targetMaterial.color;
        }
    }

    public void TakeDamage(float damage)
    {
        if (mainController != null)
        {
            int dmgInt = Mathf.CeilToInt(damage);

            // アーム自体のHPを減らす（これは制限なし）
            mainController.DamageArm(armIndex, dmgInt);

            // ボス本体へのダメージ（第2引数に true を渡してアーム経由であることを伝える）
            mainController.DamageBoss(dmgInt, true);

            // ダメージ演出
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (targetMaterial != null)
        {
            targetMaterial.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            targetMaterial.color = originalColor;
        }
    }
}