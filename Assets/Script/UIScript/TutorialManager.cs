using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class TutoialManager : MonoBehaviour
{
    public enum TutorialStep
    {
        Move, Dash, Fly, GravityPanel, Damage, LockOn, Attack1, SwitchWeapon, Attack2, SwitchArmor, Complete
    }

    [Header("UI References")]
    public Text missionText;
    public GameObject checkmarkIcon;

    [Header("Player References")]
    public PlayerModesAndVisuals modesAndVisuals;
    public PlayerStatus playerStatus;

    [Header("Tutorial Gimmicks")]
    public GameObject gravityPanelObject;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    private GameObject _currentEnemy;

    [Header("Scene Transition")]
    public string nextSceneName = "GameClearScene";
    public float transitionDelay = 3.0f;

    private TPSCameraController _tpsCam;
    private TutorialStep currentStep = TutorialStep.Move;
    private bool stepCleared = false;
    private bool enemyIsDead = false;
    private bool isSpawning = false; // 生成中の重複防止
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
        if (stepCleared || isSpawning) return;

        switch (currentStep)
        {
            case TutorialStep.Move:
                if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f) OnActionSuccess();
                break;
            case TutorialStep.Dash:
                if (Input.GetKey(KeyCode.LeftShift) && (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)) OnActionSuccess();
                break;
            case TutorialStep.Fly:
                if (Input.GetKey(KeyCode.Space)) OnActionSuccess();
                break;
            case TutorialStep.GravityPanel:
                if (playerStatus != null && playerStatus.isMovementSlowed) OnActionSuccess();
                break;
            case TutorialStep.Damage:
                if (_currentEnemy == null) SpawnTutorialEnemy(true);
                if (playerStatus != null && playerStatus.CurrentHP < initialHp) OnActionSuccess();
                break;
            case TutorialStep.LockOn:
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                if (_tpsCam != null && _tpsCam.LockOnTarget != null) OnActionSuccess();
                break;
            case TutorialStep.Attack1:
                // enemyIsDeadフラグが立つか、敵がシーンから消えたら成功
                if (enemyIsDead || (_currentEnemy == null && !isSpawning))
                {
                    OnActionSuccess();
                }
                break;
            case TutorialStep.SwitchWeapon:
                // 【修正】Eキーを押した、あるいは「すでにAttack2モードになっている」なら合格
                bool isAttack2Now = modesAndVisuals != null && modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2;
                if (Input.GetKeyDown(KeyCode.E) || isAttack2Now)
                {
                    OnActionSuccess();
                }
                break;

            case TutorialStep.Attack2:
                if (_currentEnemy == null && !isSpawning) SpawnTutorialEnemy(false);

                bool isCorrectMode = modesAndVisuals != null && modesAndVisuals.CurrentWeaponMode == PlayerModesAndVisuals.WeaponMode.Attack2;

                // 敵が死んだ（または消滅した）判定
                if (enemyIsDead || (_currentEnemy == null && !isSpawning))
                {
                    if (isCorrectMode)
                    {
                        OnActionSuccess();
                    }
                    else
                    {
                        // 違うモードで倒してしまった場合
                        enemyIsDead = false;
                        StartCoroutine(RespawnMessage());
                    }
                }
                break;

            case TutorialStep.SwitchArmor:
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2)) OnActionSuccess();
                break;
        }
    }

    private void OnActionSuccess()
    {
        stepCleared = true;
        enemyIsDead = false; // 次のステップのためにリセット
        StartCoroutine(ShowCheckmarkAndNext());
    }

    IEnumerator RespawnMessage()
    {
        missionText.text = "<color=red>Eキーで武装を切り替えてから\n攻撃してください！</color>";
        yield return new WaitForSeconds(1.0f);
        SpawnTutorialEnemy(false);
        UpdateTutorialUI();
    }

    IEnumerator ShowCheckmarkAndNext()
    {
        checkmarkIcon.SetActive(true);
        missionText.color = Color.green;
        yield return new WaitForSeconds(1.5f);

        if (currentStep != TutorialStep.SwitchArmor) checkmarkIcon.SetActive(false);

        if (currentStep != TutorialStep.Complete)
        {
            currentStep++;
            enemyIsDead = false; // 確実にリセット
            UpdateTutorialUI();
            stepCleared = false;
        }
    }

    IEnumerator HideGravityPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gravityPanelObject != null && currentStep != TutorialStep.GravityPanel)
        {
            gravityPanelObject.SetActive(false);
        }
    }

    void UpdateTutorialUI()
    {
        if (playerStatus != null) initialHp = playerStatus.CurrentHP;

        if (gravityPanelObject != null)
        {
            if (currentStep == TutorialStep.GravityPanel) gravityPanelObject.SetActive(true);
            else if (currentStep == TutorialStep.Damage) StartCoroutine(HideGravityPanelAfterDelay(1.5f));
        }

        switch (currentStep)
        {
            case TutorialStep.Move: missionText.text = "・WASDで\n移動せよ"; break;
            case TutorialStep.Dash: missionText.text = "・LeftShift+\nWASDで\nダッシュせよ"; break;
            case TutorialStep.Fly: missionText.text = "・Spaceで\n上昇せよ"; break;
            case TutorialStep.GravityPanel: missionText.text = "・重力パネルに触れ\n　速度低下を体験せよ"; break;
            case TutorialStep.Damage: missionText.text = "・敵の攻撃を受け\n　ダメージを体験せよ"; break;
            case TutorialStep.LockOn: missionText.text = "・右クリックで敵を\n　ロックオンせよ"; break;
            case TutorialStep.Attack1:
                missionText.text = "・左クリックで\n　攻撃しましょう";
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                break;
            case TutorialStep.SwitchWeapon:
                missionText.text = "・Eキーで武器を切り替えろ";
                break;
            case TutorialStep.Attack2:
                // ステップ開始時に一度だけリセット
                enemyIsDead = false;
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                // メッセージは Update メソッド内で動的に更新されるため、ここでは初期値のみ
                missionText.text = "・切り替えた武装で\n　敵を撃破せよ";
                break;
            case TutorialStep.SwitchArmor: missionText.text = "・1,2キー\nでアーマー換装"; break;
            case TutorialStep.Complete:
                missionText.color = Color.green;
                checkmarkIcon.SetActive(true);
                missionText.text = "・全ミッション完了！";
                if (_currentEnemy != null) Destroy(_currentEnemy);
                StartCoroutine(TransitionToClearScene());
                break;
        }
        if (currentStep != TutorialStep.Complete) missionText.color = Color.white;
    }

    private void SpawnTutorialEnemy(bool enableAttack)
    {
        if (enemyPrefab != null && spawnPoint != null)
        {
            isSpawning = true;
            if (_currentEnemy != null) Destroy(_currentEnemy);
            _currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            enemyIsDead = false;
            if (_currentEnemy.TryGetComponent<TutorialEnemyController>(out var enemyCtrl))
            {
                enemyCtrl.canShootBeam = enableAttack;
                enemyCtrl.onDeath += () => { enemyIsDead = true; };
            }
            isSpawning = false;
        }
    }

    IEnumerator TransitionToClearScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(nextSceneName);
    }
}