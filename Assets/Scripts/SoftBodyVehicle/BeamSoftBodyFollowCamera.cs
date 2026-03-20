using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeamSoftBodyFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.3f, 0f);

    [Header("Chase Cam")]
    [SerializeField, Min(0.5f)] private float chaseHeight = 4.2f;
    [SerializeField, Min(1f)] private float chaseDistance = 9.5f;
    [SerializeField, Range(0.01f, 1f)] private float positionSmoothTime = 0.18f;
    [SerializeField, Range(0.01f, 1f)] private float rotationSmoothTime = 0.12f;

    [Header("Orbit (Right Mouse Button)")]
    [SerializeField, Min(2f)] private float minDistance = 4f;
    [SerializeField, Min(4f)] private float maxDistance = 22f;
    [SerializeField, Min(0f)] private float orbitYawSpeed = 280f;
    [SerializeField, Min(0f)] private float orbitPitchSpeed = 180f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField, Min(0f)] private float zoomSpeed = 1.4f;
    [SerializeField, Min(0f)] private float orbitReturnDelay = 1.2f;

    [Header("Shake")]
    [SerializeField, Min(0f)] private float shakeDecay = 6f;

    private Vector3 velocity;
    private float shakeAmount;
    private float orbitYaw;
    private float orbitPitch = 14f;
    private float orbitDistance;
    private float lastOrbitInputTime = -10f;
    private bool orbitInitialized;

    public Transform Target
    {
        get { return target; }
        set { target = value; }
    }

    public void AddShake(float amount)
    {
        shakeAmount = Mathf.Max(shakeAmount, amount);
    }

    private void Awake()
    {
        orbitDistance = chaseDistance;
    }

    private void Update()
    {
        if (target != null && !orbitInitialized)
        {
            orbitYaw = target.eulerAngles.y;
            orbitInitialized = true;
        }

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            orbitYaw += mouseX * orbitYawSpeed * Time.deltaTime;
            orbitPitch -= mouseY * orbitPitchSpeed * Time.deltaTime;
            orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);
            lastOrbitInputTime = Time.time;
        }

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            orbitDistance -= scroll * zoomSpeed;
            orbitDistance = Mathf.Clamp(orbitDistance, minDistance, maxDistance);
            lastOrbitInputTime = Time.time;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        bool freelook = Input.GetMouseButton(1) || (Time.time - lastOrbitInputTime < orbitReturnDelay);
        float yaw;
        float pitch;
        float distance;

        if (freelook)
        {
            yaw = orbitYaw;
            pitch = orbitPitch;
            distance = orbitDistance;
        }
        else
        {
            float targetYaw = target.eulerAngles.y;
            yaw = Mathf.LerpAngle(orbitYaw, targetYaw, 1f - Mathf.Exp(-Time.deltaTime / 0.6f));
            orbitYaw = yaw;
            pitch = Mathf.Lerp(orbitPitch, 14f, 1f - Mathf.Exp(-Time.deltaTime / 0.8f));
            orbitPitch = pitch;
            distance = Mathf.Lerp(orbitDistance, chaseDistance, 1f - Mathf.Exp(-Time.deltaTime / 0.8f));
            orbitDistance = distance;
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 boomEnd = rotation * new Vector3(0f, 0f, -distance);
        Vector3 desired = target.position + new Vector3(0f, chaseHeight * 0.25f, 0f) + boomEnd;
        desired.y = Mathf.Max(desired.y, target.position.y + 0.6f);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, positionSmoothTime);

        Vector3 lookTarget = target.position + lookOffset;
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
        float rotationLerp = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, rotationSmoothTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerp);

        if (shakeAmount > 0.0001f)
        {
            transform.position += Random.insideUnitSphere * shakeAmount;
            shakeAmount = Mathf.Max(0f, shakeAmount - shakeDecay * shakeAmount * Time.deltaTime);

            if (shakeAmount < 0.001f)
            {
                shakeAmount = 0f;
            }
        }
    }
}
