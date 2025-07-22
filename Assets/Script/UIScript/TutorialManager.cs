using UnityEngine;
using UnityEngine.UI; // Text, Canvas, Slider ���g������
using System.Collections;
<<<<<<< HEAD
using UnityEngine.SceneManagement; // SceneManager���g�p���邽�߂ɒǉ�
=======
>>>>>>> New

public class TutorialManager : MonoBehaviour
{
    public PlayerController playerController;
<<<<<<< HEAD
    public TPSCameraController tpsCameraController;
    public Text tutorialText; // �ꎞ�I�Ȑ����p�e�L�X�g
    public Text objectiveText; // �펞�\�������ڕW�e�L�X�g

    // �`���[�g���A���i�s�x�Q�[�W
    [Header("UI Elements")]
    [Tooltip("�`���[�g���A���̐i�s�x�������X���C�_�[�B")]
    public Slider tutorialProgressBar; // �`���[�g���A���i�s�x������UI�X���C�_�[

    // HP�Q�[�W��PlayerHealth�ւ̎Q��
    [Tooltip("�v���C���[��HP�Q�[�W�iSlider�j�ւ̎Q�ƁB")]
    public Slider hpSlider;
    [Tooltip("�v���C���[��PlayerHealth�X�N���v�g�ւ̎Q�ƁB")]
    public PlayerHealth playerHealth;

    public GameObject enemyPrefab;
    public Transform enemySpawnPoint;
    public GameObject armorModeEnemyPrefab;
    public Transform armorModeEnemySpawnPoint;

    [Header("Tutorial Camera Settings")]
    [Tooltip("�v���C���[�̐��ʂ���ǂꂭ�炢����邩")]
    public float tutorialCameraDistance = 3.0f;
    [Tooltip("�v���C���[�̊�_����ǂꂭ�炢�����ɃJ������u����")]
    public float tutorialCameraHeight = 1.5f;
    [Tooltip("�J�������v���C���[�̂ǂ̍��������邩�i�v���C���[�̒��S����̃I�t�Z�b�g�j")]
    public float tutorialCameraLookAtOffset = 1.0f;
    [Tooltip("�`���[�g���A���J�����ւ̐؂�ւ��A�܂���TPS�J�����ւ̕��A�̃X���[�Y��")]
    public float tutorialCameraSmoothTime = 0.1f; // �Ⴆ�΁A��葬�����邽�߂ɒl������������

    [Header("Enemy Reveal Camera Settings")]
    [Tooltip("�G�o�����ɃJ�������G����ǂꂭ�炢����邩")]
    public float enemyRevealCameraDistance = 15.0f; // �����ɐݒ肷�邽�߂̐V�����ϐ�
    [Tooltip("�G�o�����ɃJ�������G����ǂꂭ�炢�����Ɉʒu���邩")]
    public float enemyRevealCameraHeight = 5.0f; // �����ɐݒ肷�邽�߂̐V�����ϐ�

    // �`���[�g���A�������ς݃t���O
    [Header("Tutorial Explanations Flags")]
    private bool hasExplainedEnergyDepletion = false;
    private bool hasExplainedHPDamage = false;

    private GameObject currentEnemyInstance; // ���ݏo�����Ă���G�̃C���X�^���X
    private Image energyFillImage; // �G�l���M�[�Q�[�W��Fill Image

    private Coroutine energyBlinkCoroutine; // �G�l���M�[�Q�[�W�_�ŃR���[�`���̎Q��
=======
    public Text tutorialText; // TextMeshProUGUI �ɕύX
    public GameObject enemyPrefab; // �G��Prefab
    public Transform enemySpawnPoint; // �G�̏o���ʒu
    public GameObject armorModeEnemyPrefab; // �A�[�}�[���[�h�؂�ւ����ɏo��������G��Prefab
    public Transform armorModeEnemySpawnPoint; // �A�[�}�[���[�h�؂�ւ����ɏo��������G�̏o���ʒu

    private GameObject currentEnemyInstance; // ���ݏo�����Ă���G�̃C���X�^���X
>>>>>>> New

    // �`���[�g���A���X�e�b�v�̒�`
    private enum TutorialStep
    {
        Welcome,
        MoveWASD,
        Jump,
        Descend,
        ResetPosition,
        MeleeAttack,
        BeamAttack,
        SpecialAttack,
        ArmorModeSwitch,
        End
    }

    private TutorialStep currentStep = TutorialStep.Welcome;

    void Start()
    {
        if (playerController == null)
        {
            Debug.LogError("TutorialManager: PlayerController�����蓖�Ă��Ă��܂���B");
            return;
        }
        if (tutorialText == null)
        {
            Debug.LogError("TutorialManager: TutorialText (TextMeshProUGUI)�����蓖�Ă��Ă��܂���B");
            return;
        }
<<<<<<< HEAD
        if (objectiveText == null)
        {
            Debug.LogError("TutorialManager: ObjectiveText (TextMeshProProUGUI)�����蓖�Ă��Ă��܂���B");
            return;
        }

        if (tutorialProgressBar == null)
        {
            Debug.LogError("TutorialManager: Tutorial Progress Bar (Slider)�����蓖�Ă��Ă��܂���B");
            return;
        }
        tutorialProgressBar.gameObject.SetActive(false);
        tutorialText.gameObject.SetActive(false); // ������Ԃł͈ꎞ�����e�L�X�g�͔�\��

        // �G�l���M�[�Q�[�W��Fill Image���擾
        if (playerController != null && playerController.energySlider != null)
        {
            // Slider��Fill Area > Fill ��Image�R���|�[�l���g��T���̂���ʓI
            Transform fillArea = playerController.energySlider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    energyFillImage = fill.GetComponent<Image>();
                }
            }
            if (energyFillImage == null)
            {
                Debug.LogWarning("TutorialManager: Energy Slider Fill Image��������܂���B�G�l���M�[�Q�[�W�̓_�ł��@�\���܂���BEnergy Slider��Fill Area/Fill�I�u�W�F�N�g��Image�R���|�[�l���g�����邩�m�F���Ă��������B");
            }
        }
        else
        {
            Debug.LogError("TutorialManager: PlayerController�܂���Energy Slider�����蓖�Ă��Ă��Ȃ����߁A�G�l���M�[�Q�[�W�̓_�ł�ݒ�ł��܂���B");
        }

        // PlayerHealth�̎Q�Ƃ��擾
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("TutorialManager: PlayerHealth�����蓖�Ă��Ă��܂���BHP�_���[�W�̃`���[�g���A�����@�\���Ȃ��\��������܂��B");
            }
        }
        if (hpSlider == null)
        {
            Debug.LogWarning("TutorialManager: HP Slider�����蓖�Ă��Ă��܂���BHP�_���[�W�̃`���[�g���A�����@�\���Ȃ��\��������܂��B");
        }

        if (tpsCameraController == null)
        {
            tpsCameraController = FindObjectOfType<TPSCameraController>();
            if (tpsCameraController == null)
            {
                Debug.LogError("TutorialManager: TPSCameraController�����蓖�Ă��Ă��܂���B�V�[����TPSCameraController�����݂��邩�m�F���Ă��������B");
                return;
            }
        }

