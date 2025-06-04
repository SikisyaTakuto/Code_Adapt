using UnityEngine;

public class BitBehavior : MonoBehaviour
{
    public float speed = 10f;
    public float rotationSpeed = 720f;
    public float attackRange = 15f;
    public int damage = 20;
    public float lifeTime = 10f;

    public float ascendDuration = 2.0f;
    public float ascendSpeed = 20f;
    public float returnAscendHeight = 10f;

    private Transform ownerTransform;
    private Transform idlePosition;
    private Rigidbody rb;
    private PlayerBitController owner;

    private enum State
    {
        Idle,
        Ascending,
        Seeking,
        ReturningAscending,
        ReturningToIdle
    }

    private State currentState = State.Idle;

    private Transform targetEnemy;
    private float stateTimer;
    private Vector3 returnTargetAboveIdle;

    public void Initialize(PlayerBitController ownerController, Transform idlePos)
    {
        owner = ownerController;
        ownerTransform = owner.transform;
        idlePosition = idlePos;
        rb = GetComponent<Rigidbody>();

        transform.position = idlePosition.position;
        transform.rotation = idlePosition.rotation;
        currentState = State.Idle;

        stateTimer = 0f;
    }

    public bool IsIdle()
    {
        return currentState == State.Idle;
    }

    public void Launch()
    {
        if (currentState == State.Idle)
        {
            currentState = State.Ascending;
            stateTimer = 0f;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                transform.position = idlePosition.position;
                transform.rotation = idlePosition.rotation;
                rb.linearVelocity = Vector3.zero;
                break;

            case State.Ascending:
                stateTimer += Time.deltaTime;
                rb.linearVelocity = Vector3.up * ascendSpeed;
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

                if (stateTimer >= ascendDuration)
                {
                    currentState = State.Seeking;
                    stateTimer = 0f;
                }
                break;

            case State.Seeking:
                stateTimer += Time.deltaTime;
                if (stateTimer > lifeTime)
                {
                    StartReturn();
                    break;
                }

                FindClosestEnemy();

                if (targetEnemy != null)
                {
                    Vector3 dir = (targetEnemy.position - transform.position).normalized;
                    rb.linearVelocity = dir * speed;
                }
                else
                {
                    rb.linearVelocity = transform.forward * (speed * 0.5f);
                }

                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                break;

            case State.ReturningAscending:
                Vector3 toAbove = returnTargetAboveIdle - transform.position;
                if (toAbove.sqrMagnitude < 0.05f)
                {
                    currentState = State.ReturningToIdle;
                }
                else
                {
                    rb.linearVelocity = toAbove.normalized * ascendSpeed;
                    transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                }
                break;

            case State.ReturningToIdle:
                Vector3 returnDir = idlePosition.position - transform.position;
                if (returnDir.sqrMagnitude < 0.05f)
                {
                    currentState = State.Idle;
                    rb.linearVelocity = Vector3.zero;
                    transform.position = idlePosition.position;
                    transform.rotation = idlePosition.rotation;
                    targetEnemy = null;
                }
                else
                {
                    rb.linearVelocity = returnDir.normalized * speed;
                    transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                }
                break;
        }
    }

    void FindClosestEnemy()
    {
        float closestDist = attackRange;
        targetEnemy = null;

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                targetEnemy = enemy.transform;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (currentState != State.Seeking) return;

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            StartReturn();
        }
        else if (!other.CompareTag("Player"))
        {
            StartReturn();
        }
    }

    void StartReturn()
    {
        targetEnemy = null;
        returnTargetAboveIdle = idlePosition.position + Vector3.up * returnAscendHeight;
        currentState = State.ReturningAscending;
        stateTimer = 0f;
    }
}
