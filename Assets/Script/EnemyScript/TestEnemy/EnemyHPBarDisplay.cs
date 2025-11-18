using UnityEngine;
using UnityEngine.UI; // Sliderを使うために必要

public class EnemyHPBarDisplay : MonoBehaviour
{
    [Header("HP Bar Settings")]
    [Tooltip("表示するHPバーのSliderコンポーネント。")]
    public Slider hpSlider;
    [Tooltip("敵のGameObjectの上部からのオフセット。")]
    public Vector3 offset = new Vector3(0, 1.5f, 0); // HPバーの表示位置調整用

    private EnemyHealth enemyHealth; // 敵のHP情報を取得するための参照
    private Camera mainCamera;       // HPバーを常にカメラの方へ向けるために必要

    void Awake()
    {
        // 親オブジェクト（敵本体）からEnemyHealthコンポーネントを取得
        enemyHealth = GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("EnemyHealthコンポーネントが親オブジェクトに見つかりません！");
            enabled = false; // スクリプトを無効にする
            return;
        }

        // シーン内のメインカメラを取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("メインカメラが見つかりません！シーンにTag: MainCameraが付与されたカメラがあることを確認してください。");
            enabled = false; // スクリプトを無効にする
            return;
        }

        // HPバーのスライダーが設定されているか確認
        if (hpSlider == null)
        {
            Debug.LogError("HP Sliderが設定されていません！インスペクターで設定してください。");
            enabled = false; // スクリプトを無効にする
            return;
        }

        // 初期HPを設定
        UpdateHPBar();
    }

    void Update()
    {
        // HPバーの位置を敵の頭上に追従させる
        // transform.positionはHPバーCanvasのRectTransformの位置になる
        transform.position = transform.parent.position + offset;

        // HPバーを常にカメラの方へ向ける (LookAtメソッドはZ軸がカメラの方向を向くように回転させる)
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);

        // HPバーの値を更新
        UpdateHPBar();

        // 敵のHPが0になったらHPバーも非表示にする
        if (enemyHealth.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    // HPバーの値を更新するメソッド
    void UpdateHPBar()
    {
        // currentHealth / maxHealth でHPの割合を計算し、Sliderのvalueに設定
        hpSlider.value = enemyHealth.currentHealth / enemyHealth.maxHealth;
    }
}