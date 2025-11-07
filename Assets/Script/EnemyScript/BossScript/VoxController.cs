using UnityEngine;

public class VoxController : MonoBehaviour
{
    [System.Serializable]
    private class ArmData
    {
        public GameObject arm;   // 実際のオブジェクト
        public float targetZ;    // 目的Z座標
        public bool isMoving;    // 動いているかどうか
    }

    [SerializeField] private GameObject Arms1;
    [SerializeField] private GameObject Arms2;
    [SerializeField] private GameObject Arms3;
    [SerializeField] private GameObject Arms4;

    private ArmData[] armsData;

    [SerializeField] private float moveSpeed = 6f;

    //private GameObject[] armsArray;
    //private GameObject activeArm;   // 現在選ばれているオブジェクト
    //[SerializeField] private float moveSpeed = 2f; // 移動速度

    //private float targetZ;   // 目標Z座標
    //private bool isMoving = false;  // 移動中かどうか

    void Start()
    {
        // 4つのアームを登録
        armsData = new ArmData[4];
        armsData[0] = new ArmData { arm = Arms1 };
        armsData[1] = new ArmData { arm = Arms2 };
        armsData[2] = new ArmData { arm = Arms3 };
        armsData[3] = new ArmData { arm = Arms4 };
        //// 配列にまとめる
        //armsArray = new GameObject[] { Arms1, Arms2, Arms3, Arms4 };

        //// 最初に1つランダム選択
        //SelectRandomArm();
    }

    void Update()
    {
        // スペースキーを押すとランダムに選び直す
        if (Input.GetKeyDown(KeyCode.P))
        {
            {
                ActivateRandomArm();
            }

            // 各アームを個別に移動処理
            foreach (var data in armsData)
            {
                if (data.isMoving && data.arm != null)
                {
                    Vector3 pos = data.arm.transform.position;
                    float step = moveSpeed * Time.deltaTime;
                    pos.z = Mathf.MoveTowards(pos.z, data.targetZ, step);
                    data.arm.transform.position = pos;

                    if (Mathf.Approximately(pos.z, data.targetZ))
                    {
                        data.isMoving = false;
                        Debug.Log($"{data.arm.name} が Z={data.targetZ:F2} に到達しました！");
                    }
                }
            }
        }

    void ActivateRandomArm()
    {
        int index = Random.Range(0, armsData.Length);
        var data = armsData[index];

        // ランダムなZ座標（-110〜-255）
        data.targetZ = Random.Range(-255f, -110f);
        data.isMoving = true;

        Debug.Log($"選ばれた: {data.arm.name} | 目標Z: {data.targetZ:F2}");
    }
  
        //int randomNumber = Random.Range(0, 4); // 0〜3
        //activeArm = armsArray[randomNumber];

        //// Z座標の目標値をランダムで決定（-110〜-255）
        //targetZ = Random.Range(-255f, -110f);

        //// 移動を再開
        //isMoving = true;

        //Debug.Log($"選ばれたオブジェクト: {activeArm.name} | 目標Z: {targetZ:F2}");
    }
}
