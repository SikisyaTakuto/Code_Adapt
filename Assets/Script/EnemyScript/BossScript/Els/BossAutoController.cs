using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BossAutoController : MonoBehaviour
{
    private ElsController _mainController;
    private CharacterController _controller; // 追加
    private Transform _player;

    [Header("AI Settings")]
    [SerializeField] private float _attackInterval = 3.0f;

    public enum AttackState { Idle, Attack1, Attack2, StabbingAttack, BeamAttack }
    [SerializeField] private AttackState _currentAttack = AttackState.Idle;

    [Header("Attack 1 (Shield Swipe)")]
    [SerializeField] private Transform _swipeShield;
    [SerializeField] private float _swipeApproachSpeed = 25f;
    [SerializeField] private float _swipeDistance = 4f;
    [SerializeField] private float _swipeHitActiveTime = 0.4f;
    [SerializeField] private float _swipeHitDuration = 0.5f;
    [SerializeField] private float _swipeDamage = 20f;

    [Header("Attack 2 (4 Shields Beam)")]
    [SerializeField] private Transform[] _shields;
    [SerializeField] private GameObject _shieldBeamPrefab;
    [SerializeField] private float _attack2Delay = 3.0f;
    [SerializeField] private float _attack2BeamDuration = 3.0f;
    [SerializeField] private float _heightMatchSpeed = 5.0f;

    [Header("Stabbing Attack")]
    [SerializeField] private Transform[] _stabBits;
    [SerializeField] private float _stabPrepareTime = 0.8f;
    [SerializeField] private float _stabDashSpeed = 55f;
    [SerializeField] private float _stabbingDamage = 15f;

    [Header("Attack 4 (2 Bits Beam)")]
    [SerializeField] private Transform[] _beamBits;
    [SerializeField] private GameObject _beamEffectPrefab;
    [SerializeField] private float _beamDuration = 1.0f;
    [SerializeField] private float _beamAttackSpread = 6.0f;
    [SerializeField] private float _beamDamage = 10f;

    private Vector3[] _beamBitLocalDefaults;
    private Vector3[] _stabBitLocalDefaults;
    private Animator _animator;

    void Start()
    {
        _mainController = GetComponent<ElsController>();
        _controller = GetComponent<CharacterController>(); // 取得
        _animator = GetComponent<Animator>();

        InitializeDefaults();
        StartCoroutine(BossAIRoutine());
    }

    private void InitializeDefaults()
    {
        if (_stabBits != null && _stabBits.Length > 0)
        {
            _stabBitLocalDefaults = new Vector3[_stabBits.Length];
            for (int i = 0; i < _stabBits.Length; i++)
                if (_stabBits[i] != null) _stabBitLocalDefaults[i] = _stabBits[i].localPosition;
        }

        if (_beamBits != null && _beamBits.Length > 0)
        {
            _beamBitLocalDefaults = new Vector3[_beamBits.Length];
            for (int i = 0; i < _beamBits.Length; i++)
                if (_beamBits[i] != null) _beamBitLocalDefaults[i] = _beamBits[i].localPosition;
        }
    }

    private IEnumerator BossAIRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        while (true)
        {
            if (_mainController.isActivated && !_mainController.IsActionInProgress)
            {
                _player = _mainController.Player;
                if (_player == null) { yield return new WaitForSeconds(1f); continue; }

                int attackIndex = Random.Range(1, 5);
                _mainController.IsActionInProgress = true;

                switch (attackIndex)
                {
                    case 1: yield return StartCoroutine(ExecuteAttack1()); break;
                    case 2: yield return StartCoroutine(ExecuteAttack2()); break;
                    case 3: yield return StartCoroutine(ExecuteStabbingAttack()); break;
                    case 4: yield return StartCoroutine(ExecuteBeamAttack()); break;
                }

                _mainController.IsActionInProgress = false;
                yield return new WaitForSeconds(_attackInterval);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    #region Attacks

    private IEnumerator ExecuteAttack1()
    {
        _currentAttack = AttackState.Attack1;
        float timeout = 2.0f;
        while (timeout > 0)
        {
            timeout -= Time.deltaTime;
            Vector3 diff = transform.position - _player.position;
            diff.y = 0;
            Vector3 targetPos = _player.position + diff.normalized * _swipeDistance;

            // --- 物理移動への変更 ---
            Vector3 moveDir = Vector3.MoveTowards(transform.position, targetPos, _swipeApproachSpeed * Time.deltaTime) - transform.position;
            _controller.Move(moveDir);

            _mainController.LookAtPlayer();
            if (Vector3.Distance(transform.position, targetPos) < 0.5f) break;
            yield return null;
        }

        if (_animator)
        {
            _animator.ResetTrigger("Attack2");
            _animator.SetTrigger("Attack1");
        }

        yield return new WaitForSeconds(_swipeHitActiveTime);
        BitCollision col = GetOrAddBitCollision(_swipeShield, _swipeDamage);
        col.SetColliderActive(true);
        yield return new WaitForSeconds(_swipeHitDuration);
        col.SetColliderActive(false);

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteAttack2()
    {
        _currentAttack = AttackState.Attack2;
        if (_animator)
        {
            _animator.ResetTrigger("Attack1");
            _animator.SetTrigger("Attack2");
        }

        float t = 0;
        while (t < _attack2Delay)
        {
            t += Time.deltaTime;

            // --- 修正ポイント ---
            float targetY = _player.position.y + 1.5f;

            // 天井がある（上方向2m以内に天井がある）場合は、現在地より上には行かない
            if (targetY > transform.position.y && _mainController.IsCeilingAbove(2.0f))
            {
                // 天井にぶつかっているので、Y座標の目標を現在の高さに固定
                targetY = transform.position.y;
            }

            Vector3 targetPos = new Vector3(transform.position.x, targetY, transform.position.z);
            Vector3 moveDir = Vector3.MoveTowards(transform.position, targetPos, _heightMatchSpeed * Time.deltaTime) - transform.position;

            _controller.Move(moveDir);
            // ------------------

            _mainController.LookAtPlayer();

            // 盾の回転（既存通り）
            foreach (var shield in _shields)
            {
                if (shield != null)
                {
                    Vector3 targetDir = _player.position - shield.position;
                    if (targetDir != Vector3.zero)
                        shield.rotation = Quaternion.Slerp(shield.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * 10f);
                }
            }
            yield return null;
        }

        foreach (var shield in _shields)
        {
            if (shield != null && _shieldBeamPrefab)
            {
                Vector3 finalDir = _player.position - shield.position;
                Quaternion fireRotation = Quaternion.LookRotation(finalDir);
                GameObject beamObj = Instantiate(_shieldBeamPrefab, shield.position, fireRotation, shield);
                BossBeamController ctrl = beamObj.GetComponent<BossBeamController>();
                if (ctrl)
                {
                    ctrl.lifetime = _attack2BeamDuration;
                    ctrl.damageAmount = _beamDamage;
                    ctrl.Fire(shield.position, _player.position, true, _player.gameObject);
                }
            }
        }
        yield return new WaitForSeconds(_attack2BeamDuration);
        foreach (var shield in _shields) if (shield != null) shield.localRotation = Quaternion.identity;
    }

    private IEnumerator ExecuteStabbingAttack()
    {
        _currentAttack = AttackState.StabbingAttack;
        int count = _stabBits.Length;
        BitCollision[] cols = new BitCollision[count];
        Vector3[] readyWorldPos = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            cols[i] = GetOrAddBitCollision(_stabBits[i], _stabbingDamage);
            float angle = i * Mathf.PI * 2f / count;
            readyWorldPos[i] = _player.position + Vector3.up * 4f + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 3f;
        }

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

        foreach (var c in cols) c.SetColliderActive(true);
        Vector3 target = _player.position;
        float dashTime = 0;
        while (dashTime < 0.6f)
        {
            dashTime += Time.deltaTime;
            for (int i = 0; i < count; i++)
                _stabBits[i].position = Vector3.MoveTowards(_stabBits[i].position, target, _stabDashSpeed * Time.deltaTime);
            yield return null;
        }
        foreach (var c in cols) c.SetColliderActive(false);

        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < count; i++)
        {
            _stabBits[i].localPosition = _stabBitLocalDefaults[i];
            _stabBits[i].localRotation = Quaternion.identity;
        }
    }

    private IEnumerator ExecuteBeamAttack()
    {
        _currentAttack = AttackState.BeamAttack;
        float t = 0;
        Vector3[] startPos = new Vector3[_beamBits.Length];
        for (int i = 0; i < _beamBits.Length; i++) startPos[i] = _beamBits[i].position;

        while (t < 1.0f)
        {
            t += Time.deltaTime / 0.5f;
            for (int i = 0; i < _beamBits.Length; i++)
            {
                float side = (i == 0) ? -1 : 1;
                Vector3 targetWorld = transform.position + transform.right * (side * _beamAttackSpread) + transform.up * 2f;
                _beamBits[i].position = Vector3.Lerp(startPos[i], targetWorld, t);
                _beamBits[i].LookAt(_player.position);
            }
            yield return null;
        }

        foreach (var bit in _beamBits)
        {
            if (_beamEffectPrefab)
            {
                GameObject beam = Instantiate(_beamEffectPrefab, bit.position, bit.rotation, bit);
                BossBeamController ctrl = beam.GetComponent<BossBeamController>();
                if (ctrl)
                {
                    ctrl.lifetime = _beamDuration;
                    ctrl.damageAmount = _beamDamage;
                    ctrl.Fire(bit.position, _player.position, true, _player.gameObject);
                }
                StartCoroutine(TemporaryCollider(GetOrAddBitCollision(bit, _beamDamage), _beamDuration));
            }
        }
        yield return new WaitForSeconds(_beamDuration + 0.5f);

        float rt = 0;
        Vector3[] currentPos = new Vector3[_beamBits.Length];
        for (int i = 0; i < _beamBits.Length; i++) currentPos[i] = _beamBits[i].localPosition;

        while (rt < 1.0f)
        {
            rt += Time.deltaTime / 0.5f;
            for (int i = 0; i < _beamBits.Length; i++)
            {
                _beamBits[i].localPosition = Vector3.Lerp(currentPos[i], _beamBitLocalDefaults[i], rt);
                _beamBits[i].localRotation = Quaternion.Slerp(_beamBits[i].localRotation, Quaternion.identity, rt);
            }
            yield return null;
        }
    }
    #endregion

    private BitCollision GetOrAddBitCollision(Transform t, float dmg)
    {
        BitCollision bc = t.GetComponent<BitCollision>();
        if (bc == null) bc = t.gameObject.AddComponent<BitCollision>();
        bc.Setup(dmg);
        return bc;
    }

    private IEnumerator TemporaryCollider(BitCollision col, float duration)
    {
        col.SetColliderActive(true);
        yield return new WaitForSeconds(duration);
        col.SetColliderActive(false);
    }
}