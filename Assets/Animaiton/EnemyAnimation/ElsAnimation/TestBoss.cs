using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestBoss : MonoBehaviour
{
    public enum BossState { Idle, Hovering, Relocating, BeamAttack, StabbingAttack }

    [Header("Basic Settings")]
    [SerializeField] private BossState _currentState = BossState.Idle;
    [SerializeField] private Transform _player;
    [SerializeField] private float _maxHealth = 1000f;
    private float _currentHealth;

    [Header("Movement Settings")]
    [SerializeField] private float _hoverSpeed = 5f;
    [SerializeField] private float _relocateSpeed = 20f;
    [SerializeField] private float _stoppingDistance = 15f;
    [SerializeField] private float _hoverHeight = 10f;

    [Header("Beam Attack (2 Bits)")]
    [SerializeField] private Transform[] _beamBits;
    [SerializeField] private GameObject _beamEffectPrefab;
    [SerializeField] private float _beamDuration = 1.0f;
    [SerializeField] private float _beamAttackSpread = 6.0f;

    [Header("Stabbing Attack (4 Separate Bits)")]
    [SerializeField] private Transform[] _stabBits;         // 配列に変更
    [SerializeField] private float _stabPrepareTime = 0.8f;
    [SerializeField] private float _stabDashSpeed = 55f;
    [SerializeField] private float _stabReturnSpeed = 20f;

    private CharacterController _controller;
    private Vector3 _targetPosition;
    private bool _isActionInProgress = false;

    private Vector3[] _beamBitLocalDefaults;
    private Vector3[] _stabBitLocalDefaults;               // 配列に変更

    void Start()
    {
        _controller = GetComponent<CharacterController>();
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

        // 突き刺しビット初期位置保存 (4つ分)
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

        if (_currentState != BossState.BeamAttack && _currentState != BossState.StabbingAttack)
            LookAtPlayer();

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
            if (rand < 0.35f)
                StartCoroutine(ExecuteBeamAttack());
            else if (rand < 0.7f)
                StartCoroutine(ExecuteStabbingAttack());
            else
                SetState(BossState.Hovering);
        }
    }

    private void SetState(BossState newState, Vector3 targetPos = default)
    {
        _currentState = newState;
        _targetPosition = targetPos;
    }
    #endregion

    #region 突き刺し攻撃 (4つ同時)
    private IEnumerator ExecuteStabbingAttack()
    {
        if (_stabBits == null || _stabBits.Length == 0 || _player == null) yield break;

        _isActionInProgress = true;
        _currentState = BossState.StabbingAttack;

        int count = _stabBits.Length;
        Vector3[] startWorldPos = new Vector3[count];
        Vector3[] readyWorldPos = new Vector3[count];

        // 1. 予備動作：プレイヤーを囲むように頭上に展開
        for (int i = 0; i < count; i++)
        {
            startWorldPos[i] = _stabBits[i].position;
            // プレイヤーの周囲に円状に配置
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

        // 2. 突進：各ビットがプレイヤーの現在位置へ突っ込む
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

        yield return new WaitForSeconds(0.3f);

        // 3. 帰還
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
    #endregion

    // --- 以下、移動・ビーム・LookAt 等は変更なし ---
    #region ビーム攻撃・移動 (既存のまま)
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