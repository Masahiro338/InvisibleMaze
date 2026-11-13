using UnityEngine;

/// <summary>
/// 床に置くダメージトラップ。プレイヤーが踏むとダメージを受ける。
/// 乗り続けていると、一定間隔ごとに繰り返しダメージを受ける。
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageTrap : MonoBehaviour
{
    [Tooltip("1回あたりのダメージ量")]
    public int damage = 10;
    [Tooltip("乗り続けたときに次のダメージが入るまでの秒数")]
    public float damageInterval = 1f;
    [Tooltip("オフにすると見えないトラップになる")]
    public bool visible = true;

    private float nextDamageTime = 0f;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        // CharacterController との接触を確実に検知するため、動かない Rigidbody を付ける
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var r = GetComponent<Renderer>();
        if (r != null) r.enabled = visible;
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider other)
    {
        if (Time.time < nextDamageTime) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.TakeDamage(damage);
        nextDamageTime = Time.time + damageInterval;
    }
}
