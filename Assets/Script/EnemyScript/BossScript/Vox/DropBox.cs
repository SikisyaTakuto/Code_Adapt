using UnityEngine;
using System.Collections;

public class DropBox : MonoBehaviour
{
    [Header("中身の確率設定")]
    [Range(0f, 1f)]
    public float bombChance = 0.7f;  // 爆弾の確率 0〜1

    [Header("中身のプレハブ")]
    public GameObject bombPrefab;
    public GameObject[] enemyPrefabs;
    public GameObject breakEffect;

    [SerializeField]
    private Vector3 breakEffectOffset = new Vector3(0f, 0.5f, 0f);

    private bool alreadyOpened = false;

    private Animator animator;

    private int groundLayer;

    void Start()
    {
        // Ground レイヤー番号を取得
        groundLayer = LayerMask.NameToLayer("Ground");
        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != groundLayer) return;
        if (alreadyOpened) return;

        alreadyOpened = true;

        // アニメーション再生
        if (animator != null)
        {
            animator.SetTrigger("Open");
            StartCoroutine(OnBreakAnimationEnd());
        }

        // アニメーション後に中身を出したい場合は
        // Destroy をすぐに呼ばない方がいい
    }

    IEnumerator OnBreakAnimationEnd()
    {
        SpawnRandomItem();

        if (breakEffect != null)
        {
            Instantiate(
                breakEffect,
                transform.position + breakEffectOffset,
                Quaternion.identity
            );
        }
        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }


    private void SpawnRandomItem()
    {
        float r = Random.value;
        GameObject prefab = null;

        if (r < bombChance)
        {
            prefab = bombPrefab;
        }
        else
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            }
            else
            {
                prefab = bombPrefab;
            }
        }

        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
