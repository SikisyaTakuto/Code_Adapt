using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestBoss : MonoBehaviour
{
    // Attack2 をステートに追加
    public enum BossState { Idle, Hovering, Relocating, BeamAttack, StabbingAttack, Attack2 }

    [Header("Basic Settings")]
    [SerializeField] private BossState _currentState = BossState.Idle;
    [SerializeField] private Transform _player;
    [SerializeField] private float _maxHealth = 1000f;
    private float _currentHealth;
    private Animator _animator;

    [Header("Movement Settings")]
    [SerializeField] private float _hoverSpeed = 5f;
    [SerializeField] private float _relocateSpeed = 20f;
    [SerializeField] private float _stoppingDistance = 15f;
    [SerializeField] private float _hoverHeight = 10f;

    [Header("Existing Beam Attack (2 Bits)")]
    [SerializeField] private Transform[] _beamBits;
    [SerializeField] private GameObject _beamEffectPrefab;
    [SerializeField] private float _beamDuration = 1.0f;
    [SerializeField] private float _beamAttackSpread = 6.0f;

    [Header("Stabbing Attack (4 Separate Bits)")]
    [SerializeField] private Transform[] _stabBits;
    [SerializeField] private float _stabPrepareTime = 0.8f;
    [SerializeField] private float _stabDashSpeed = 55f;
    [SerializeField] private float _stabReturnSpeed = 20f;

    [Header("New Attack 2 (4 Shields Beam)")]
    [SerializeField] private Transform[] _shields;          // ボスが持っている4つの盾を登録
    [SerializeField] private GameObject _shieldBeamPrefab;  // 盾用ビームのエフェクト
    private float _attack2Delay = 6.0f;    // アニメ開始からビームが出るまでの時間
    private float _attack2Duration = 12.0f; // 攻撃モーションの終了までの時間

    private CharacterController _controller;
    private Vector3 _targetPosition;
    private bool _isActionInProgress = false;

    private Vector3[] _beamBitLocalDefaults;
    private Vector3[] _stabBitLocalDefaults;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _currentHealth = _maxHealth;

        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // ビームビット初期位置保存
        if (_beamBits != null)
        {
            _beamBitLocalDefaults = new Vector3[_beamBits.Length];
            for (int i = 0; i < _beamBits.Length; i++)
                _beamBitLocalDefaults[i] = _beamBits[i].localPosition;
        }

        // 突き刺しビット初期位置保存
        if (_stabBits != null)
        {
            _stabBitLocalDefaults = new Vector3[_stabBits.Length];
            for (int i = 0; i < _stabBits.Length; i++)
                _stabBitLocalDefaults[i] = _stabBits[i].localPosition;
        }

        StartCoroutine(BossLoop());
    }

    void Update()
    {
        if (_player == null) return;

        // 攻撃動作中（特に盾ビーム中）はLookAtを停止、またはアニメーションに任せる
        if (_currentState != BossState.BeamAttack &&
            _currentState != BossState.StabbingAttack &&
            _currentState != BossState.Attack2)
        {
            LookAtPlayer();
        }

        switch (_currentState)
        {
            case BossState.Hovering:
                UpdateHovering();
                break;
            case BossState.Relocating:
                MoveTowardsTarget(_relocateSpeed, 1.0f);
                break;
        }
    }

    #region AIロジック
    private IEnumerator BossLoop()
    {
        while (_currentHealth > 0)
        {
            if (!_isActionInProgress)
            {
                ChooseNextAction();
            }
            yield return new WaitForSeconds(0.6f);
        }
    }

    private void ChooseNextAction()
    {
        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance > _stoppingDistance + 5f)
        {
            _isActionInProgress = true;
            SetState(BossState.Relocating, GetRandomPositionNearPlayer());
        }
        else
        {
            float rand = Random.value;
            // 確率を調整（Attack2を30%で追加）
            if (rand < 0.30f)
                StartCoroutine(ExecuteAttack2());
            else if (rand < 0.65f)
                StartCoroutine(ExecuteBeamAttack());
            else
                StartCoroutine(ExecuteStabbingAttack());
        }
    }

    private void SetState(BossState newState, Vector3 targetPos = default)
    {
        _currentState = newState;
        _targetPosition = targetPos;
    }
    #endregion

    #region Attack 2 (4枚の盾からビーム)
    private IEnumerator ExecuteAttack2()
    {
        if (_shields == null || _shields.Length == 0 || _player == null) yield break;

        _isActionInProgress = true;
        _currentState = BossState.Attack2;

        // アニメーション開始
        if (_animator) _animator.SetTrigger("Attack2");

        // 発射タイミングまで待機
        yield return new WaitForSeconds(_attack2Delay);

        // 各盾からビームを生成
        foreach (var shield in _shields)
        {
            if (_shieldBeamPrefab)
            {
                // --- 修正ポイント：プレイヤーへの回転を計算 ---
                // 盾の位置からプレイヤーの位置へのベクトルを計算
                Vector3 directionToPlayer = (_player.position + Vector3.up) - shield.position;
                // 回転を作成 (LookRotation)
                Quaternion beamRotation = Quaternion.LookRotation(directionToPlayer);

                // 計算した回転(beamRotation)を使用して生成
                // shieldを親にすることで、ボスの移動には追従させつつ、向きは生成時のプレイヤー方向へ固定
                GameObject beam = Instantiate(_shieldBeamPrefab, shield.position, beamRotation, shield);

                Destroy(beam, _attack2Duration - _attack2Delay);
            }
        }

        // アニメーション終了まで待機
        yield return new WaitForSeconds(_attack2Duration - _attack2Delay);

        _isActionInProgress = false;
        _currentState = BossState.Hovering;
    }
    #endregion

    #region 既存の攻撃 (Stabbing / Beam)
    private IEnumerator ExecuteStabbingAttack()
    {
        if (_stabBits == null || _stabBits.Length == 0 || _player == null) yield break;

        _isActionInProgress = true;
        _currentState = BossState.StabbingAttack;

        int count = _stabBits.Length;
        Vector3[] startWorldPos = new Vector3[count];
        Vector3[] readyWorldPos = new Vector3[count];

        BitCollision[] bitCollisions = new BitCollision[count];
        for (int i = 0; i < count; i++)
        {
            bitCollisions[i] = _stabBits[i].GetComponent<BitCollision>();
            if (bitCollisions[i] == null) bitCollisions[i] = _stabBits[i].gameObject.AddComponent<BitCollision>();
        }

        for (int i = 0; i < count; i++)
        {
            startWorldPos[i] = _stabBits[i].position;
            float angle = i * Mathf.PI * 2f / count;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 4f;
            readyWorldPos[i] = _player.position + Vector3.up * 8f + offset;
        }

        float elapsed = 0;
        while (elapsed < _stabPrepareTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _stabPrepareTime;
            for (int i = 0; i < count; i++)
            {
                _stabBits[i].position = Vector3.Lerp(startWorldPos[i], readyWorldPos[i], t);
                _stabBits[i].LookAt(_player.position);
            }
            yield return null;
        }

        foreach (var col in bitCollisions) col.SetColliderActive(true);

        Vector3 attackTarget = _player.position;
        bool allReached = false;
        float timeout = 0;

        while (!allReached && timeout < 2.0f)
        {
            timeout += Time.deltaTime;
            allReached = true;
            for (int i = 0; i < count; i++)
            {
                _stabBits[i].position = Vector3.MoveTowards(_stabBits[i].position, attackTarget, _stabDashSpeed * Time.deltaTime);
                if (Vector3.Distance(_stabBits[i].position, attackTarget) > 0.5f) allReached = false;
            }
            yield return null;
        }

        foreach (var col in bitCollisions) col.SetColliderActive(false);
        yield return new WaitForSeconds(0.3f);

        float returnElapsed = 0;
        float returnDuration = 0.8f;
        Vector3[] burstPos = new Vector3[count];
        for (int i = 0; i < count; i++) burstPos[i] = _stabBits[i].localPosition;

        while (returnElapsed < returnDuration)
        {
            returnElapsed += Time.deltaTime;
            float t = returnElapsed / returnDuration;
            for (int i = 0; i < count; i++)
            {
                _stabBits[i].localPosition = Vector3.Lerp(burstPos[i], _stabBitLocalDefaults[i], t);
                _stabBits[i].localRotation = Quaternion.Slerp(_stabBits[i].localRotation, Quaternion.identity, t);
            }
            yield return null;
        }

        _isActionInProgress = false;
        _currentState = BossState.Hovering;
    }

    private IEnumerator ExecuteBeamAttack()
    {
        if (_beamBits == null || _beamBits.Length < 2) yield break;
        _isActionInProgress = true;
        _currentState = BossState.BeamAttack;

        float transitionTime = 0.4f;
        Vector3[] worldStartPos = new Vector3[_beamBits.Length];
        for (int i = 0; i < _beamBits.Length; i++) worldStartPos[i] = _beamBits[i].position;

        for (float t = 0; t < 1; t += Time.deltaTime / transitionTime)
        {
            for (int i = 0; i < _beamBits.Length; i++)
            {
                float side = (i == 0) ? -1 : 1;
                Vector3 targetWorld = transform.position + transform.right * (side * _beamAttackSpread) + transform.up * 2f;
                _beamBits[i].position = Vector3.Lerp(worldStartPos[i], targetWorld, t);
                _beamBits[i].LookAt(_player.position);
            }
            yield return null;
        }

        foreach (var bit in _beamBits)
        {
            if (_beamEffectPrefab)
            {
                GameObject beam = Instantiate(_beamEffectPrefab, bit.position, bit.rotation, bit);
                Destroy(beam, _beamDuration);
            }
        }
        yield return new WaitForSeconds(_beamDuration);

        for (float t = 0; t < 1; t += Time.deltaTime / transitionTime)
        {
            for (int i = 0; i < _beamBits.Length; i++)
            {
                _beamBits[i].localPosition = Vector3.Lerp(_beamBits[i].localPosition, _beamBitLocalDefaults[i], t);
                _beamBits[i].localRotation = Quaternion.Slerp(_beamBits[i].localRotation, Quaternion.identity, t);
            }
            yield return null;
        }

        _isActionInProgress = false;
        _currentState = BossState.Hovering;
    }
    #endregion

    #region 移動・補助
    private void UpdateHovering()
    {
        Vector3 hoverOffset = new Vector3(Mathf.Sin(Time.time) * 7f, _hoverHeight + Mathf.Cos(Time.time * 0.5f) * 3f, Mathf.Cos(Time.time) * 7f);
        _controller.Move(((_player.position + hoverOffset) - transform.position) * _hoverSpeed * Time.deltaTime);
    }

    private void MoveTowardsTarget(float speed, float stopRange)
    {
        Vector3 dir = (_targetPosition - transform.position);
        if (dir.magnitude > stopRange) _controller.Move(dir.normalized * speed * Time.deltaTime);
        else { _isActionInProgress = false; _currentState = BossState.Hovering; }
    }

    private Vector3 GetRandomPositionNearPlayer()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * _stoppingDistance;
        return _player.position + new Vector3(randomCircle.x, _hoverHeight + Random.Range(-3f, 6f), randomCircle.y);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = _player.position - transform.position; dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0) Die();
    }

    private void Die()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
    #endregion
}