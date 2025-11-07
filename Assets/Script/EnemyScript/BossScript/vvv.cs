using UnityEngine;

public class vvv : MonoBehaviour
{
    [SerializeField] private GameObject Arms1;
    [SerializeField] private GameObject Arms2;
    [SerializeField] private GameObject Arms3;
    [SerializeField] private GameObject Arms4;

    private GameObject[] armsArray;
    private GameObject activeArm;   // 現在選ばれているオブジェクト
    [SerializeField] private float moveSpeed = 2f; // 移動速度

    private float targetZ;   // 目標Z座標
    private bool isMoving = false; // 移動中フラグ

    void Start()
    {
        armsArray = new GameObject[] { Arms1, Arms2, Arms3, Arms4 };
        SelectRandomArm();
    }

    void Update()
    {
        // スペースキーで別のオブジェクトをランダム選択＆再スタート
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SelectRandomArm();
        }

        // 選ばれたオブジェクトを動かす処理
        if (isMoving && activeArm != null)
        {
            // 現在位置
            Vector3 currentPos = activeArm.transform.position;

            // 目標地点までの補間移動（一定速度で）
            float step = moveSpeed * Time.deltaTime;
            currentPos.z = Mathf.MoveTowards(currentPos.z, targetZ, step);
            activeArm.transform.position = currentPos;

            // 到達チェック
            if (Mathf.Approximately(currentPos.z, targetZ))
            {
                isMoving = false;
                Debug.Log($"{activeArm.name} が Z={targetZ:F2} に到達しました！");
            }
        }
    }

    void SelectRandomArm()
    {
        // 1〜4のオブジェクトからランダム選択
        int randomNumber = Random.Range(0, 4);
        activeArm = armsArray[randomNumber];

        // Z座標の目標値をランダムで決定（-110〜-255）
        targetZ = Random.Range(-255f, -110f);

        // 移動を再開
        isMoving = true;

        Debug.Log($"選ばれたオブジェクト: {activeArm.name} | 目標Z: {targetZ:F2}");
    }
}
