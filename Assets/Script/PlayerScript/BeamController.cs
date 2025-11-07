using UnityEngine;

/// <summary>
/// ビームの視覚効果（エフェクト/モデル）と持続時間を制御します。
/// ビーム本体はZ軸方向に伸びるように設計されている必要があります。
/// </summary>
public class BeamController : MonoBehaviour
{
    [Tooltip("ビームの持続時間")]
    public float lifetime = 0.5f;

    //[Tooltip("ビームが当たった時に発生する着弾エフェクトのプレハブ (オプション)")]
    //public GameObject impactEffectPrefab;

    [Tooltip("ビーム本体のエフェクトまたはモデル（これをZ軸方向にスケールする）")]
    public Transform beamVisual; // ビームの視覚要素（子オブジェクト）

    void Awake()
    {
        // 必須参照の確認（エディタでの設定ミス防止）
        if (beamVisual == null)
        {
            Debug.LogError("BeamController: Beam Visual (ビーム本体) が設定されていません。");
            // スクリプトを無効化する代わりに、エラーをログに出すだけに留めます
        }
    }

    /// <summary>
    /// ビームの始点と終点を設定し、表示を開始します。
    /// </summary>
    /// <param name="startPoint">ビームの始点 (通常は銃口)</param>
    /// <param name="endPoint">ビームの終点 (Raycastのヒット位置)</param>
    /// <param name="didHit">何かに当たったかどうか</param>
    public void Fire(Vector3 startPoint, Vector3 endPoint, bool didHit)
    {
        // 視覚要素が設定されていない場合は処理を中断
        if (beamVisual == null)
        {
            Destroy(gameObject, 0.1f); // エラーの場合もすぐに破棄
            return;
        }

        // 1. ビームの長さを計算
        float distance = Vector3.Distance(startPoint, endPoint);

        // 2. ビーム本体の位置・回転・スケールを設定
        Vector3 localScale = beamVisual.localScale;
        localScale.z = distance;
        beamVisual.localScale = localScale;

        //// 3. 着弾エフェクトの生成
        //if (didHit && impactEffectPrefab != null)
        //{
        //    // 終点位置にエフェクトを生成
        //    // ビームの反対側を向かせることで、ヒット面に対して垂直になるようにする
        //    GameObject impactInstance = Instantiate(
        //        impactEffectPrefab,
        //        endPoint,
        //        Quaternion.LookRotation((startPoint - endPoint).normalized)
        //    );

        //    // ★修正点: 生成した着弾エフェクトを一定時間後に破棄します
        //    // ParticleSystemのDurationに合わせて時間を設定するのが理想ですが、
        //    // ここでは簡易的にビーム本体と同じ lifetime を使用します。
        //    Destroy(impactInstance, lifetime);
        //}

        // 4. 一定時間後に自身を破棄
        Destroy(gameObject, lifetime);
    }
}