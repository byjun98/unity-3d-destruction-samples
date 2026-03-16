using UnityEngine;

public sealed class DemoImpactShooter : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform aimTarget;
    [SerializeField, Min(0.1f)] private float shotSpeed = 18f;
    [SerializeField, Min(0f)] private float fireCooldown = 0.2f;
    [SerializeField] private KeyCode keyboardFireKey = KeyCode.Space;
    [SerializeField] private bool fireWithMouse = true;
    [SerializeField] private AudioClip fireSound;

    [Header("Raycast Damage")]
    [SerializeField] private bool useRaycastDamage = true;
    [SerializeField, Min(1)] private int raycastDamage = 120;
    [SerializeField, Min(0.1f)] private float raycastRange = 30f;

    private float nextFireTime;

    private void Update()
    {
        bool shouldFire = Input.GetKeyDown(keyboardFireKey);

        if (fireWithMouse)
        {
            shouldFire |= Input.GetMouseButtonDown(0);
        }

        if (shouldFire)
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        if (projectilePrefab == null || Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;

        Transform origin = muzzle != null ? muzzle : transform;
        Vector3 direction = GetAimDirection(origin.position);

        if (useRaycastDamage && TryApplyRaycastDamage(origin.position, direction))
        {
            PlayFireSound(origin.position);
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, origin.position, Quaternion.LookRotation(direction));
        Rigidbody body = projectile.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.velocity = direction * shotSpeed;
        }

        PlayFireSound(origin.position);
    }

    private Vector3 GetAimDirection(Vector3 origin)
    {
        if (aimTarget != null)
        {
            Vector3 toTarget = aimTarget.position - origin;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                return toTarget.normalized;
            }
        }

        Camera aimCamera = GetComponent<Camera>();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (aimCamera != null)
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return ray.direction.normalized;
        }

        Vector3 forward = transform.forward;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private bool TryApplyRaycastDamage(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;

        if (!Physics.Raycast(origin, direction, out hit, raycastRange))
        {
            return false;
        }

        BreakableObject breakable = hit.collider.GetComponentInParent<BreakableObject>();

        if (breakable == null)
        {
            return false;
        }

        breakable.TakeDamage(raycastDamage, hit.point, hit.normal, direction);
        return true;
    }

    private void PlayFireSound(Vector3 position)
    {
        if (fireSound != null)
        {
            AudioSource.PlayClipAtPoint(fireSound, position);
        }
    }
}
