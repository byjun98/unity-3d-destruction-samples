using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class StressDamageReceiver : MonoBehaviour
{
    [Header("Stress Target")]
    [SerializeField] private StressCollapseBuilding building;
    [SerializeField, Min(0)] private int layerIndex;

    [Header("Damage")]
    [SerializeField, Min(1f)] private float maxHealth = 45f;
    [SerializeField, Min(0f)] private float stressDamageScale = 0.35f;
    [SerializeField, Min(0f)] private float stressDamageOnBreak = 12f;

    [Header("Break Response")]
    [SerializeField] private bool releasePieceOnBreak = true;
    [SerializeField, Min(0f)] private float releaseImpulse = 3f;
    [SerializeField, Min(0f)] private float brokenLifetime = 12f;

    [Header("Collision Damage")]
    [SerializeField] private bool takeCollisionDamage = true;
    [SerializeField, Min(0f)] private float minimumCollisionSpeed = 4f;
    [SerializeField, Min(0f)] private float collisionDamageScale = 3f;

    [Header("Feedback")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject breakEffectPrefab;
    [SerializeField, Min(0.1f)] private float feedbackLifetime = 3f;
    [SerializeField] private Color damagedTint = new Color(1f, 0.42f, 0.24f, 1f);

    private Renderer[] renderers;
    private Collider[] colliders;
    private Rigidbody body;
    private MaterialPropertyBlock propertyBlock;
    private float health;
    private bool broken;

    private void Awake()
    {
        health = Mathf.Max(1f, maxHealth);
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        body = GetComponent<Rigidbody>();
        propertyBlock = new MaterialPropertyBlock();

        if (building == null)
        {
            building = GetComponentInParent<StressCollapseBuilding>();
        }
    }

    public void ConfigureForDemo(
        StressCollapseBuilding owner,
        int targetLayer,
        float configuredHealth,
        float configuredStressDamageScale,
        float configuredBreakStressDamage,
        GameObject configuredHitEffect,
        GameObject configuredBreakEffect)
    {
        building = owner;
        layerIndex = Mathf.Max(0, targetLayer);
        maxHealth = Mathf.Max(1f, configuredHealth);
        health = maxHealth;
        stressDamageScale = Mathf.Max(0f, configuredStressDamageScale);
        stressDamageOnBreak = Mathf.Max(0f, configuredBreakStressDamage);
        hitEffectPrefab = configuredHitEffect;
        breakEffectPrefab = configuredBreakEffect;
    }

    public bool ApplyImpact(float damage, Vector3 hitPoint, Vector3 hitNormal, Vector3 forceDirection)
    {
        if (broken)
        {
            return false;
        }

        float clampedDamage = Mathf.Max(0f, damage);

        if (clampedDamage <= 0f)
        {
            return false;
        }

        health = Mathf.Max(0f, health - clampedDamage);
        ApplyDamageTint();
        SpawnFeedback(hitEffectPrefab, hitPoint, SurfaceRotation(hitNormal));

        if (building != null)
        {
            building.DamageLayerAt(layerIndex, clampedDamage * stressDamageScale, hitPoint);
        }

        if (health <= 0f)
        {
            Break(hitPoint, forceDirection);
        }

        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!takeCollisionDamage || broken || collision.contactCount == 0)
        {
            return;
        }

        float speed = collision.relativeVelocity.magnitude;

        if (speed < minimumCollisionSpeed)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        ApplyImpact(speed * collisionDamageScale, contact.point, contact.normal, collision.relativeVelocity);
    }

    private void Break(Vector3 hitPoint, Vector3 forceDirection)
    {
        if (broken)
        {
            return;
        }

        broken = true;
        SpawnFeedback(breakEffectPrefab, hitPoint, Quaternion.identity);

        if (building != null)
        {
            building.DamageLayerAt(layerIndex, stressDamageOnBreak, hitPoint);
        }

        if (releasePieceOnBreak && body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();

            Vector3 impulseDirection = forceDirection.sqrMagnitude > 0.001f ? forceDirection.normalized : Random.onUnitSphere;
            body.AddForce(impulseDirection * releaseImpulse, ForceMode.Impulse);

            if (brokenLifetime > 0f)
            {
                Destroy(gameObject, brokenLifetime);
            }

            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private void ApplyDamageTint()
    {
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        float damageRatio = 1f - Mathf.Clamp01(health / Mathf.Max(1f, maxHealth));
        Color tint = Color.Lerp(Color.white, damagedTint, damageRatio);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", tint);
            propertyBlock.SetColor("_Color", tint);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void SpawnFeedback(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(prefab, position, rotation);
        Destroy(effect, feedbackLifetime);
    }

    private static Quaternion SurfaceRotation(Vector3 normal)
    {
        Vector3 forward = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(forward, up);
    }
}
