using UnityEngine;

public class PlayerBitController : MonoBehaviour
{
    public GameObject bitPrefab;
    public Transform[] bitPositions = new Transform[4];  // プレイヤー後方のビット待機位置
    private BitBehavior[] bits = new BitBehavior[4];
    private int nextBitIndex = 0;

    void Start()
    {
        // ビットを生成して待機位置にセット、ビット配列に保持
        for (int i = 0; i < bitPositions.Length; i++)
        {
            GameObject bitObj = Instantiate(bitPrefab, bitPositions[i].position, Quaternion.identity);
            BitBehavior bit = bitObj.GetComponent<BitBehavior>();
            if (bit != null)
            {
                bit.Initialize(this, bitPositions[i]);
                bits[i] = bit;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            LaunchNextBit();
        }
    }

    void LaunchNextBit()
    {
        // 射出可能なビットを探す（待機状態のビットのみ）
        for (int i = 0; i < bits.Length; i++)
        {
            int index = (nextBitIndex + i) % bits.Length;
            if (bits[index] != null && bits[index].IsIdle())
            {
                bits[index].Launch();
                nextBitIndex = (index + 1) % bits.Length;
                break;
            }
        }
    }
}
