using UnityEngine;
using System.Collections;

public class BeamController : MonoBehaviour
{
    // ビームエフェクトのルートにある全てのParticle System
    private ParticleSystem[] particles;

    // 【設定項目】ビームの表示時間
    [Header("ビームの持続時間")]
    public float beamDuration = 0.1f;

    void Awake()
    {
        // ゲームオブジェクトとその全ての子オブジェクトからParticleSystemを取得
        particles = GetComponentsInChildren<ParticleSystem>();

        if (particles.Length == 0)
        {
            Debug.LogError("BeamController: 子を含むParticleSystemが見つかりません。");
            enabled = false;
        }

        // 初期状態でエフェクトを停止しておく
        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>ビームを発射し、同時に自動消滅処理を開始します。</summary>
    public void Fire(Vector3 startPoint, Vector3 endPoint)
    {
        // Line Rendererがないため、ビームの位置と方向を親オブジェクトで設定します
        // startPoint (発射元) をこのゲームオブジェクトの位置として設定します
        transform.position = startPoint;

        // 方向を設定 (endPointが正確な着弾点の場合)
        // ビームがstartPointからendPointを向くように回転させる
        Vector3 direction = endPoint - startPoint;
        transform.rotation = Quaternion.LookRotation(direction);

        // ※ Particle Systemが持つ "Shape" モジュールの設定で、
        // ビームが適切にstartPointからendPointまで伸びるように調整が必要です。

        // 1. 全てのParticle Systemを再生開始
        foreach (var ps in particles)
        {
            ps.Play();
        }

        // 2. 設定された時間後にこのゲームオブジェクト全体を破棄する
        Destroy(gameObject, beamDuration);
    }
}