=======
>>>>>>> New
        if (enemyPrefab == null)
        {
            Debug.LogWarning("TutorialManager: Enemy Prefab�����蓖�Ă��Ă��܂���B�ߐ�/�r�[���U���̃`���[�g���A�����@�\���Ȃ��\��������܂��B");
        }
        if (enemySpawnPoint == null)
        {
            Debug.LogWarning("TutorialManager: Enemy Spawn Point�����蓖�Ă��Ă��܂���B�ߐ�/�r�[���U���̃`���[�g���A�����@�\���Ȃ��\��������܂��B");
        }
        if (armorModeEnemyPrefab == null)
        {
            Debug.LogWarning("TutorialManager: Armor Mode Enemy Prefab�����蓖�Ă��Ă��܂���B�A�[�}�[���[�h�؂�ւ��̃`���[�g���A�����@�\���Ȃ��\��������܂��B");
        }
        if (armorModeEnemySpawnPoint == null)
        {
            Debug.LogWarning("TutorialManager: Armor Mode Enemy Spawn Point�����蓖�Ă��Ă��܂���B�A�[�}�[���[�h�؂�ւ��̃`���[�g���A�����@�\���Ȃ��\��������܂��B");
        }

<<<<<<< HEAD
        // �C�x���g�w��
        if (playerController != null)
        {
            playerController.onEnergyDepleted += HandleEnergyDepletionTutorial;
        }
        if (playerHealth != null)
        {
            playerHealth.onHealthDamaged += HandleHPDamageTutorial;
        }

=======

        // �`���[�g���A���J�n
