using UnityEngine;
using UnityEngine.UI; // Text, Canvas, Slider ���g������
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public PlayerController playerController;
    public Text tutorialText; // TextMeshProUGUI �ɕύX
    public GameObject enemyPrefab; // �G��Prefab
    public Transform enemySpawnPoint; // �G�̏o���ʒu
    public GameObject armorModeEnemyPrefab; // �A�[�}�[���[�h�؂�ւ����ɏo��������G��Prefab
    public Transform armorModeEnemySpawnPoint; // �A�[�}�[���[�h�؂�ւ����ɏo��������G�̏o���ʒu

    private GameObject currentEnemyInstance; // ���ݏo�����Ă���G�̃C���X�^���X

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


        // �`���[�g���A���J�n
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
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
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v4: �I���g�L�[�ŉ�����
        currentStep = TutorialStep.Descend;
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
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v8: �z�C�[�������݂œ���U��
        currentStep = TutorialStep.SpecialAttack;
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

        yield return StartCoroutine(ShowMessage("�f���炵���I����U�����g�����Ȃ��܂��ˁI", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �v���C���[���ēx���S�ɖ߂�
        currentStep = TutorialStep.ResetPosition;
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

        yield return StartCoroutine(ShowMessage("�A�[�}�[���[�h�̐؂�ւ������I\n�󋵂ɍ��킹�ă��[�h���g�������܂��傤�B", 2.0f));
        yield return StartCoroutine(WaitForPlayerAction(0.5f));

        // �X�e�b�v10: �`���[�g���A���I��
        currentStep = TutorialStep.End;
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
    IEnumerator ShowMessage(string message, float duration)
    {
        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);
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
    }

    /// <summary>
    /// �v���C���[�����̈ʒu�Ƀe���|�[�g������
    /// </summary>
    /// <param name="position"></param>
    void TeleportPlayer(Vector3 position)
    {
        if (playerController != null)
        {
            // CharacterController���g�p���Ă��邽�߁A����position��ݒ肷��̂ł͂Ȃ��A
            // Disable���Ĉʒu��ύX���A�ēxEnable�����@����邩�A
            // �܂���Controller.Move���g���Ĕ��ɒZ�����Ԃňړ�������Ȃǂ̍H�v���K�v�B
            // �����ł͊ȈՓI�ɁA��U�I�t�ɂ��Ĉʒu��ݒ�A�����ɃI���ɖ߂��B
            // �i�������A����ɂ��ꎞ�I�ɃR���W���������������\��������̂Œ��Ӂj
            playerController.gameObject.SetActive(false);
            playerController.transform.position = position;
            playerController.gameObject.SetActive(true);
            Debug.Log($"�v���C���[�� {position} �Ƀe���|�[�g���܂����B");
        }
    }

    /// <summary>
    /// �G�𐶐�����B�����̓G������Δj������B
    /// </summary>
    /// <param name="prefab">�G��Prefab</param>
    /// <param name="position">�o���ʒu</param>
    void SpawnEnemy(GameObject prefab, Vector3 position)
    {
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance); // �����̓G��j��
            currentEnemyInstance = null;
        }

        // ���łɑ��݂��Ă���S�Ă�Enemy�^�O�̃I�u�W�F�N�g��j��
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            Destroy(enemy);
        }

        if (prefab != null)
        {
            currentEnemyInstance = Instantiate(prefab, position, Quaternion.identity);
            // EnemyHealth�X�N���v�g���A�^�b�`����Ă��邱�Ƃ��m�F
            EnemyHealth enemyHealth = currentEnemyInstance.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // Root�ɂȂ��ꍇ�́A�q�I�u�W�F�N�g���猟��
                enemyHealth = currentEnemyInstance.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                // �G���|���ꂽ�Ƃ��ɃC�x���g���w�ǂł���悤�ɂ���
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