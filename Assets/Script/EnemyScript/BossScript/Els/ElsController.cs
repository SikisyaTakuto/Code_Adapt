using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class ElsController : MonoBehaviour
{
    public bool isActivated = false;

    public enum BossState { Idle, Hovering, Relocating }

    [Header("Basic Settings")]
    [SerializeField] private BossState _currentState = BossState.Idle;
    [SerializeField] private Transform _player;
    [SerializeField] private float _maxHealth = 2000f;
    public UnityEngine.UI.Slider bossHpBar;

    private float _currentHealth;
    private bool _isDead = false;
    private Animator _animator;
    private CharacterController _controller;
    private bool _isActionInProgress = false; // 攻撃中フラグ

    [Header("Movement Settings")]
    [SerializeField] private float _hoverSpeed = 5f;
    [SerializeField] private float _relocateSpeed = 20f;
    [SerializeField] private float _stoppingDistance = 15f;
    [SerializeField] private float _hoverHeight = 10f;

    [Header("Collision & Raycast")]
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private LayerMask _ceilingLayer;
    [SerializeField] private float _collisionCheckRadius = 2.0f;
    [SerializeField] private int _maxRelocateAttempts = 15;

    private int _combinedCollisionLayers => _wallLayer | _ceilingLayer;
    private Vector3 _targetPosition;
    private float _relocateTimer = 0f;
    private const float RELOCATE_TIMEOUT = 3.0f;

    // 外部（BossAutoController）から読み書きするプロパティ
    public bool IsActionInProgress { get => _isActionInProgress; set => _isActionInProgress = value; }
    public Transform Player => _player;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _currentHealth = _maxHealth;

        if (bossHpBar != null)
        {
            bossHpBar.gameObject.SetActive(false);
            bossHpBar.maxValue = _maxHealth; // 最大値をセット
            bossHpBar.value = _maxHealth;
        }

        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartCoroutine(BossLoop());
    }

    void Update()
    {
        if (!isActivated || _player == null || _isDead) return;

        if (bossHpBar != null && !bossHpBar.gameObject.activeSelf)
            bossHpBar.gameObject.SetActive(true);

        // 攻撃中でなければ回転と移動を行う
        if (!_isActionInProgress)
        {
            LookAtPlayer();

            switch (_currentState)
            {
                case BossState.Hovering:
                    UpdateHovering();
                    break;
                case BossState.Relocating:
                    if (IsHeadingIntoWall())
                    {
                        SetState(BossState.Relocating, GetRandomPositionNearPlayer());
                    }
                    else
                    {
                        MoveTowardsTarget(_relocateSpeed, 1.5f);
                    }
                    break;
            }
        }
    }

    private IEnumerator BossLoop()
    {
        while (!_isDead)
        {
            if (isActivated && !_isActionInProgress)
            {
                float distance = Vector3.Distance(transform.position, _player.position);
                // 距離が離れすぎていたら再配置、近ければホバリング
                if (distance > _stoppingDistance + 5f)
                {
                    SetState(BossState.Relocating, GetRandomPositionNearPlayer());
                }
                else
                {
                    _currentState = BossState.Hovering;
                }
            }
            yield return new WaitForSeconds(0.6f);
        }
    }

    #region Movement Core
    private void UpdateHovering()
    {
        // プレイヤーの周りを円状に漂う
        Vector3 hoverOffset = new Vector3(Mathf.Sin(Time.time) * 7f, _hoverHeight, Mathf.Cos(Time.time) * 7f);
        Vector3 targetHoverPos = _player.position + hoverOffset;
        Vector3 moveDir = (targetHoverPos - transform.position);

        if (!Physics.SphereCast(transform.position, _collisionCheckRadius, moveDir.normalized, out _, 1.0f, _combinedCollisionLayers))
        {
            _controller.Move(moveDir * _hoverSpeed * Time.deltaTime);
        }
    }

    private void MoveTowardsTarget(float speed, float stopRange)
    {
        Vector3 dir = (_targetPosition - transform.position);
        _relocateTimer += Time.deltaTime;

        if (dir.magnitude > stopRange && _relocateTimer < RELOCATE_TIMEOUT)
        {
            _controller.Move(dir.normalized * speed * Time.deltaTime);
        }
        else
        {
            _relocateTimer = 0f;
            _currentState = BossState.Hovering;
        }
    }

    public void SetState(BossState newState, Vector3 targetPos = default)
    {
        _currentState = newState;
        _targetPosition = targetPos;
    }

    public void LookAtPlayer()
    {
        if (_player == null) return;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }
    #endregion

    #region Collision Helpers (省略なし)
    private bool IsHeadingIntoWall()
    {
        Vector3 direction = (_targetPosition - transform.position).normalized;
        if (direction == Vector3.zero) return false;
        return Physics.SphereCast(transform.position, _collisionCheckRadius, direction, out _, 2.0f, _combinedCollisionLayers);
    }

    public bool IsCeilingAbove(float distance)
    {
        // 上方に球体判定を飛ばして天井があるかチェック
        return Physics.SphereCast(transform.position, _collisionCheckRadius, Vector3.up, out _, distance, _ceilingLayer);
    }

    private Vector3 GetRandomPositionNearPlayer()
    {
        Vector3 finalPos = transform.position;
        for (int i = 0; i < _maxRelocateAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * _stoppingDistance;
            Vector3 candidatePos = _player.position + new Vector3(randomCircle.x, _hoverHeight + Random.Range(-2f, 4f), randomCircle.y);

            if (!Physics.CheckSphere(candidatePos, _collisionCheckRadius, _combinedCollisionLayers))
            {
                Vector3 dir = (candidatePos - transform.position);
                if (!Physics.Raycast(transform.position, dir.normalized, dir.magnitude, _wallLayer))
                {
                    return candidatePos;
                }
            }
        }
        return finalPos;
    }
    #endregion

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;
        if (bossHpBar != null) bossHpBar.value = _currentHealth;
        if (_currentHealth <= 0) Die();
    }

    private void Die()
    {
        _isDead = true;
        isActivated = false;
        _isActionInProgress = false;
        StopAllCoroutines();
        if (MissionManager.Instance != null) MissionManager.Instance.CompleteCurrentMission();
        Invoke(nameof(GoToClearScene), 3.0f);
    }

    private void GoToClearScene() => SceneManager.LoadScene("ClearScene2");
}