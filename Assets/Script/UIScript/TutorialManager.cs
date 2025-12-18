using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// チュートリアルの進行を管理するマネージャー。
/// プレイヤーの入力、ステータス、敵の状態を監視してミッションを進める。
/// </summary>
public class TutoialManager : MonoBehaviour
{
    public enum TutorialStep
    {
        Move,           // WASD移動
        Dash,           // エネルギー消費の説明
        Fly,            // 上昇
        Damage,         // 敵のビームを受ける体験
        LockOn,         // 敵をターゲット
        AttackMelee,    // 近接で破壊
        SwitchWeapon,   // Eキー
        AttackBeam,     // ビームで破壊
        SwitchArmor,    // 換装
        Complete        // 完了
    }

    [Header("UI References")]
    public Text missionText;
    public GameObject checkmarkIcon;

    [Header("Player References")]
    public PlayerModesAndVisuals modesAndVisuals;
    public PlayerStatus playerStatus; // HP/Energy参照用

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;    // TutorialEnemyControllerがアタッチされたプレハブ
    public Transform spawnPoint;
    private GameObject _currentEnemy;

    [Header("Scene Transition")]
    public string nextSceneName = "GameClearScene";
    public float transitionDelay = 3.0f;

    private TPSCameraController _tpsCam;
    private TutorialStep currentStep = TutorialStep.Move;
    private bool stepCleared = false;
    private bool enemyIsDead = false;

    // 判定用の保存変数
    private float initialHp;

    void Start()
    {
        _tpsCam = FindObjectOfType<TPSCameraController>();
        if (playerStatus == null) playerStatus = FindObjectOfType<PlayerStatus>();

        checkmarkIcon.SetActive(false);
        UpdateTutorialUI();
    }

    void Update()
    {
        if (stepCleared) return;

        switch (currentStep)
        {
            case TutorialStep.Move:
                if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)
                    OnActionSuccess();
                break;

            case TutorialStep.Dash:
                // Shift移動を検知
                if (Input.GetKey(KeyCode.LeftShift) && (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f))
                    OnActionSuccess();
                break;

            case TutorialStep.Fly:
                if (Input.GetKey(KeyCode.Space))
                    OnActionSuccess();
                break;

            case TutorialStep.Damage:
                // 敵を出し、ビーム攻撃を有効化する
                if (_currentEnemy == null) SpawnTutorialEnemy(enableAttack: true);

                // プレイヤーのHPが減ったらクリア
                if (playerStatus != null && playerStatus.CurrentHP < initialHp)
                    OnActionSuccess();
                break;

            case TutorialStep.LockOn:
                // 攻撃を受け終わったので一度敵を出し直す(攻撃停止)
                if (_currentEnemy == null) SpawnTutorialEnemy(enableAttack: false);

                if (_tpsCam != null && _tpsCam.LockOnTarget != null)
                    OnActionSuccess();
                break;

            case TutorialStep.AttackMelee:
                // 近接攻撃で敵がDie()を呼び、イベントが飛んできたら成功
                if (enemyIsDead) OnActionSuccess();
                break;

            case TutorialStep.SwitchWeapon:
                if (Input.GetKeyDown(KeyCode.E))
                    OnActionSuccess();
                break;

            case TutorialStep.AttackBeam:
                // 敵を出し直してビームで壊させる
                if (_currentEnemy == null) SpawnTutorialEnemy(enableAttack: false);
                if (enemyIsDead) OnActionSuccess();
                break;

            case TutorialStep.SwitchArmor:
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3))
                    OnActionSuccess();
                break;
        }
    }

    /// <summary>
    /// チュートリアル用の敵を生成し、死亡イベントを購読する。
    /// </summary>
    private void SpawnTutorialEnemy(bool enableAttack)
    {
        if (enemyPrefab != null && spawnPoint != null)
        {
            if (_currentEnemy != null) Destroy(_currentEnemy);

            _currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            enemyIsDead = false;

            if (_currentEnemy.TryGetComponent<TutorialEnemyController>(out var enemyCtrl))
            {
                enemyCtrl.canShootBeam = enableAttack;
                // 敵が死んだらフラグを立てる
                enemyCtrl.onDeath += () => { enemyIsDead = true; };
            }
        }
    }

    private void OnActionSuccess()
    {
        stepCleared = true;
        StartCoroutine(ShowCheckmarkAndNext());
    }

    IEnumerator ShowCheckmarkAndNext()
    {
        checkmarkIcon.SetActive(true);
        missionText.color = Color.green;

        yield return new WaitForSeconds(1.5f);

        // 次が Complete 以外なら一度アイコンを隠す
        if (currentStep != TutorialStep.SwitchArmor)
        {
            checkmarkIcon.SetActive(false);
        }

        if (currentStep != TutorialStep.Complete)
        {
            currentStep++;
            UpdateTutorialUI();
            stepCleared = false;
        }
    }

    void UpdateTutorialUI()
    {
        string message = "";

        // ステップ開始時のHPを記録（Damageステップの判定用）
        if (playerStatus != null) initialHp = playerStatus.CurrentHP;

        switch (currentStep)
        {
            case TutorialStep.Move: message = "・WASDで移動せよ"; break;
            case TutorialStep.Dash: message = "・Shiftでダッシュせよ\n　(エネルギーを消費)"; break;
            case TutorialStep.Fly: message = "・Spaceで上昇せよ"; break;
            case TutorialStep.Damage:
                message = "・敵の攻撃を受け\n　ダメージを体験せよ";
                break;
            case TutorialStep.LockOn:
                message = "・右クリックで敵を\n　ロックオンせよ";
                break;
            case TutorialStep.AttackMelee:
                message = "・近接攻撃で敵を破壊せよ";
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                break;
            case TutorialStep.SwitchWeapon:
                message = "・Eキーで武器を切り替えろ";
                break;
            case TutorialStep.AttackBeam:
                message = "・ビームで敵を破壊せよ";
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                break;
            case TutorialStep.SwitchArmor:
                message = "・1,2,3キーでアーマー換装";
                break;
            case TutorialStep.Complete:
                missionText.color = Color.green;
                checkmarkIcon.SetActive(true); // チェックマークを表示したままにする
                message = "・全ミッション完了！";
                if (_currentEnemy != null) Destroy(_currentEnemy);
                StartCoroutine(TransitionToClearScene());
                break;
        }

        if (currentStep != TutorialStep.Complete) missionText.color = Color.white;
        missionText.text = message;
    }

    IEnumerator TransitionToClearScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(nextSceneName);
    }
}