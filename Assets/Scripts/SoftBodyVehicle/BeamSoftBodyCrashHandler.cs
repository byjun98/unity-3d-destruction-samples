using UnityEngine;

[RequireComponent(typeof(BeamSoftBodyVehicle))]
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class BeamSoftBodyCrashHandler : MonoBehaviour
{
    [Header("Impact thresholds (m/s)")]
    [SerializeField, Min(0f)] private float minImpactSpeed = 1.6f;
    [SerializeField, Min(0f)] private float lightImpactSpeed = 4.5f;
    [SerializeField, Min(0f)] private float mediumImpactSpeed = 9.5f;
    [SerializeField, Min(0f)] private float heavyImpactSpeed = 16f;

    [Header("Damage scaling")]
    [SerializeField, Min(0f)] private float impactForcePerSpeedUnit = 5f;
    [SerializeField, Min(0f)] private float impactRadiusBase = 1f;
    [SerializeField, Min(0f)] private float impactRadiusPerSpeed = 0.06f;
    [SerializeField, Min(0f)] private float maxImpactForce = 320f;
    [SerializeField, Min(0f)] private float maxImpactRadius = 3f;

    [Header("Stay-collision (continuous scrape)")]
    [SerializeField, Min(0f)] private float stayMinImpactSpeed = 6.5f;
    [SerializeField, Min(0f)] private float stayCooldown = 0.12f;
    [SerializeField, Min(0f)] private float stayForceScale = 0.45f;

    [Header("VFX prefabs")]
    [SerializeField] private GameObject minorImpactVfx;
    [SerializeField] private GameObject mediumImpactVfx;
    [SerializeField] private GameObject heavyImpactVfx;
    [SerializeField] private GameObject smokeVfx;
    [SerializeField] private GameObject sparkVfx;
    [SerializeField] private Material vfxParticleMaterial;

    [Header("Camera shake")]
    [SerializeField] private BeamSoftBodyFollowCamera shakeTarget;
    [SerializeField, Min(0f)] private float shakeScale = 0.05f;

    private BeamSoftBodyVehicle softBody;
    private Rigidbody body;
    private float lastImpactTime = -10f;
    private float lastProcessedTime = -10f;
    private float lastImpactIntensity;

    public float LastImpactIntensity
    {
        get { return lastImpactIntensity; }
    }

    public float LastImpactTime
    {
        get { return lastImpactTime; }
    }

    private void Awake()
    {
        softBody = GetComponent<BeamSoftBodyVehicle>();
        body = GetComponent<Rigidbody>();

        if (shakeTarget == null)
        {
            shakeTarget = FindObjectOfType<BeamSoftBodyFollowCamera>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision, false);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastProcessedTime < stayCooldown)
        {
            return;
        }

        ProcessCollision(collision, true);
    }

    private void ProcessCollision(Collision collision, bool isStay)
    {
        if (collision.contactCount == 0)
        {
            return;
        }

        float speed = collision.relativeVelocity.magnitude;
        float threshold = isStay ? stayMinImpactSpeed : minImpactSpeed;

        if (speed < threshold)
        {
            return;
        }

        ContactPoint best = collision.GetContact(0);
        float bestScore = -1f;

        Vector3 relativeVelocityDir = collision.relativeVelocity.sqrMagnitude > 0.001f
            ? collision.relativeVelocity.normalized
            : -transform.forward;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            float score = Mathf.Abs(Vector3.Dot(contact.normal, relativeVelocityDir));

            if (score > bestScore)
            {
                bestScore = score;
                best = contact;
            }
        }

        Vector3 worldPoint = best.point;
        Vector3 worldDirection = best.normal;

        if (worldDirection.sqrMagnitude < 0.001f)
        {
            worldDirection = -relativeVelocityDir;
        }

        float scale = isStay ? stayForceScale : 1f;
        float force = Mathf.Min(maxImpactForce, speed * impactForcePerSpeedUnit) * scale;
        float radius = Mathf.Min(maxImpactRadius, impactRadiusBase + speed * impactRadiusPerSpeed);

        softBody.ApplyImpact(worldPoint, worldDirection.normalized, force, radius);

        if (!isStay)
        {
            Vector3 dampDirection = Vector3.Project(body.velocity, best.normal);
            body.AddForce(-dampDirection * speed * 0.35f, ForceMode.Impulse);
            SpawnImpactVfx(worldPoint, best.normal, speed);
            TriggerCameraShake(speed);
            lastImpactTime = Time.time;
            lastImpactIntensity = speed;
        }
        else
        {
            SpawnImpactVfx(worldPoint, best.normal, speed * 0.55f);
        }

        lastProcessedTime = Time.time;
    }

    private void SpawnImpactVfx(Vector3 point, Vector3 normal, float intensity)
    {
        Vector3 forward = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        float t = Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed * 1.4f, intensity);
        float visualScale = Mathf.Lerp(0.6f, 2.4f, t);

        GameObject primary = SelectPrimaryVfx(intensity);

        if (primary != null)
        {
            SpawnVfx(primary, point, rotation, visualScale, 4f);
        }

        if (sparkVfx != null && intensity >= lightImpactSpeed)
        {
            SpawnVfx(sparkVfx, point, rotation, Mathf.Lerp(0.7f, 1.6f, t), 2.5f);
        }

        if (smokeVfx != null && intensity >= mediumImpactSpeed)
        {
            SpawnVfx(smokeVfx, point, Quaternion.LookRotation(Vector3.up), Mathf.Lerp(0.8f, 1.8f, t), 5f);
        }
    }

    private void SpawnVfx(GameObject prefab, Vector3 point, Quaternion rotation, float scale, float life)
    {
        GameObject fx = Instantiate(prefab, point, rotation);
        ReplaceParticleMaterials(fx);
        UrpMaterialFixer.FixHierarchy(fx);
        fx.transform.localScale *= scale;
        Destroy(fx, life);
    }

    private void ReplaceParticleMaterials(GameObject root)
    {
        if (vfxParticleMaterial == null || root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r == null)
            {
                continue;
            }

            Material[] sourceMats = r.sharedMaterials;

            if (sourceMats == null || sourceMats.Length == 0)
            {
                continue;
            }

            Material[] result = new Material[sourceMats.Length];

            for (int j = 0; j < sourceMats.Length; j++)
            {
                Material src = sourceMats[j];

                if (src == null)
                {
                    result[j] = null;
                    continue;
                }

                if (IsAlreadyUrp(src.shader))
                {
                    result[j] = src;
                    continue;
                }

                Material clone = new Material(vfxParticleMaterial);
                clone.name = src.name + "_VfxClone";

                Texture mainTex = null;

                if (src.HasProperty("_MainTex"))
                {
                    mainTex = src.GetTexture("_MainTex");
                }
                else if (src.HasProperty("_BaseMap"))
                {
                    mainTex = src.GetTexture("_BaseMap");
                }

                if (mainTex != null)
                {
                    if (clone.HasProperty("_BaseMap"))
                    {
                        clone.SetTexture("_BaseMap", mainTex);
                    }

                    if (clone.HasProperty("_MainTex"))
                    {
                        clone.SetTexture("_MainTex", mainTex);
                    }
                }

                Color tint = Color.white;

                if (src.HasProperty("_TintColor"))
                {
                    tint = src.GetColor("_TintColor");
                }
                else if (src.HasProperty("_Color"))
                {
                    tint = src.GetColor("_Color");
                }
                else if (src.HasProperty("_BaseColor"))
                {
                    tint = src.GetColor("_BaseColor");
                }

                if (clone.HasProperty("_BaseColor"))
                {
                    clone.SetColor("_BaseColor", tint);
                }

                if (clone.HasProperty("_Color"))
                {
                    clone.SetColor("_Color", tint);
                }

                result[j] = clone;
            }

            r.sharedMaterials = result;
        }
    }

    private static bool IsAlreadyUrp(Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        return shader.name.StartsWith("Universal Render Pipeline/", System.StringComparison.OrdinalIgnoreCase);
    }

    private GameObject SelectPrimaryVfx(float intensity)
    {
        if (intensity >= heavyImpactSpeed && heavyImpactVfx != null)
        {
            return heavyImpactVfx;
        }

        if (intensity >= mediumImpactSpeed && mediumImpactVfx != null)
        {
            return mediumImpactVfx;
        }

        return minorImpactVfx;
    }

    private void TriggerCameraShake(float intensity)
    {
        if (shakeTarget == null)
        {
            return;
        }

        float t = Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed, intensity);
        shakeTarget.AddShake(Mathf.Lerp(0.05f, 1f, t) * shakeScale);
    }
}
