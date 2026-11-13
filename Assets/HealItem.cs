using UnityEngine;

/// <summary>
/// 地面に置く回復アイテム。プレイヤーが触れると HP を回復して消える。
/// HP が満タンのときは拾わない（アイテムは残る）。
/// くるくる回りながら上下にふわふわ動く。
/// </summary>
[RequireComponent(typeof(Collider))]
public class HealItem : MonoBehaviour
{
    [Tooltip("回復量")]
    public int healAmount = 25;

    [Header("見た目の動き")]
    public float rotateSpeed = 90f;   // 度/秒
    public float bobHeight = 0.15f;   // 上下の振れ幅 (m)
    public float bobSpeed = 2f;

    private Vector3 basePosition;

    void Awake()
    {
        // 触れたら判定だけ取る（ぶつからない）
        GetComponent<Collider>().isTrigger = true;

        // CharacterController との接触を確実に検知するため、動かない Rigidbody を付ける
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = basePosition + Vector3.up * offset;
    }

    void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (health.Heal(healAmount))
        {
            Destroy(gameObject);
        }
    }
}
