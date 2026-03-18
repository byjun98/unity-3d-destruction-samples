using UnityEngine;

[DisallowMultipleComponent]
public sealed class VoxelDemoShooter : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float explosionRadius = 1.2f;
    [SerializeField, Min(0f)] private float explosionDamage = 160f;
    [SerializeField, Min(0f)] private float explosionForce = 9f;
    [SerializeField, Min(0.1f)] private float range = 100f;
    [SerializeField, Min(0f)] private float fireCooldown = 0.15f;
    [SerializeField] private GameObject hitMarkerPrefab;

    private Camera shooterCamera;
    private float nextFireTime;

    private void Awake()
    {
        shooterCamera = GetComponent<Camera>();

        if (shooterCamera == null)
        {
            shooterCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot(GetMouseRay());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot(GetCenterRay());
        }
    }

    private Ray GetMouseRay()
    {
        if (shooterCamera != null)
        {
            return shooterCamera.ScreenPointToRay(Input.mousePosition);
        }

        return new Ray(transform.position, transform.forward);
    }

    private Ray GetCenterRay()
    {
        if (shooterCamera != null)
        {
            return shooterCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        return new Ray(transform.position, transform.forward);
    }

    private void Shoot(Ray ray)
    {
        nextFireTime = Time.time + fireCooldown;

        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, range))
        {
            return;
        }

        VoxelDestructibleBuilding building = hit.collider.GetComponentInParent<VoxelDestructibleBuilding>();

        if (building == null)
        {
            return;
        }

        building.ApplyExplosion(hit.point, explosionRadius, explosionDamage, explosionForce);
        SpawnHitMarker(hit.point, hit.normal);
    }

    private void SpawnHitMarker(Vector3 position, Vector3 normal)
    {
        if (hitMarkerPrefab == null)
        {
            return;
        }

        Quaternion rotation = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        GameObject marker = Instantiate(hitMarkerPrefab, position, rotation);
        Destroy(marker, 2f);
    }
}