>>>>>>> New
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
<<<<<<< HEAD
        playerController.canReceiveInput = false;

        // �X�e�b�v1: �悤����
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("���̐��E�Ő����������߂̊�{���w�т܂��傤�B", 3.0f));
        UpdateObjectiveText("�悤�����I\n�`���[�g���A�����J�n���܂��B");


        // �X�e�b�v2: WASD�ړ�
        currentStep = TutorialStep.MoveWASD;
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�܂��͊�{����ł��B\nWASD�L�[���g���āA�ړ����Ă��������B", 3.0f));
        UpdateObjectiveText("�ڕW: WASD�L�[���g���Ĉړ����Ă��������B"); // �펞�ڕW���X�V
        ResetCameraToTPS();
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        yield return StartCoroutine(WaitForWASDMoveCompletion(5.0f));
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�f���炵���I�ړ�����̓o�b�`���ł��I", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // �Z���E�F�C�g

        // �X�e�b�v3: �X�y�[�X�L�[�Ŕ��
        currentStep = TutorialStep.Jump;
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("���ɁA�X�y�[�X�L�[�������Ĕ��ł݂܂��傤�I", 3.0f));
        UpdateObjectiveText("�ڕW: �X�y�[�X�L�[�������Ĕ��ł݂܂��傤�B"); // �펞�ڕW���X�V
        ResetCameraToTPS();
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        yield return StartCoroutine(WaitForJumpCompletion(3.0f));
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�㏸�����I�󒆂ł̈ړ����d�v�ł��B", 2.0f));
=======
        // �v���C���[�̓��͂��ꎞ�I�ɖ�����
        playerController.canReceiveInput = false;

        // �X�e�b�v1: �悤����
        yield return StartCoroutine(ShowMessage("�悤�����A�V�ăp�C���b�g�I\n���̐��E�Ő����������߂̊�{���w�т܂��傤�B", 3.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // �Z���Ԋu

        // �X�e�b�v2: WASD�ړ�
        currentStep = TutorialStep.MoveWASD;
        yield return StartCoroutine(ShowMessage("�܂��͊�{����ł��B\nWASD�L�[���g���āA5�b�Ԉړ����Ă��������B", 0)); // 0�͎��������Ȃ�
        playerController.canReceiveInput = true; // WASD���͎�t�J�n
        playerController.ResetInputTracking();

        float moveStartTime = Time.time;
        bool wasdCompleted = false;
        while (Time.time < moveStartTime + 5.0f)
        {
            if (playerController.WASDMoveTimer >= 5.0f) // �A������5�b�Ԉړ�������
            {
                wasdCompleted = true;
                break;
            }
            yield return null;
        }
        playerController.canReceiveInput = false; // ���͎�t��~
        if (wasdCompleted)
        {
            yield return StartCoroutine(ShowMessage("�f���炵���I�ړ�����̓o�b�`���ł��I", 2.0f));
        }
        else
        {
            yield return StartCoroutine(ShowMessage("WASD�L�[�ňړ��𑱂��Ă��������B���Ə����ł��B", 2.0f));
            yield return new WaitForSeconds(1f); // �����҂��Ă���Ď��s�𑣂�
            StartCoroutine(TutorialSequence()); // ���̃X�e�b�v�����蒼��
            yield break;
        }

        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v3: �X�y�[�X�L�[�Ŕ��
        currentStep = TutorialStep.Jump;
        yield return StartCoroutine(ShowMessage("���ɁA�X�y�[�X�L�[��������3�b�Ԕ��ł݂܂��傤�I", 0));
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        float jumpStartTime = Time.time;
        bool jumpCompleted = false;
        while (Time.time < jumpStartTime + 3.0f)
        {
            if (playerController.JumpTimer >= 3.0f)
            {
                jumpCompleted = true;
                break;
            }
            yield return null;
        }
        playerController.canReceiveInput = false;
        if (jumpCompleted)
        {
            yield return StartCoroutine(ShowMessage("�㏸�����I�󒆂ł̈ړ����d�v�ł��B", 2.0f));
        }
        else
        {
            yield return StartCoroutine(ShowMessage("�X�y�[�X�L�[�����������Ă��������B���Ə����Ŋ����ł��B", 2.0f));
            yield return new WaitForSeconds(1f);
            StartCoroutine(TutorialSequence());
            yield break;
        }
>>>>>>> New
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v4: �I���g�L�[�ŉ�����
        currentStep = TutorialStep.Descend;
<<<<<<< HEAD
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("���x��Alt�L�[�������ĉ��~���Ă݂܂��傤�B", 3.0f));
        UpdateObjectiveText("�ڕW: Alt�L�[�������ĉ��~���Ă݂܂��傤�B"); // �펞�ڕW���X�V
        ResetCameraToTPS();
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        yield return StartCoroutine(WaitForDescendCompletion(2.0f));
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("���~�������ł��I����Ŏ��R���݂ɔ�щ��܂��ˁB", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v5: ���S�ɖ߂�
        currentStep = TutorialStep.ResetPosition;
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�f���炵���I�ł́A���̈ʒu�ɖ߂��Ď��̌P���Ɉڂ�܂��傤�B", 3.0f));

        // �J�������v���C���[���ʂɐݒ肵�A�ړ�����������܂ő҂�
        SetCameraToPlayerFront();

        // �v���C���[���e���|�[�g���A�����ɃJ������TPS���[�h�ɖ߂�
        TeleportPlayer(Vector3.zero);
        ResetCameraToTPS(); // �J������TPS���[�h�ɖ߂�

        yield return StartCoroutine(WaitForPlayerAction(2.0f)); // �S�Ă̈ړ�������������̒Z���ҋ@


        // �X�e�b�v6: ���N���b�N�ŋߐڍU��
        currentStep = TutorialStep.MeleeAttack;
        objectiveText.gameObject.SetActive(false);
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            // �G�̏o���ʒu�ɃJ������������ (�������猩���낷�悤��)
            SetCameraToLookAtPosition(enemySpawnPoint.position, enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(enemyPrefab, enemySpawnPoint.position); // �V�����G���X�|�[��
            yield return StartCoroutine(ShowMessage("�ڂ̑O�ɓG������܂����B\n���N���b�N�ŋߐڍU�����ł��܂��B", 4.0f));
            UpdateObjectiveText("�ڕW: ���N���b�N�ŋߐڍU�����g���A�G��|���܂��傤�B"); // �펞�ڕW���X�V
        }
        else
        {
            yield return StartCoroutine(ShowMessage("�ߐڍU���̃`���[�g���A�����J�n���܂��B�i�GPrefab���ݒ肳��Ă��܂���j", 3.0f));
            UpdateObjectiveText("�ڕW: �ߐڍU���̃`���[�g���A���i�GPrefab�Ȃ��j�B"); // �펞�ڕW���X�V
        }
        // ���b�Z�[�W�\�����TPS�J�����ɖ߂�
        ResetCameraToTPS();

        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�����ȋߐڍU���ł����I", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v7: �E�N���b�N�Ńr�[���U��
        currentStep = TutorialStep.BeamAttack;
        objectiveText.gameObject.SetActive(false);
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            // �G�̏o���ʒu�ɃJ������������ (�������猩���낷�悤��)
            SetCameraToLookAtPosition(enemySpawnPoint.position + new Vector3(0, 0, 10), enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 10)); // �������ꂽ�ʒu�ɐV�����G���X�|�[��
            yield return StartCoroutine(ShowMessage("�������ꂽ�ʒu�ɓG���ďo�����܂����B\n�E�N���b�N�Ńr�[���U�����L���ł��B", 4.0f));
            UpdateObjectiveText("�ڕW: �E�N���b�N�Ńr�[���U�����g���A�G��|���܂��傤�B"); // �펞�ڕW���X�V
        }
        else
        {
            yield return StartCoroutine(ShowMessage("�r�[���U���̃`���[�g���A�����J�n���܂��B�i�GPrefab���ݒ肳��Ă��܂���j", 3.0f));
            UpdateObjectiveText("�ڕW: �r�[���U���̃`���[�g���A���i�GPrefab�Ȃ��j�B"); // �펞�ڕW���X�V
        }
        // ���b�Z�[�W�\�����TPS�J�����ɖ߂�
        ResetCameraToTPS();

        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�r�[���U���������ł��I\n����ŉ����̓G���|������܂���B", 2.0f));
=======
        yield return StartCoroutine(ShowMessage("���x��Alt�L�[��������2�b�ԉ��~���Ă݂܂��傤�B", 0));
        playerController.canReceiveInput = true;
        playerController.ResetInputTracking();
        float descendStartTime = Time.time;
        bool descendCompleted = false;
        while (Time.time < descendStartTime + 2.0f)
        {
            if (playerController.DescendTimer >= 2.0f)
            {
                descendCompleted = true;
                break;
            }
            yield return null;
        }
        playerController.canReceiveInput = false;
        if (descendCompleted)
        {
            yield return StartCoroutine(ShowMessage("���~�������ł��I����Ŏ��R���݂ɔ�щ��܂��ˁB", 2.0f));
        }
        else
        {
            yield return StartCoroutine(ShowMessage("Alt�L�[�����������Ă��������B���Ə����Ŋ����ł��B", 2.0f));
            yield return new WaitForSeconds(1f);
            StartCoroutine(TutorialSequence());
            yield break;
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v5: ���S�ɖ߂����G�o��
        currentStep = TutorialStep.ResetPosition;
        yield return StartCoroutine(ShowMessage("�f���炵���I�ł́A���̈ʒu�ɖ߂��Ď��̌P���Ɉڂ�܂��傤�B", 3.0f));
        TeleportPlayer(Vector3.zero); // �v���C���[�����_�ɖ߂�
        yield return new WaitForSeconds(2.0f); // �e���|�[�g�̉��o����

        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            yield return StartCoroutine(ShowMessage("�ڂ̑O�ɓG������܂����B\n�U���̏��������܂��傤�I", 3.0f));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position);
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v6: ���N���b�N�ŋߐڍU��
        currentStep = TutorialStep.MeleeAttack;
        yield return StartCoroutine(ShowMessage("���N���b�N�ŋߐڍU�����ł��܂��B\n�G���U�����Ă݂܂��傤�I", 0));
        playerController.canReceiveInput = true;
        bool meleeAttacked = false;
        playerController.onMeleeAttackPerformed += () => { meleeAttacked = true; };
        yield return new WaitUntil(() => meleeAttacked && currentEnemyInstance == null); // �U�����ēG��|���܂ő҂�
        playerController.canReceiveInput = false;
        playerController.onMeleeAttackPerformed -= () => { meleeAttacked = true; }; // �C�x���g�w�ǉ���

        yield return StartCoroutine(ShowMessage("�����ȋߐڍU���ł����I", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �G���ēx�o�� (�r�[���U���p)
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            yield return StartCoroutine(ShowMessage("�������ꂽ�ʒu�ɓG���ďo�����܂����B", 2.0f));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 10)); // �������ꂽ�ʒu
        }
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v7: �E�N���b�N�Ńr�[���U��
        currentStep = TutorialStep.BeamAttack;
        yield return StartCoroutine(ShowMessage("�������ꂽ�G�ɂ͉E�N���b�N�Ńr�[���U�����L���ł��B\n�G��|���܂��傤�I", 0));
        playerController.canReceiveInput = true;
        bool beamAttacked = false;
        playerController.onBeamAttackPerformed += () => { beamAttacked = true; };
        yield return new WaitUntil(() => beamAttacked && currentEnemyInstance == null); // �U�����ēG��|���܂ő҂�
        playerController.canReceiveInput = false;
        playerController.onBeamAttackPerformed -= () => { /*beamAttated = true;*/ }; // �C�x���g�w�ǉ���

        yield return StartCoroutine(ShowMessage("�r�[���U���������ł��I\n����ŉ����̓G���|������܂���B", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �G���ēx�o�� (����U���p)
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            yield return StartCoroutine(ShowMessage("�����̓G������܂����B", 2.0f));
            // �����o��������ꍇ�́A�K���Ȉʒu�ɐ��̔z�u���郍�W�b�N��ǉ�
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 5));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(5, 0, 5));
            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(-5, 0, 5));
        }
>>>>>>> New
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v8: �z�C�[�������݂œ���U��
        currentStep = TutorialStep.SpecialAttack;
<<<<<<< HEAD
        objectiveText.gameObject.SetActive(false);
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            // �G�̏o���ʒu�ɃJ������������ (�������猩���낷�悤��)
            SetCameraToLookAtPosition(enemySpawnPoint.position + new Vector3(0, 0, 5), enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(enemyPrefab, enemySpawnPoint.position + new Vector3(0, 0, 5));
            yield return StartCoroutine(ShowMessage("�G������܂����B\n�z�C�[���N���b�N�œ���U���������Ă݂܂��傤�B\n�����̓G�ɗL���ł��I", 4.0f));
            UpdateObjectiveText("�ڕW: �z�C�[���N���b�N�œ���U�����g���A�S�Ă̓G��|���܂��傤�B"); // �펞�ڕW���X�V
        }
        else
        {
            yield return StartCoroutine(ShowMessage("����U���̃`���[�g���A�����J�n���܂��B�i�GPrefab���ݒ肳��Ă��܂���j", 3.0f));
            UpdateObjectiveText("�ڕW: ����U���̃`���[�g���A���i�GPrefab�Ȃ��j�B"); // �펞�ڕW���X�V
        }
        // ���b�Z�[�W�\�����TPS�J�����ɖ߂�
        ResetCameraToTPS();

        playerController.canUseSwordBitAttack = true;
        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        playerController.canReceiveInput = false;
        playerController.canUseSwordBitAttack = false;

        objectiveText.gameObject.SetActive(false);
=======
        yield return StartCoroutine(ShowMessage("�Ō�ɁA�z�C�[���N���b�N�œ���U���������Ă݂܂��傤�B\n�����̓G�ɗL���ł��I", 0));
        playerController.canUseSwordBitAttack = true; // ����U����L���ɂ���
        playerController.canReceiveInput = true;
        bool bitAttacked = false;
        playerController.onBitAttackPerformed += () => { bitAttacked = true; };
        // �S�Ă̓G���|���܂ő҂�
        yield return new WaitUntil(() => bitAttacked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        playerController.canReceiveInput = false;
        playerController.onBitAttackPerformed -= () => { bitAttacked = true; }; // �C�x���g�w�ǉ���
        playerController.canUseSwordBitAttack = false; // ����U���𖳌��ɖ߂�

>>>>>>> New
        yield return StartCoroutine(ShowMessage("�f���炵���I����U�����g�����Ȃ��܂��ˁI", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �v���C���[���ēx���S�ɖ߂�
        currentStep = TutorialStep.ResetPosition;
<<<<<<< HEAD
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("��U�A�����ɖ߂�܂��傤�B", 2.0f));

        // �J�������v���C���[���ʂɐݒ肵�A�ړ�����������܂ő҂�
        SetCameraToPlayerFront();

        // �v���C���[���e���|�[�g���A�����ɃJ������TPS���[�h�ɖ߂�
        TeleportPlayer(Vector3.zero);
        ResetCameraToTPS(); // �J������TPS���[�h�ɖ߂�

        yield return StartCoroutine(WaitForPlayerAction(0.5f)); // �S�Ă̈ړ�������������̒Z���ҋ@


        // �X�e�b�v9: 1, 2, 3�ŃA�[�}�[���[�h�؂�ւ�
        currentStep = TutorialStep.ArmorModeSwitch;
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("�Ō�ɁA1, 2, 3�L�[�ŃA�[�}�[���[�h��؂�ւ��邱�Ƃ��ł��܂��B\n�D���ȃ��[�h�ɐ؂�ւ��Ă݂܂��傤�I", 4.0f));
        UpdateObjectiveText("�ڕW: 1, 2, 3�L�[�ŃA�[�}�[���[�h��؂�ւ��Ă݂܂��傤�B"); // �펞�ڕW���X�V
        ResetCameraToTPS();
        if (armorModeEnemyPrefab != null && armorModeEnemySpawnPoint != null)
        {
            // �G�̏o���ʒu�ɃJ������������ (�������猩���낷�悤��)
            SetCameraToLookAtPosition(armorModeEnemySpawnPoint.position, enemyRevealCameraDistance, enemyRevealCameraHeight, tutorialCameraSmoothTime);

            SpawnEnemy(armorModeEnemyPrefab, armorModeEnemySpawnPoint.position);
            objectiveText.gameObject.SetActive(false);
            yield return StartCoroutine(ShowMessage("�A�[�}�[���[�h��؂�ւ��āA�V�����G�ɒ���ł݂܂��傤�I", 2.0f));
            UpdateObjectiveText("�ڕW: �A�[�}�[���[�h��؂�ւ��āA�G��|���܂��傤�B"); // �펞�ڕW���X�V
        }
        // ���b�Z�[�W�\�����TPS�J�����ɖ߂�
        ResetCameraToTPS();

        playerController.canReceiveInput = true;
        yield return StartCoroutine(WaitForArmorModeChangeCompletion());

        if (armorModeEnemyPrefab != null && armorModeEnemySpawnPoint != null)
        {
            yield return StartCoroutine(WaitForEnemyDefeatCompletion());
        }

        playerController.canReceiveInput = false;

        objectiveText.gameObject.SetActive(false);
=======
        yield return StartCoroutine(ShowMessage("��U�A�����ɖ߂�܂��傤�B", 2.0f));
        TeleportPlayer(Vector3.zero);
        yield return new WaitForSeconds(1.0f);

        // �X�e�b�v9: 1, 2, 3�ŃA�[�}�[���[�h�؂�ւ�
        currentStep = TutorialStep.ArmorModeSwitch;
        yield return StartCoroutine(ShowMessage("�Ō�ɁA1, 2, 3�L�[�ŃA�[�}�[���[�h��؂�ւ��邱�Ƃ��ł��܂��B\n�D���ȃ��[�h�ɐ؂�ւ��Ă݂܂��傤�I", 0));
        // �v���C���[�̋߂��ɓG���o�������āA���[�h�؂�ւ��̗��R��^���邱�Ƃ��\
        if (armorModeEnemyPrefab != null && armorModeEnemySpawnPoint != null)
        {
            SpawnEnemy(armorModeEnemyPrefab, armorModeEnemySpawnPoint.position);
            yield return StartCoroutine(ShowMessage("�A�[�}�[���[�h��؂�ւ��āA�V�����G�ɒ���ł݂܂��傤�I", 2.0f));
        }

        playerController.canReceiveInput = true;
        bool armorModeChanged = false;
        playerController.onArmorModeChanged += (mode) => {
            Debug.Log($"Armor mode changed to: {mode}");
            armorModeChanged = true;
        };
        yield return new WaitUntil(() => armorModeChanged); // �����ꂩ�̃��[�h�ɐ؂�ւ��܂ő҂�
        playerController.canReceiveInput = false;
        playerController.onArmorModeChanged -= (mode) => { armorModeChanged = true; }; // �C�x���g�w�ǉ���

>>>>>>> New
        yield return StartCoroutine(ShowMessage("�A�[�}�[���[�h�̐؂�ւ������I\n�󋵂ɍ��킹�ă��[�h���g�������܂��傤�B", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v10: �`���[�g���A���I��
        currentStep = TutorialStep.End;
<<<<<<< HEAD
        SetCameraToPlayerFront();
        objectiveText.gameObject.SetActive(false);
        yield return StartCoroutine(ShowMessage("����Ŋ�{�P���͏I���ł��I\n�L��Ȑ��E�֔�ї����܂��傤�I", 5.0f));
        objectiveText.gameObject.SetActive(false); // �`���[�g���A���I������objectiveText���\���ɂ���ꍇ
        ResetCameraToTPS();
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        if (playerController != null)
        {
            playerController.canReceiveInput = true;
            Debug.Log("�`���[�g���A���I���B�v���C���[�̓��͂�L���ɂ��܂����B");
        }
        else
        {
            Debug.LogError("PlayerController��null�̂��߁A�`���[�g���A���I�����Ƀv���C���[�̓��͂�L���ɂł��܂���ł����B");
        }
    }

    /// <summary>
    /// ���b�Z�[�W��\�����A�w��b���҂B�Q�[���͈ꎞ��~���A�v���C���[���͖͂����������B
    /// ���̃��\�b�h��tutorialText���g�p���A�\����ɔ�A�N�e�B�u�ɂ���B
    /// </summary>
    /// <param name="message">�\�����郁�b�Z�[�W</param>
    /// <param name="duration">���b�Z�[�W�\�����ԁi�b�j�B���̎��Ԍo�ߌ�ɃQ�[���ĊJ�A���͗L�����B</param>
=======
        yield return StartCoroutine(ShowMessage("����Ŋ�{�P���͏I���ł��I\n�L��Ȑ��E�֔�ї����܂��傤�I", 5.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �`���[�g���A���I����̏����i��: ���C���Q�[���V�[���ւ̑J�ځj
        Debug.Log("�`���[�g���A���I���B");
        // �����Ƀ��C���Q�[���V�[�������[�h���鏈���Ȃǂ�ǉ�
        // ��: SceneManager.LoadScene("MainGameScene");
    }

    /// <summary>
    /// ���b�Z�[�W��\�����A�w��b���҂i0�b�̏ꍇ�̓v���C���[���͑҂��j
    /// </summary>
    /// <param name="message">�\�����郁�b�Z�[�W</param>
    /// <param name="duration">�\�����ԁi�b�j�B0�̏ꍇ�̓v���C���[���͑҂��B</param>
    /// <returns></returns>
>>>>>>> New
    IEnumerator ShowMessage(string message, float duration)
    {
        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);
<<<<<<< HEAD
        objectiveText.gameObject.SetActive(false); // tutorialText�\������objectiveText���\���ɂ���
        tutorialProgressBar.gameObject.SetActive(false); // ���b�Z�[�W���̓Q�[�W���\��
        Time.timeScale = 0f; // �Q�[�����ꎞ��~
        playerController.canReceiveInput = false; // ���͂𖳌���
        yield return null; // Time.timeScale�ύX��K�p���邽��1�t���[���҂�

        float unscaledTime = 0f;
        while (unscaledTime < duration)
        {
            unscaledTime += Time.unscaledDeltaTime; // UnscaledDeltaTime ���g�p���ă|�[�Y���Ɏ��Ԃ�i�߂�
            yield return null;
        }

        tutorialText.gameObject.SetActive(false); // ���b�Z�[�W�\����A��\���ɂ���
        Time.timeScale = 1f; // �Q�[�����ĊJ
        playerController.canReceiveInput = true; // ���͂�L����
        Debug.Log($"ShowMessage: Player input enabled after message. Current canReceiveInput: {playerController.canReceiveInput}", playerController.gameObject);
    }

    /// <summary>
    /// �펞�\�������ڕW�e�L�X�g���X�V����B
    /// </summary>
    /// <param name="objective">�\������ڕW�e�L�X�g</param>
    void UpdateObjectiveText(string objective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = objective;
            objectiveText.gameObject.SetActive(true); // ��ɕ\�����ێ�
        }
    }

    /// <summary>
    /// �w��b���ҋ@����iTime.timeScale�̉e�����󂯂Ȃ��j�B
    /// </summary>
    /// <param name="delay">�ҋ@���ԁi�b�j</param>
    IEnumerator WaitForPlayerAction(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
    }

    /// <summary>
    /// WASD�ړ����w�莞�Ԋ�������܂őҋ@���A�Q�[�W���X�V����B
    /// </summary>
    /// <param name="requiredTime">WASD�ړ����K�v�Ȏ��ԁi�b�j</param>
    IEnumerator WaitForWASDMoveCompletion(float requiredTime)
    {
        tutorialProgressBar.gameObject.SetActive(true); // �Q�[�W��\��
        tutorialProgressBar.value = 0;
        tutorialProgressBar.maxValue = requiredTime;
        Time.timeScale = 1f; // �Q�[�����ĊJ

        while (playerController.WASDMoveTimer < requiredTime)
        {
            tutorialProgressBar.value = playerController.WASDMoveTimer;
            yield return null;
        }
        tutorialProgressBar.gameObject.SetActive(false); // �Q�[�W���\��
    }

    /// <summary>
    /// �W�����v���w�莞�Ԋ�������܂őҋ@���A�Q�[�W���X�V����B
    /// </summary>
    /// <param name="requiredTime">�W�����v���K�v�Ȏ��ԁi�b�j</param>
    IEnumerator WaitForJumpCompletion(float requiredTime)
    {
        tutorialProgressBar.gameObject.SetActive(true);
        tutorialProgressBar.value = 0;
        tutorialProgressBar.maxValue = requiredTime;
        Time.timeScale = 1f; // �Q�[�����ĊJ

        while (playerController.JumpTimer < requiredTime)
        {
            tutorialProgressBar.value = playerController.JumpTimer;
            yield return null;
        }
        tutorialProgressBar.gameObject.SetActive(false);
    }

    /// <summary>
    /// ���~���w�莞�Ԋ�������܂őҋ@���A�Q�[�W���X�V����B
    /// </summary>
    /// <param name="requiredTime">���~���K�v�Ȏ��ԁi�b�j</param>
    IEnumerator WaitForDescendCompletion(float requiredTime)
    {
        tutorialProgressBar.gameObject.SetActive(true);
        tutorialProgressBar.value = 0;
        tutorialProgressBar.maxValue = requiredTime;
        Time.timeScale = 1f; // �Q�[�����ĊJ

        while (playerController.DescendTimer < requiredTime)
        {
            tutorialProgressBar.value = playerController.DescendTimer;
            yield return null;
        }
        tutorialProgressBar.gameObject.SetActive(false);
    }

    /// <summary>
    /// �G���S�ē|�����܂őҋ@����B
    /// (���̃R���[�`�����Ă΂ꂽ���_�� currentEnemyInstance �ɗL���ȓG���ݒ肳��Ă��邱�Ƃ�O��Ƃ��܂�)
    /// </summary>
    IEnumerator WaitForEnemyDefeatCompletion()
    {
        tutorialProgressBar.gameObject.SetActive(false); // �Q�[�W�͎g�p���Ȃ�
        Time.timeScale = 1f; // �Q�[�����ĊJ

        // currentEnemyInstance �� null �ɂȂ�܂őҋ@
        // SpawnEnemy �� currentEnemyInstance ���ݒ肳��AHandleEnemyDeath �� null �ɂȂ邱�Ƃ𗘗p
        while (currentEnemyInstance != null)
        {
            yield return null;
        }
        Debug.Log("WaitForEnemyDefeatCompletion: Enemy defeated, proceeding.");
    }

    /// <summary>
    /// �A�[�}�[���[�h���ύX�����܂őҋ@����B
    /// </summary>
    IEnumerator WaitForArmorModeChangeCompletion()
    {
        tutorialProgressBar.gameObject.SetActive(false); // �Q�[�W�͎g�p���Ȃ�
        Time.timeScale = 1f; // �Q�[�����ĊJ

        bool armorModeChanged = false;
        System.Action<int> onArmorModeChangedHandler = (mode) => { armorModeChanged = true; };
        playerController.onArmorModeChanged += onArmorModeChangedHandler;

        yield return new WaitUntil(() => armorModeChanged); // �t���O��true�ɂȂ�܂ő҂�

        playerController.onArmorModeChanged -= onArmorModeChangedHandler; // �C�x���g�w�ǂ�����
    }

    /// <summary>
    /// �J�������v���C���[�̐��ʂɐݒ肷��w���p�[���\�b�h
    /// </summary>
    void SetCameraToPlayerFront()
    {
        if (playerController == null || tpsCameraController == null) return;

        Vector3 playerPos = playerController.transform.position;
        Vector3 cameraLookAtPoint = playerPos + Vector3.up * tutorialCameraLookAtOffset;
        Vector3 cameraDesiredPos = playerPos + playerController.transform.forward * -tutorialCameraDistance + Vector3.up * tutorialCameraHeight;
        Quaternion cameraDesiredRot = Quaternion.LookRotation(cameraLookAtPoint - cameraDesiredPos);

        tpsCameraController.SetFixedCameraView(cameraDesiredPos, cameraDesiredRot, tutorialCameraSmoothTime);
        Debug.Log("�J�������v���C���[���ʂɐݒ肵�܂����B");
    }

    /// <summary>
    /// ����̃^�[�Q�b�g�ʒu�ɃJ������������w���p�[���\�b�h
    /// </summary>
    /// <param name="targetPosition">�J�����������^�[�Q�b�g�̈ʒu</param>
    /// <param name="distance">�^�[�Q�b�g����̋���</param>
    /// <param name="height">�^�[�Q�b�g����̍���</param>
    /// <param name="smoothTime">�X���[�Y�Ȉړ�����</param>
    void SetCameraToLookAtPosition(Vector3 targetPosition, float distance, float height, float smoothTime)
    {
        if (tpsCameraController == null) return;

        // �^�[�Q�b�g�𒆐S�ɁA������猩���낷�悤�Ȉʒu���v�Z
        Vector3 cameraDesiredPos = targetPosition + Vector3.back * distance + Vector3.up * height;
        Quaternion cameraDesiredRot = Quaternion.LookRotation(targetPosition - cameraDesiredPos);

        tpsCameraController.SetFixedCameraView(cameraDesiredPos, cameraDesiredRot, smoothTime);
        Debug.Log($"�J�������^�[�Q�b�g {targetPosition} �ɐݒ肵�܂����B");
    }

    /// <summary>
    /// �J������ʏ��TPS�Ǐ]���[�h�ɖ߂��w���p�[���\�b�h
    /// </summary>
    void ResetCameraToTPS()
    {
        if (tpsCameraController == null) return;
        tpsCameraController.ResetToTPSView(tutorialCameraSmoothTime);
        Debug.Log("�J������TPS���[�h�ɖ߂��܂����B");
=======
        Time.timeScale = 0f; // �Q�[�����ꎞ��~
        yield return null; // Time.timeScale�ύX��K�p���邽��1�t���[���҂�

        if (duration > 0)
        {
            float unscaledTime = 0f;
            while (unscaledTime < duration)
            {
                unscaledTime += Time.unscaledDeltaTime; // UnscaledDeltaTime ���g�p���ă|�[�Y���Ɏ��Ԃ�i�߂�
                yield return null;
            }
        }
        else // duration��0�̏ꍇ�A�v���C���[���N���b�N����܂ő҂�
        {
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return));
        }
        tutorialText.gameObject.SetActive(false);
        Time.timeScale = 1f; // �Q�[�����ĊJ
    }

    /// <summary>
    /// �Z���Ԋu�Ńv���C���[�Ɏv�l���Ԃ�^����
    /// </summary>
    /// <param name="delay">�x�����ԁi�b�j</param>
    /// <returns></returns>
    IEnumerator WaitForPlayerAction(float delay)
    {
        yield return new WaitForSeconds(delay);
>>>>>>> New
    }

    /// <summary>
    /// �v���C���[�����̈ʒu�Ƀe���|�[�g������
    /// </summary>
<<<<<<< HEAD
    /// <param name="position">�e���|�[�g��̈ʒu</param>
=======
    /// <param name="position"></param>
>>>>>>> New
    void TeleportPlayer(Vector3 position)
    {
        if (playerController != null)
        {
<<<<<<< HEAD
            CharacterController charController = playerController.gameObject.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                // �e���|�[�g�ʒu������Y�������ɏグ�邱�ƂŁA�n�ʂւ̂߂荞�݂�h��
                playerController.transform.position = position + Vector3.up * 0.1f; // 0.1f �͒����\
                charController.enabled = true;
                Debug.Log($"�v���C���[�� {playerController.transform.position} �Ƀe���|�[�g���܂����B");
            }
            else
            {
                playerController.transform.position = position + Vector3.up * 0.1f;
                Debug.LogWarning($"�v���C���[��CharacterController��������܂���B���ڈʒu��ݒ肵�܂����B");
            }
=======
            // CharacterController���g�p���Ă��邽�߁A����position��ݒ肷��̂ł͂Ȃ��A
            // Disable���Ĉʒu��ύX���A�ēxEnable�����@����邩�A
            // �܂���Controller.Move���g���Ĕ��ɒZ�����Ԃňړ�������Ȃǂ̍H�v���K�v�B
            // �����ł͊ȈՓI�ɁA��U�I�t�ɂ��Ĉʒu��ݒ�A�����ɃI���ɖ߂��B
            // �i�������A����ɂ��ꎞ�I�ɃR���W���������������\��������̂Œ��Ӂj
            playerController.gameObject.SetActive(false);
            playerController.transform.position = position;
            playerController.gameObject.SetActive(true);
            Debug.Log($"�v���C���[�� {position} �Ƀe���|�[�g���܂����B");
>>>>>>> New
        }
    }

    /// <summary>
<<<<<<< HEAD
    /// �G�𐶐�����B�����́uEnemy�v�^�O�̃I�u�W�F�N�g�͑S�Ĕj�������B
=======
    /// �G�𐶐�����B�����̓G������Δj������B
>>>>>>> New
    /// </summary>
    /// <param name="prefab">�G��Prefab</param>
    /// <param name="position">�o���ʒu</param>
    void SpawnEnemy(GameObject prefab, Vector3 position)
    {
<<<<<<< HEAD
        // ����: ���̏����́A�V�����G���X�|�[������O�ɁA�V�[�����̑S�ẮuEnemy�v�^�O�̕t�����I�u�W�F�N�g��j�����܂��B
        // ����ɂ��A�O�̃`���[�g���A���X�e�b�v�œ|������Ȃ������G��A�ȑO�ɃX�|�[�������G���N���A����܂��B
=======
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance); // �����̓G��j��
            currentEnemyInstance = null;
        }

        // ���łɑ��݂��Ă���S�Ă�Enemy�^�O�̃I�u�W�F�N�g��j��
>>>>>>> New
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            Destroy(enemy);
        }

        if (prefab != null)
        {
            currentEnemyInstance = Instantiate(prefab, position, Quaternion.identity);
<<<<<<< HEAD
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
=======
            // EnemyHealth�X�N���v�g���A�^�b�`����Ă��邱�Ƃ��m�F
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // Root�ɂȂ��ꍇ�́A�q�I�u�W�F�N�g���猟��
>>>>>>> New
                enemyHealth = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
<<<<<<< HEAD
=======
                // �G���|���ꂽ�Ƃ��ɃC�x���g���w�ǂł���悤�ɂ���
>>>>>>> New
                enemyHealth.onDeath += HandleEnemyDeath;
                Debug.Log($"�G�� {position} �ɏo�������܂����B");
            }
            else
            {
                Debug.LogWarning($"�o���������G '{prefab.name}' �� EnemyHealth �X�N���v�g��������܂���B");
            }
        }
    }

    /// <summary>
    /// �G���|���ꂽ�Ƃ��ɌĂяo�����n���h��
    /// </summary>
    private void HandleEnemyDeath()
    {
        Debug.Log("�G���|����܂����I");
<<<<<<< HEAD
        if (currentEnemyInstance != null)
        {
            EnemyHealth eh = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (eh == null)
            {
                eh = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }
            if (eh != null)
            {
                eh.onDeath -= HandleEnemyDeath; // �C�x���g�w�ǉ�����Y�ꂸ��
            }
            currentEnemyInstance = null; // �G���|���ꂽ��Q�Ƃ��N���A
        }
    }

    // �G�l���M�[�͊����̃`���[�g���A���n���h��
    void HandleEnergyDepletionTutorial()
    {
        if (hasExplainedEnergyDepletion) return; // ��x��������
        hasExplainedEnergyDepletion = true;

        StartCoroutine(EnergyDepletionSequence());
    }

    IEnumerator EnergyDepletionSequence()
    {
        // ���̓_�ŃR���[�`���������Ă���Β�~
        if (energyBlinkCoroutine != null) StopCoroutine(energyBlinkCoroutine);

        SetCameraToPlayerFront(); // �J�������v���C���[���ʂɐݒ�
        objectiveText.gameObject.SetActive(false);
        // ShowMessage��tutorialText���g�p���A���b�Z�[�W�\����ɂ͔�\���ɂ���
        yield return StartCoroutine(ShowMessage("�G�l���M�[���Ȃ��Ȃ�܂����I\n�u�[�X�g�����U���̓G�l���M�[������܂��B\n�G�l���M�[�͎��Ԃŉ񕜂��܂��B", 4.0f));

        // �G�l���M�[�Q�[�W�̓_�ł��J�n
        if (energyFillImage != null)
        {
            energyBlinkCoroutine = StartCoroutine(EnergyGaugeBlink(energyFillImage, 5.0f, 10.0f)); // 5�b�ԓ_�ŁA�_�ő��x10
        }

        yield return StartCoroutine(WaitForPlayerAction(2.0f)); // �v���C���[�Ɋώ@���Ԃ�^����

        // �_�ł��~���A���̐F�ɖ߂�
        if (energyBlinkCoroutine != null)
        {
            StopCoroutine(energyBlinkCoroutine);
            energyBlinkCoroutine = null;
        }
        if (energyFillImage != null)
        {
            energyFillImage.color = new Color(energyFillImage.color.r, energyFillImage.color.g, energyFillImage.color.b, 1f); // �A���t�@�l��1�ɖ߂�
        }
        ResetCameraToTPS(); // �J������TPS���[�h�ɖ߂�
    }

    // �G�l���M�[�Q�[�W�_�ŃR���[�`��
    IEnumerator EnergyGaugeBlink(Image targetImage, float blinkDuration, float blinkSpeed)
    {
        if (targetImage == null) yield break;

        Color originalColor = targetImage.color;
        float timer = 0f;
        while (timer < blinkDuration)
        {
            // Time.unscaledTime ���g�p���āATime.timeScale��0�ł��_�ł���悤�ɂ���
            float alpha = Mathf.Abs(Mathf.Sin(Time.unscaledTime * blinkSpeed));
            targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        targetImage.color = originalColor; // �_�ŏI����A���̐F�ɖ߂�
    }

    // HP�_���[�W���̃`���[�g���A���n���h��
    void HandleHPDamageTutorial()
    {
        if (hasExplainedHPDamage) return; // ��x��������
        hasExplainedHPDamage = true;

        StartCoroutine(HPDamageSequence());
    }

    IEnumerator HPDamageSequence()
    {
        SetCameraToPlayerFront(); // �J�������v���C���[���ʂɐݒ�
        objectiveText.gameObject.SetActive(false);
        // ShowMessage��tutorialText���g�p���A���b�Z�[�W�\����ɂ͔�\���ɂ���
        ResetCameraToTPS(); // �J������TPS���[�h�ɖ߂�
        yield return StartCoroutine(ShowMessage("�_���[�W���󂯂܂����I\nHP��0�ɂȂ�ƃQ�[���I�[�o�[�ł��B\n�G�̍U���ɒ��ӂ��܂��傤�I", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));
    }
}
=======
        // �G���|���ꂽ��A���̓G�I�u�W�F�N�g�̎Q�Ƃ��N���A
        if (currentEnemyInstance != null)
        {
            // �����̓G���o�����Ă���ꍇ�̂��߂ɁADestroy���ꂽ�I�u�W�F�N�g�̎Q�Ƃ��m���ɃN���A����
            // �����ł̓C�x���g���w�ǉ������Anull�ɂ���
            currentEnemyInstance.GetComponent<EnemyHealth>().onDeath -= HandleEnemyDeath;
            currentEnemyInstance = null; // �P���currentEnemyInstance�������Ă��Ȃ��̂Œ���
        }
    }
}
>>>>>>> New
