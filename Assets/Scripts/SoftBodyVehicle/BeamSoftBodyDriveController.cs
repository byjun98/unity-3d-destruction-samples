using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class BeamSoftBodyDriveController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField, Min(0f)] private float accelerationForce = 22000f;
    [SerializeField, Min(0f)] private float reverseForce = 11000f;
    [SerializeField, Min(0f)] private float brakeForce = 26000f;
    [SerializeField, Min(0f)] private float maxSpeed = 24f;
    [SerializeField, Min(0f)] private float maxReverseSpeed = 9f;
    [SerializeField, Min(0f)] private float steerTorque = 11000f;
    [SerializeField, Min(0f)] private float lateralFriction = 9f;
    [SerializeField, Min(0f)] private float rollingResistance = 0.55f;
    [SerializeField, Min(0.05f)] private float groundCheckDistance = 1.4f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Vector3 centerOfMass = new Vector3(0f, -0.25f, 0f);

    [Header("Visual Wheels")]
    [SerializeField] private Transform[] visualWheels;
    [SerializeField, Min(0.05f)] private float wheelRadius = 0.36f;
    [SerializeField] private bool autoDetectWheelSpinAxis = true;
    [SerializeField] private Vector3 wheelSpinAxisOverride = Vector3.right;

    private Vector3[] wheelSpinAxes;

    [Header("Input")]
    [SerializeField] private KeyCode forwardKey = KeyCode.W;
    [SerializeField] private KeyCode reverseKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode handbrakeKey = KeyCode.Space;
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    [Header("Reset")]
    [SerializeField] private bool reloadSceneOnReset = true;
    [SerializeField] private Transform respawnPoint;

    private Rigidbody body;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float throttle;
    private float steer;
    private bool handbraking;
    private bool isGrounded;

    public Rigidbody Body
    {
        get { return body; }
    }

    public float ForwardSpeed
    {
        get { return body == null ? 0f : Vector3.Dot(body.velocity, transform.forward); }
    }

    public float Speed
    {
        get { return body == null ? 0f : body.velocity.magnitude; }
    }

    public float Throttle
    {
        get { return throttle; }
    }

    public float Steer
    {
        get { return steer; }
    }

    public bool Handbraking
    {
        get { return handbraking; }
    }

    public bool IsGrounded
    {
        get { return isGrounded; }
    }

    public void AssignVisualWheels(Transform[] wheels)
    {
        visualWheels = wheels;
        wheelSpinAxes = null;
    }

    private void EnsureWheelSpinAxes()
    {
        if (visualWheels == null || visualWheels.Length == 0)
        {
            wheelSpinAxes = null;
            return;
        }

        if (wheelSpinAxes != null && wheelSpinAxes.Length == visualWheels.Length)
        {
            return;
        }

        wheelSpinAxes = new Vector3[visualWheels.Length];

        for (int i = 0; i < visualWheels.Length; i++)
        {
            wheelSpinAxes[i] = ResolveSpinAxis(visualWheels[i]);
        }
    }

    private Vector3 ResolveSpinAxis(Transform wheel)
    {
        if (wheel == null || !autoDetectWheelSpinAxis)
        {
            return wheelSpinAxisOverride.sqrMagnitude > 0.0001f ? wheelSpinAxisOverride.normalized : Vector3.right;
        }

        Renderer wheelRenderer = wheel.GetComponent<Renderer>();

        if (wheelRenderer == null)
        {
            Renderer[] childRenderers = wheel.GetComponentsInChildren<Renderer>(true);

            if (childRenderers.Length > 0)
            {
                wheelRenderer = childRenderers[0];
            }
        }

        if (wheelRenderer == null)
        {
            return Vector3.right;
        }

        Vector3 size = wheelRenderer.localBounds.size;

        if (size.x <= size.y && size.x <= size.z)
        {
            return Vector3.right;
        }

        if (size.y <= size.x && size.y <= size.z)
        {
            return Vector3.up;
        }

        return Vector3.forward;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.mass = 1450f;
        body.drag = 0.08f;
        body.angularDrag = 4.5f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.centerOfMass = centerOfMass;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Update()
    {
        throttle = (Input.GetKey(forwardKey) ? 1f : 0f) - (Input.GetKey(reverseKey) ? 1f : 0f);
        steer = (Input.GetKey(rightKey) ? 1f : 0f) - (Input.GetKey(leftKey) ? 1f : 0f);
        handbraking = Input.GetKey(handbrakeKey);

        if (Input.GetKeyDown(resetKey))
        {
            ResetVehicle();
        }

        SpinVisualWheels();
    }

    private void FixedUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * 0.6f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        ApplyLateralFriction();

        if (!isGrounded)
        {
            return;
        }

        float fwdSpeed = ForwardSpeed;

        if (throttle > 0.01f)
        {
            if (fwdSpeed < maxSpeed)
            {
                body.AddForce(transform.forward * accelerationForce * throttle, ForceMode.Force);
            }
        }
        else if (throttle < -0.01f)
        {
            if (fwdSpeed > 0.2f)
            {
                body.AddForce(transform.forward * brakeForce * throttle, ForceMode.Force);
            }
            else if (-fwdSpeed < maxReverseSpeed)
            {
                body.AddForce(transform.forward * reverseForce * throttle, ForceMode.Force);
            }
        }
        else
        {
            body.AddForce(-transform.forward * fwdSpeed * rollingResistance, ForceMode.Force);
        }

        float steerStrength = Mathf.Clamp01(Mathf.Abs(fwdSpeed) / 5.5f);
        steerStrength = Mathf.Lerp(0.18f, 1f, steerStrength);
        float reverseSteer = fwdSpeed < -0.2f ? -1f : 1f;
        body.AddTorque(transform.up * steerTorque * steer * steerStrength * reverseSteer, ForceMode.Force);

        if (handbraking)
        {
            body.AddForce(-transform.forward * fwdSpeed * 4.2f, ForceMode.Acceleration);
        }
    }

    private void ApplyLateralFriction()
    {
        Vector3 right = transform.right;
        float sideVel = Vector3.Dot(body.velocity, right);
        float frictionScale = handbraking ? 0.35f : 1f;
        body.AddForce(-right * sideVel * lateralFriction * frictionScale, ForceMode.Acceleration);
    }

    private void SpinVisualWheels()
    {
        if (visualWheels == null || visualWheels.Length == 0)
        {
            return;
        }

        EnsureWheelSpinAxes();

        float angularVel = ForwardSpeed / Mathf.Max(0.05f, wheelRadius);
        float deltaDeg = angularVel * Mathf.Rad2Deg * Time.deltaTime;

        for (int i = 0; i < visualWheels.Length; i++)
        {
            Transform wheel = visualWheels[i];

            if (wheel == null)
            {
                continue;
            }

            Vector3 axis = wheelSpinAxes != null && i < wheelSpinAxes.Length ? wheelSpinAxes[i] : Vector3.right;
            wheel.Rotate(axis, deltaDeg, Space.Self);
        }
    }

    public void ResetVehicle()
    {
        if (reloadSceneOnReset)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            return;
        }

        Vector3 pos = respawnPoint != null ? respawnPoint.position : spawnPosition;
        Quaternion rot = respawnPoint != null ? respawnPoint.rotation : spawnRotation;
        transform.SetPositionAndRotation(pos, rot);
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }
}
