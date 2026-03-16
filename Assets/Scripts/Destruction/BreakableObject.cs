using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class BreakableObject : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int collisionDamageMultiplier = 8;

    [Header("Visuals")]
    [SerializeField] private GameObject intactVisual;
    [SerializeField] private GameObject fracturedPrefab;
    [SerializeField] private GameObject hitDecalPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject breakEffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip breakSound;

    [Header("Physics")]
    [SerializeField, Min(0f)] private float breakImpactVelocity = 6f;
    [SerializeField, Min(0f)] private float explosionForce = 8f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 2.5f;
    [SerializeField, Min(0f)] private float upwardsModifier = 0.25f;

    [Header("Cleanup")]
    [Tooltip("Seconds before spawned fractured pieces are removed. Set to 0 to keep them.")]
    [SerializeField, Min(0f)] private float fracturedLifetime = 8f;
    [SerializeField, Min(0.1f)] private float feedbackLifetime = 2f;
    [SerializeField, Min(0.1f)] private float decalLifetime = 12f;

    private int currentHealth;
    private bool isBroken;
    private DestructibleMaterialTag materialTag;

    public bool IsBroken => isBroken;
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        materialTag = GetComponent<DestructibleMaterialTag>();
        isBroken = false;

        if (intactVisual != null)
        {
            intactVisual.SetActive(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken || collision.collider.GetComponentInParent<ImpactProjectile>() != null)
        {
            return;
        }

        float impactVelocity = collision.relativeVelocity.magnitude;

        if (impactVelocity < breakImpactVelocity || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        int damage = Mathf.RoundToInt(impactVelocity * collisionDamageMultiplier);

        TakeDamage(damage, contact.point, contact.normal, collision.relativeVelocity.normalized);
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, Vector3 forceDirection)
    {
        if (isBroken)
        {
            return;
        }

        int scaledDamage = Mathf.RoundToInt(Mathf.Max(0, damage) * GetDamageMultiplier());
        currentHealth -= scaledDamage;

        SpawnHitFeedback(hitPoint, hitNormal);

        if (currentHealth <= 0)
        {
            Break(hitPoint, forceDirection);
        }
    }

    [ContextMenu("Break Now")]
    private void BreakNow()
    {
        TakeDamage(maxHealth, transform.position, Vector3.up, transform.forward);
    }

    private void SpawnHitFeedback(Vector3 hitPoint, Vector3 hitNormal)
    {
        GameObject decalPrefab = Resolve(materialTag != null ? materialTag.HitDecalPrefab : null, hitDecalPrefab);
        GameObject effectPrefab = Resolve(materialTag != null ? materialTag.HitEffectPrefab : null, hitEffectPrefab);
        AudioClip clip = Resolve(materialTag != null ? materialTag.HitSound : null, hitSound);

        if (decalPrefab != null)
        {
            Quaternion decalRotation = SurfaceRotation(hitNormal, true);
            Vector3 decalPosition = hitPoint + hitNormal.normalized * 0.01f;
            GameObject decal = Instantiate(decalPrefab, decalPosition, decalRotation);
            Destroy(decal, decalLifetime);
        }

        SpawnFeedbackPrefab(effectPrefab, hitPoint, SurfaceRotation(hitNormal, false), feedbackLifetime);
        PlayClip(clip, hitPoint);
    }

    private void Break(Vector3 hitPoint, Vector3 forceDirection)
    {
        if (isBroken)
        {
            return;
        }

        isBroken = true;

        SpawnFeedbackPrefab(Resolve(materialTag != null ? materialTag.BreakEffectPrefab : null, breakEffectPrefab),
            hitPoint, SurfaceRotation(Vector3.up, false), feedbackLifetime);
        PlayClip(Resolve(materialTag != null ? materialTag.BreakSound : null, breakSound), hitPoint);

        if (intactVisual != null)
        {
            intactVisual.SetActive(false);
        }

        Collider[] ownColliders = GetComponents<Collider>();
        for (int i = 0; i < ownColliders.Length; i++)
        {
            ownColliders[i].enabled = false;
        }

        if (fracturedPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        GameObject fracturedObject = Instantiate(fracturedPrefab, transform.position, transform.rotation);
        fracturedObject.transform.localScale = transform.lossyScale;

        MeshCollider[] meshColliders = fracturedObject.GetComponentsInChildren<MeshCollider>();
        for (int i = 0; i < meshColliders.Length; i++)
        {
            meshColliders[i].convex = true;
        }

        Rigidbody[] rigidbodies = fracturedObject.GetComponentsInChildren<Rigidbody>();
        float force = explosionForce * GetExplosionMultiplier();

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];
            body.isKinematic = false;
            body.useGravity = true;

            Vector3 direction = body.worldCenterOfMass - hitPoint;

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = forceDirection.sqrMagnitude > 0.01f ? forceDirection.normalized : Random.onUnitSphere;
            }
            else
            {
                direction.Normalize();
            }

            body.AddExplosionForce(force, hitPoint, explosionRadius, upwardsModifier, ForceMode.Impulse);
            body.AddForce(direction * force * 0.4f, ForceMode.Impulse);
        }

        if (fracturedLifetime > 0f)
        {
            Destroy(fracturedObject, fracturedLifetime);
        }

        Destroy(gameObject);
    }

    private float GetDamageMultiplier()
    {
        return materialTag != null ? materialTag.DamageMultiplier : 1f;
    }

    private float GetExplosionMultiplier()
    {
        return materialTag != null ? materialTag.ExplosionForceMultiplier : 1f;
    }

    private static T Resolve<T>(T taggedValue, T fallbackValue) where T : Object
    {
        return taggedValue != null ? taggedValue : fallbackValue;
    }

    private static void SpawnFeedbackPrefab(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        Destroy(instance, lifetime);
    }

    private static void PlayClip(AudioClip clip, Vector3 position)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position);
    }

    private static Quaternion SurfaceRotation(Vector3 normal, bool faceIntoSurface)
    {
        Vector3 forward = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;

        if (faceIntoSurface)
        {
            forward = -forward;
        }

        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(forward, up);
    }
}
