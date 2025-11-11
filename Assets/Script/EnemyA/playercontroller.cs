using UnityEngine;

public class playerController : MonoBehaviour
{
    // プレイヤーの移動速度
    public float moveSpeed = 5.0f;

    // プレイヤーの回転速度
    public float rotationSpeed = 500.0f;

    // CharacterControllerコンポーネントを格納する変数
    private CharacterController characterController;

    void Start()
    {
        // アタッチされているCharacterControllerコンポーネントを取得
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            Debug.LogError("PlayerControllerにはCharacterControllerが必要です。アタッチしてください。");
        }
    }

    void Update()
    {
        // 1. 入力の取得
        float horizontalInput = Input.GetAxis("Horizontal"); // A/Dキー または 左/右矢印キー
        float verticalInput = Input.GetAxis("Vertical");   // W/Sキー または 上/下矢印キー

        // 2. 移動方向ベクトルの計算
        // XZ平面での移動を想定し、Y軸（高さ）は0にする
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // 3. 移動処理
        if (moveDirection.magnitude >= 0.1f)
        {
            // CharacterControllerを使って移動を実行
            // Time.deltaTimeを掛けることで、フレームレートに依存しない移動速度にする
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

            // 4. 回転処理 (プレイヤーを進行方向に向ける)
            RotateToDirection(moveDirection);
        }
    }

    // プレイヤーを滑らかに進行方向へ回転させる関数
    private void RotateToDirection(Vector3 direction)
    {
        // 現在の方向と目標方向から、目標の回転（Quaternion）を計算
        // Quaternion.LookRotationは、Z軸がdirectionを向く回転を作成する
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // 現在の回転から目標の回転へ、滑らかに（Slerp）回転させる
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}