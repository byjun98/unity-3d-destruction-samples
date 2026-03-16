using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class ImpactProjectile : MonoBehaviour
{
    [SerializeField, Min(0)] private int baseDamage = 35;
    [SerializeField, Min(0f)] private float damagePerSpeed = 4f;
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        BreakableObject breakable = collision.collider.GetComponentInParent<BreakableObject>();

        if (breakable != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            Vector3 velocity = body != null ? body.velocity : transform.forward;
            Vector3 forceDirection = velocity.sqrMagnitude > 0.01f ? velocity.normalized : transform.forward;
            int damage = baseDamage + Mathf.RoundToInt(velocity.magnitude * damagePerSpeed);

            breakable.TakeDamage(damage, contact.point, contact.normal, forceDirection);
        }

        Destroy(gameObject);
    }
}
