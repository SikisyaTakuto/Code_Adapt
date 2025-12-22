using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class TutoialManager : MonoBehaviour
{
    public enum TutorialStep
    {
        Move,
        Dash,
        Fly,
        Damage,
        LockOn,
        Attack1,        // 「攻撃しましょう」
        SwitchWeapon,
        Attack2,        // 「切り替えた武装で攻撃してください」
        SwitchArmor,
        Complete
    }

    [Header("UI References")]
    public Text missionText;
    public GameObject checkmarkIcon;

    [Header("Player References")]
    public PlayerModesAndVisuals modesAndVisuals;
    public PlayerStatus playerStatus;

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
                bool hasMovementInput = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
                if (Input.GetKey(KeyCode.LeftShift) && hasMovementInput)
                    OnActionSuccess();
                break;

            case TutorialStep.Fly:
                if (Input.GetKey(KeyCode.Space))
                    OnActionSuccess();
                break;

            case TutorialStep.Damage:
                if (_currentEnemy == null) SpawnTutorialEnemy(true);
                if (playerStatus != null && playerStatus.CurrentHP < initialHp)
                    OnActionSuccess();
                break;

            case TutorialStep.LockOn:
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                if (_tpsCam != null && _tpsCam.LockOnTarget != null)
                    OnActionSuccess();
                break;

            case TutorialStep.Attack1:
                if (enemyIsDead) OnActionSuccess();
                break;

            case TutorialStep.SwitchWeapon:
                if (Input.GetKeyDown(KeyCode.E))
                    OnActionSuccess();
                break;

            case TutorialStep.Attack2:
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                if (enemyIsDead) OnActionSuccess();
                break;

            case TutorialStep.SwitchArmor:
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
                    OnActionSuccess();
                break;
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

        if (currentStep != TutorialStep.SwitchArmor) checkmarkIcon.SetActive(false);

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
        if (playerStatus != null) initialHp = playerStatus.CurrentHP;

        switch (currentStep)
        {
            case TutorialStep.Move: message = "・WASDで\n移動せよ"; break;
            case TutorialStep.Dash: message = "・LeftShift+\nWASDで\nダッシュせよ"; break;
            case TutorialStep.Fly: message = "・Spaceで\n上昇せよ"; break;
            case TutorialStep.Damage: message = "・敵の攻撃を受け\n　ダメージを体験せよ"; break;
            case TutorialStep.LockOn: message = "・右クリックで敵を\n　ロックオンせよ"; break;

            case TutorialStep.Attack1:
                // ★修正箇所
                message = "・左クリックで\n　攻撃しましょう";
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                break;

            case TutorialStep.SwitchWeapon:
                message = "・Eキーで武器を切り替えろ";
                break;

            case TutorialStep.Attack2:
                // ★修正箇所
                message = "・切り替えた武装で\n　攻撃してください";
                if (_currentEnemy == null) SpawnTutorialEnemy(false);
                break;

            case TutorialStep.SwitchArmor:
                message = "・1,2キー\nでアーマー換装";
                break;

            case TutorialStep.Complete:
                missionText.color = Color.green;
                checkmarkIcon.SetActive(true);
                message = "・全ミッション完了！";
                if (_currentEnemy != null) Destroy(_currentEnemy);
                StartCoroutine(TransitionToClearScene());
                break;
        }

        if (currentStep != TutorialStep.Complete) missionText.color = Color.white;
        missionText.text = message;
    }

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
                enemyCtrl.onDeath += () => { enemyIsDead = true; };
            }
        }
    }

    IEnumerator TransitionToClearScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(nextSceneName);
    }
}