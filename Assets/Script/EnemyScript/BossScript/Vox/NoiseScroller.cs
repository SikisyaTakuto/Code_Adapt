using UnityEngine;
using System.Collections.Generic;

public class MaterialGlitcher : MonoBehaviour
{
    private Renderer _renderer;

    [Header("マテリアル設定")]
    [SerializeField] private List<Material> _noiseMaterials;

    [Header("切り替え設定")]
    [SerializeField] private float _minSwitchTime = 0.05f;
    [SerializeField] private float _maxSwitchTime = 0.15f;

    // 一般的なテクスチャプロパティ名の候補リスト
    private readonly string[] _texturePropNames = { "_BaseMap", "_MainTex", "_BaseColorMap" };
    private float _timer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_noiseMaterials == null || _noiseMaterials.Count == 0)
        {
            Debug.LogWarning("Noise Materials がセットされていません。");
            enabled = false;
        }
    }

    void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            SwitchMaterial();
            _timer = Random.Range(_minSwitchTime, _maxSwitchTime);
        }
    }

    private void SwitchMaterial()
    {
        int index = Random.Range(0, _noiseMaterials.Count);
        Material nextMat = _noiseMaterials[index];

        // マテリアルを適用
        _renderer.material = nextMat;

        // プロパティが存在する場合のみ、ランダムなオフセットを適用
        foreach (string prop in _texturePropNames)
        {
            if (nextMat.HasProperty(prop))
            {
                Vector2 randomOffset = new Vector2(Random.value, Random.value);
                _renderer.material.SetTextureOffset(prop, randomOffset);
                break; // 見つかったらループを抜ける
            }
        }

        // 明るさ（色）の変更も安全に行う
        if (nextMat.HasProperty("_BaseColor"))
        {
            float brightness = Random.Range(0.8f, 1.2f);
            _renderer.material.SetColor("_BaseColor", Color.white * brightness);
        }
    }
}