using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StressCollapseShooter : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField, Min(0.1f)] private float range = 80f;
    [SerializeField, Min(0f)] private float damage = 22f;
    [SerializeField, Min(0f)] private float impulse = 8f;
    [SerializeField, Min(0f)] private float cooldown = 0.12f;
    [SerializeField] private KeyCode keyboardFireKey = KeyCode.Space;
    [SerializeField] private bool fireWithMouse = true;
    [SerializeField] private GameObject hitEffectPrefab;

    private float nextFireTime;

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = GetComponent<Camera>();
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (Input.GetKeyDown(keyboardFireKey))
        {
            Fire(GetCenterRay());
            return;
        }

        if (fireWithMouse && Input.GetMouseButtonDown(0))
        {
            Fire(GetMouseRay());
        }
    }

    private Ray GetCenterRay()
    {
        if (aimCamera != null)
        {
            return aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        return new Ray(transform.position, transform.forward);
    }

    private Ray GetMouseRay()
    {
        if (aimCamera != null)
        {
            return aimCamera.ScreenPointToRay(Input.mousePosition);
        }

        return new Ray(transform.position, transform.forward);
    }

    private void Fire(Ray ray)
    {
        nextFireTime = Time.time + cooldown;
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, range))
        {
            return;
        }

        Vector3 forceDirection = ray.direction.sqrMagnitude > 0.001f ? ray.direction.normalized : transform.forward;
        StressDamageReceiver receiver = hit.collider.GetComponentInParent<StressDamageReceiver>();

        if (receiver != null)
        {
            receiver.ApplyImpact(damage, hit.point, hit.normal, forceDirection);
        }

        Rigidbody hitBody = hit.rigidbody;

        if (hitBody != null && !hitBody.isKinematic)
        {
            hitBody.AddForceAtPosition(forceDirection * impulse, hit.point, ForceMode.Impulse);
        }

        SpawnHitEffect(hit.point, hit.normal);
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        Vector3 forward = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(forward, up));
        Destroy(effect, 3f);
    }
}
