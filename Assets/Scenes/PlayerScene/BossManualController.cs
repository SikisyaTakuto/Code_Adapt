using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class BossManualController : MonoBehaviour
{
    public bool isActivated = true;

    public enum BossState { Idle, Attack1, Attack2, StabbingAttack }
    [SerializeField] private BossState _currentState = BossState.Idle;
    [SerializeField] private Transform _player;

    [Header("Attack 1 (Shield Swipe)")]
    [SerializeField] private Transform _swipeShield;
    [SerializeField] private float _swipeApproachSpeed = 25f;
    [SerializeField] private float _swipeDistance = 4f;
    [SerializeField] private float _swipeHitActiveTime = 0.4f;
    [SerializeField] private float _swipeHitDuration =5f;
    [SerializeField] private float _swipeDamage = 20f;

    [Header("Attack 2 (4 Shields Beam)")]
    [SerializeField] private Transform[] _shields;
    [SerializeField] private GameObject _shieldBeamPrefab;
    [SerializeField] private float _attack2Delay = 6.0f;
    [SerializeField] private float _attack2BeamDuration = 6.0f;
    [SerializeField] private float _heightMatchSpeed = 5.0f;

    [Header("Stabbing Attack")]
    [SerializeField] private Transform[] _stabBits;
    [SerializeField] private float _stabPrepareTime = 0.8f;
    [SerializeField] private float _stabDashSpeed = 55f;
    [SerializeField] private float _stabbingDamage = 15f;

    [Header("Attack 4 (2 Bits Beam)")]
    [SerializeField] private Transform[] _beamBits;        // 2つのビットを登録
    [SerializeField] private GameObject _beamEffectPrefab; // ビームのエフェクト
    [SerializeField] private float _beamDuration = 1.0f;   // ビームの持続時間
    [SerializeField] private float _beamAttackSpread = 6.0f; // ビームの左右の広がり
    [SerializeField] private float _beamDamage = 10f;      // ビームのダメージ
    private Vector3[] _beamBitLocalDefaults;

    private Animator _animator;
    private bool _isActionInProgress = false;
    private Vector3[] _stabBitLocalDefaults;

    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // --- 突き刺しビットの初期化 ---
        if (_stabBits != null && _stabBits.Length > 0)
        {
            _stabBitLocalDefaults = new Vector3[_stabBits.Length];
            for (int i = 0; i < _stabBits.Length; i++)
            {
                if (_stabBits[i] != null)
                    _stabBitLocalDefaults[i] = _stabBits[i].localPosition;
                else
                    Debug.LogError($"StabBitsの要素 {i} が空です！インスペクターを確認してください。");
            }
        }
        else
        {
            Debug.LogWarning("StabBitsがインスペクターで登録されていません。");
        }

        // --- ビームビットの初期化（前回の修正） ---
        if (_beamBits != null && _beamBits.Length > 0)
        {
            _beamBitLocalDefaults = new Vector3[_beamBits.Length];
            for (int i = 0; i < _beamBits.Length; i++)
            {
                if (_beamBits[i] != null)
                    _beamBitLocalDefaults[i] = _beamBits[i].localPosition;
            }
        }
    }

    void Update()
    {
        if (!isActivated || _player == null) return;

        // 攻撃中でなければプレイヤーの方を向く
        if (!_isActionInProgress)
        {
            LookAtPlayer();
            CheckInput();
        }
    }

    private void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) StartCoroutine(ExecuteAttack1());
        if (Input.GetKeyDown(KeyCode.Alpha2)) StartCoroutine(ExecuteAttack2());
        if (Input.GetKeyDown(KeyCode.Alpha3)) StartCoroutine(ExecuteStabbingAttack());
        if (Input.GetKeyDown(KeyCode.Alpha4)) StartCoroutine(ExecuteBeamAttack()); // 追加
    }

    #region Attack 1: Swipe (盾の薙ぎ払い)
    private IEnumerator ExecuteAttack1()
    {
        _isActionInProgress = true;
        _currentState = BossState.Attack1;

        // --- 1. 接近処理 ---
        float timeout = 2.0f;
        while (timeout > 0)
        {
            timeout -= Time.deltaTime;
            Vector3 diff = transform.position - _player.position;
            diff.y = 0;
            Vector3 targetPos = _player.position + diff.normalized * _swipeDistance;
            targetPos.y = _player.position.y;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, _swipeApproachSpeed * Time.deltaTime);
            LookAtPlayer();

            if (Vector3.Distance(transform.position, targetPos) < 0.5f) break;
            yield return null;
        }

        // --- 2. 攻撃アニメーションの開始 ---
        if (_animator) _animator.SetTrigger("Attack1");

        // 【修正ポイント】振りかぶりに合わせて待機してから判定を生成・有効化
        yield return new WaitForSeconds(_swipeHitActiveTime);

        // --- 3. 判定の追加と有効化（突き刺し攻撃と同じ方式） ---
        // 判定が必要なタイミングで初めてコンポーネントを取得/追加し、ダメージをセット
        BitCollision col = GetOrAddBitCollision(_swipeShield, _swipeDamage);

        col.SetColliderActive(true);
        Debug.Log("<color=orange>[Attack1]</color> 盾の判定を生成・有効化しました");

        // 判定持続時間
        yield return new WaitForSeconds(_swipeHitDuration);

        // --- 4. 判定の終了 ---
        col.SetColliderActive(false);
        Debug.Log("<color=orange>[Attack1]</color> 盾の判定を無効化しました");

        // アニメーションの終わりの余韻
        yield return new WaitForSeconds(0.5f);

        FinishAction();
    }
    #endregion

    #region Attack 2: 4-Shield Beam (一斉斉射)
    private IEnumerator ExecuteAttack2()
    {
        _isActionInProgress = true;
        _currentState = BossState.Attack2;

        // --- 1. 高さ合わせ ---
        float heightOffset = 1.0f;
        float preparationTimer = 0f;
        while (preparationTimer < 1.0f)
        {
            preparationTimer += Time.deltaTime;
            float targetY = _player.position.y + heightOffset;
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetY, transform.position.z), _heightMatchSpeed * Time.deltaTime);
            LookAtPlayer();
            yield return null;
        }

        // --- 2. 溜め（チャージ）フェーズ ---
        if (_animator) _animator.SetTrigger("Attack2");

        float chargeTimer = 0;
        while (chargeTimer < _attack2Delay)
        {
            chargeTimer += Time.deltaTime;
            LookAtPlayer();
            yield return null;
        }

        // --- 3. 一斉発射 ---
        foreach (var shield in _shields)
        {
            if (shield != null && _shieldBeamPrefab)
            {
                Vector3 targetPoint = _player.position + Vector3.up * heightOffset;
                Vector3 shootDirection = targetPoint - shield.position;

                // ビームを生成
                GameObject beamObj = Instantiate(_shieldBeamPrefab, shield.position, Quaternion.LookRotation(shootDirection), shield);

                // 【修正】BossBeamControllerを取得してパラメータを渡す
                BossBeamController controller = beamObj.GetComponent<BossBeamController>();
                if (controller != null)
                {
                    controller.lifetime = _attack2BeamDuration; // ボス側の持続時間を反映
                    controller.damageAmount = _beamDamage;      // ビーム用のダメージ変数を反映

                    // Fireを実行。当たっても消えない設定で発射
                    controller.Fire(shield.position, targetPoint, true, _player.gameObject);
                }
                else
                {
                    // Controllerがない場合の保険
                    Destroy(beamObj, _attack2BeamDuration);
                }
            }
        }

        // ビームが消えるまで待機（ボスは硬直）
        yield return new WaitForSeconds(_attack2BeamDuration);

        FinishAction();
    }
    #endregion

    #region Attack 3: Stabbing
    private IEnumerator ExecuteStabbingAttack()
    {
        _isActionInProgress = true;
        _currentState = BossState.StabbingAttack;

        int count = _stabBits.Length;
        Vector3[] readyWorldPos = new Vector3[count];
        BitCollision[] cols = new BitCollision[count];

        for (int i = 0; i < count; i++)
        {
            cols[i] = GetOrAddBitCollision(_stabBits[i], _stabbingDamage);
            float angle = i * Mathf.PI * 2f / count;
            readyWorldPos[i] = _player.position + Vector3.up * 5f + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 4f;
        }

        // 準備移動
        float elapsed = 0;
        Vector3[] startPos = new Vector3[count];
        for (int i = 0; i < count; i++) startPos[i] = _stabBits[i].position;

        while (elapsed < _stabPrepareTime)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < count; i++)
            {
                _stabBits[i].position = Vector3.Lerp(startPos[i], readyWorldPos[i], elapsed / _stabPrepareTime);
                _stabBits[i].LookAt(_player.position);
            }
            yield return null;
        }

        // 突撃
        foreach (var c in cols) c.SetColliderActive(true);
        Vector3 target = _player.position;
        float dashTime = 0;
        while (dashTime < 0.5f)
        {
            dashTime += Time.deltaTime;
            for (int i = 0; i < count; i++) _stabBits[i].position = Vector3.MoveTowards(_stabBits[i].position, target, _stabDashSpeed * Time.deltaTime);
            yield return null;
        }

        foreach (var c in cols) c.SetColliderActive(false);

        // 帰還
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < count; i++)
        {
            if (_stabBits[i] != null && _stabBitLocalDefaults != null)
            {
                _stabBits[i].localPosition = _stabBitLocalDefaults[i];
                _stabBits[i].localRotation = Quaternion.identity;
            }
        }

        FinishAction();
    }
    #endregion

    #region Attack 4: 2-Bits Beam
    private IEnumerator ExecuteBeamAttack()
    {
        if (_beamBits == null || _beamBits.Length < 2) yield break;

        _isActionInProgress = true;
        _currentState = BossState.Attack2; // 必要に応じてEnumを調整してください

        float transitionTime = 0.5f; // 移動にかける時間
        Vector3[] worldStartPos = new Vector3[_beamBits.Length];
        for (int i = 0; i < _beamBits.Length; i++) worldStartPos[i] = _beamBits[i].position;

        // 1. ビットを攻撃ポジション（ボスの左右）へ展開
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / transitionTime;
            for (int i = 0; i < _beamBits.Length; i++)
            {
                float side = (i == 0) ? -1 : 1;
                // ボスから見て左右に展開し、少し浮かせる
                Vector3 targetWorld = transform.position + transform.right * (side * _beamAttackSpread) + transform.up * 2f;
                _beamBits[i].position = Vector3.Lerp(worldStartPos[i], targetWorld, t);
                _beamBits[i].LookAt(_player.position);
            }
            yield return null;
        }

        // 2. ビーム発射
        foreach (var bit in _beamBits)
        {
            if (_beamEffectPrefab)
            {
                // ビットの向きで生成
                GameObject beam = Instantiate(_beamEffectPrefab, bit.position, bit.rotation, bit);

                BossBeamController controller = beam.GetComponent<BossBeamController>();
                if (controller != null)
                {
                    controller.lifetime = _beamDuration; // ボス側の設定時間を反映
                    controller.damageAmount = _beamDamage;
                    // プレイヤーをターゲットとして発射
                    controller.Fire(bit.position, _player.position, true, _player.gameObject);
                }
                else
                {
                    // Controllerがない場合のみ、予備としてDestroyを呼ぶ
                    Destroy(beam, _beamDuration);
                }

                // BitCollision（物理判定）が必要な場合は残す
                BitCollision col = GetOrAddBitCollision(bit, _beamDamage);
                StartCoroutine(TemporaryCollider(col, _beamDuration));
            }
        }

        yield return new WaitForSeconds(_beamDuration);

        // 3. ビットを元のポジションに戻す
        float returnTime = 0;

        // worldEndPos 配列の初期化漏れを防ぐ
        Vector3[] worldEndPos = new Vector3[_beamBits.Length];
        for (int i = 0; i < _beamBits.Length; i++)
        {
            if (_beamBits[i] != null) worldEndPos[i] = _beamBits[i].localPosition;
        }

        while (returnTime < 1.0f)
        {
            returnTime += Time.deltaTime / transitionTime;
            for (int i = 0; i < _beamBits.Length; i++)
            {
                // _beamBitLocalDefaults が Null ではないか、要素数が足りているか確認
                if (_beamBits[i] != null && _beamBitLocalDefaults != null && i < _beamBitLocalDefaults.Length)
                {
                    _beamBits[i].localPosition = Vector3.Lerp(worldEndPos[i], _beamBitLocalDefaults[i], returnTime);
                    _beamBits[i].localRotation = Quaternion.Slerp(_beamBits[i].localRotation, Quaternion.identity, returnTime);
                }
            }
            yield return null;
        }

        // 最後に確実に位置を合わせる
        for (int i = 0; i < _beamBits.Length; i++)
        {
            _beamBits[i].localPosition = _beamBitLocalDefaults[i];
            _beamBits[i].localRotation = Quaternion.identity;
        }

        FinishAction();
    }

    // 当たり判定の有効化/無効化を制御するコルーチン
    private IEnumerator TemporaryCollider(BitCollision col, float duration)
    {
        col.SetColliderActive(true);
        yield return new WaitForSeconds(duration);
        col.SetColliderActive(false);
    }
    #endregion

    private void FinishAction()
    {
        _isActionInProgress = false;
        _currentState = BossState.Idle;
    }

    private void LookAtPlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    private BitCollision GetOrAddBitCollision(Transform t, float dmg)
    {
        BitCollision bc = t.GetComponent<BitCollision>();
        if (bc == null) bc = t.gameObject.AddComponent<BitCollision>();
        bc.Setup(dmg);
        return bc;
    }
}