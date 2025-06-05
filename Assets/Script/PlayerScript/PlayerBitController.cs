using UnityEngine;

public class PlayerBitController : MonoBehaviour
{
    // ビットのプレハブ（インスペクターで設定）
    public GameObject bitPrefab;

    // プレイヤー後方に配置するビットの待機位置（Transform配列、サイズは4）
    public Transform[] bitPositions = new Transform[4];

    // 生成されたビットのスクリプト参照を格納する配列（サイズは4）
    private BitBehavior[] bits = new BitBehavior[4];

    // 次に射出するビットのインデックスを管理
    private int nextBitIndex = 0;

    void Start()
    {
        // ビットを生成し、それぞれの待機位置にセットするループ
        for (int i = 0; i < bitPositions.Length; i++)
        {
            // bitPrefabからビットオブジェクトを生成し、待機位置の座標に配置
            GameObject bitObj = Instantiate(bitPrefab, bitPositions[i].position, Quaternion.identity);

            // 生成したオブジェクトからBitBehaviorコンポーネントを取得
            BitBehavior bit = bitObj.GetComponent<BitBehavior>();

            // BitBehaviorが存在すれば初期化処理を行い、配列に保持
            if (bit != null)
            {
                // ビットにこのコントローラーと待機位置を渡して初期化
                bit.Initialize(this, bitPositions[i]);

                // bits配列に登録
                bits[i] = bit;
            }
        }
    }

    void Update()
    {
        // 毎フレーム、Gキーが押されたかをチェック
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Gキーが押されたら次のビットを射出する
            LaunchNextBit();
        }
    }

    // 射出可能な次のビットを探して射出処理を行うメソッド
    void LaunchNextBit()
    {
        // ビット配列を順番に調べるループ
        // nextBitIndexから始めて順にチェックし、待機状態のビットを見つける
        for (int i = 0; i < bits.Length; i++)
        {
            // 配列のインデックスをループさせるための計算
            int index = (nextBitIndex + i) % bits.Length;

            // ビットが存在していて、かつ待機中（射出可能）であれば
            if (bits[index] != null && bits[index].IsIdle())
            {
                // 該当ビットを射出する
                bits[index].Launch();

                // 次回はその次のビットから探すようにインデックスを更新
                nextBitIndex = (index + 1) % bits.Length;

                // 射出完了したのでループを抜ける
                break;
            }
        }
    }
}
