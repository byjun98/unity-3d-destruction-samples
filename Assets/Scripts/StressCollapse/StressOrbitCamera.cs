using UnityEngine;

[DisallowMultipleComponent]
public sealed class StressOrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField, Min(1f)] private float distance = 18f;
    [SerializeField, Min(1f)] private float minDistance = 6f;
    [SerializeField, Min(1f)] private float maxDistance = 60f;
    [SerializeField] private float yaw = 35f;
    [SerializeField, Range(-89f, 89f)] private float pitch = 22f;

    [SerializeField, Min(0.1f)] private float orbitSpeed = 220f;
    [SerializeField, Min(0.1f)] private float zoomSpeed = 14f;
    [SerializeField, Min(0.1f)] private float panSpeed = 0.05f;
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.18f;
    [SerializeField] private bool autoOrbitWhenIdle = true;
    [SerializeField, Range(0f, 80f)] private float idleOrbitSpeed = 6f;
    [SerializeField, Min(0f)] private float idleDelay = 4f;

    private Vector3 pivotOffset;
    private Vector3 smoothedPosition;
    private Quaternion smoothedRotation;
    private float lastInteractionTime;

    private void Awake()
    {
        if (pivot == null)
        {
            GameObject go = new GameObject("OrbitPivot");
            go.transform.position = transform.position + transform.forward * distance;
            pivot = go.transform;
        }

        smoothedPosition = transform.position;
        smoothedRotation = transform.rotation;
        lastInteractionTime = Time.time;
    }

    public void SetPivot(Transform newPivot, float? targetDistance = null)
    {
        pivot = newPivot;

        if (targetDistance.HasValue)
        {
            distance = Mathf.Clamp(targetDistance.Value, minDistance, maxDistance);
        }
    }

    private void Update()
    {
        if (pivot == null)
        {
            return;
        }

        bool interacted = false;

        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
            float dy = Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
            yaw += dx;
            pitch = Mathf.Clamp(pitch - dy, -89f, 89f);
            interacted = true;
        }

        if (Input.GetMouseButton(2))
        {
            float dx = Input.GetAxis("Mouse X") * distance * panSpeed;
            float dy = Input.GetAxis("Mouse Y") * distance * panSpeed;
            Vector3 right = transform.right * -dx;
            Vector3 up = transform.up * -dy;
            pivotOffset += right + up;
            interacted = true;
        }

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            interacted = true;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            yaw -= orbitSpeed * 0.5f * Time.deltaTime;
            interacted = true;
        }

        if (Input.GetKey(KeyCode.E))
        {
            yaw += orbitSpeed * 0.5f * Time.deltaTime;
            interacted = true;
        }

        if (interacted)
        {
            lastInteractionTime = Time.time;
        }
        else if (autoOrbitWhenIdle && Time.time - lastInteractionTime > idleDelay)
        {
            yaw += idleOrbitSpeed * Time.deltaTime;
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPivot = pivot.position + pivotOffset;
        Vector3 desiredPos = targetPivot - rotation * Vector3.forward * distance;

        if (smoothing > 0f)
        {
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(smoothing), Time.deltaTime * 60f);
            smoothedPosition = Vector3.Lerp(smoothedPosition, desiredPos, t);
            smoothedRotation = Quaternion.Slerp(smoothedRotation, rotation, t);
        }
        else
        {
            smoothedPosition = desiredPos;
            smoothedRotation = rotation;
        }

        transform.position = smoothedPosition;
        transform.rotation = smoothedRotation;
    }
}
