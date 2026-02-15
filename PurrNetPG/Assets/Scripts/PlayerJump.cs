using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    public enum GravityMode { Normal, ZeroG }

    [Header("Mode")]
    [SerializeField] private GravityMode mode = GravityMode.Normal;

    [Header("Normal Gravity Jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStick = -2f;

    [Header("Ground Check")]
    [Tooltip("Set this to your Ground layer(s). Do NOT include Player.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Range(0.5f, 1f)]
    [SerializeField] private float feetRadiusFactor = 0.95f;

    [Header("Cast Distances")]
    [Tooltip("Strict grounded used for simulation (smaller = fewer false grounded).")]
    [SerializeField] private float simCastDistance = 0.20f;

    [Tooltip("More generous grounded used for visuals/anim (bigger = less 'hover before land').")]
    [SerializeField] private float visualCastDistance = 0.45f;

    [Header("ZeroG Jump (later)")]
    [SerializeField] private float zeroGPush = 3f;
    [SerializeField] private bool zeroGPushForward = true;

    [Header("Debug")]
    [SerializeField] private bool debugGround = false;

    public bool IsGrounded { get; private set; }       // predicted tick
    public bool VisualGrounded { get; private set; }   // per-frame

    public GravityMode Mode => mode;
    public void SetMode(GravityMode newMode) => mode = newMode;

    private Collider col;
    private Collider[] selfColliders;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    private void Awake()
    {
        col = GetComponent<Collider>() ?? GetComponentInParent<Collider>();
        selfColliders = GetComponentsInChildren<Collider>(true);

        if (!col) Debug.LogError("[PlayerJump] Missing Collider for ground check.", this);
    }

    private void LateUpdate()
    {
        // Visual-only grounded (more generous)
        VisualGrounded = CheckGrounded(visualCastDistance);
    }

    public bool Simulate(ref Vector3 velocity, bool jumpPressedThisTick, float dt)
    {
        // Simulation grounded (stricter)
        IsGrounded = CheckGrounded(simCastDistance);
        bool jumpStarted = false;

        if (mode == GravityMode.Normal)
        {
            if (IsGrounded && velocity.y < 0f)
                velocity.y = groundedStick;

            if (jumpPressedThisTick && IsGrounded)
            {
                velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
                jumpStarted = true;
            }

            velocity.y += gravity * dt;
        }
        else
        {
            if (jumpPressedThisTick)
            {
                Vector3 dir = zeroGPushForward ? transform.forward : transform.up;
                velocity += dir.normalized * zeroGPush;
                jumpStarted = true;
            }
        }

        return jumpStarted;
    }

    private bool CheckGrounded(float castDistance)
    {
        if (!col) return false;

        // Works great with CapsuleCollider
        if (col is CapsuleCollider cap)
        {
            Transform t = cap.transform;

            // Convert capsule data to world space
            float radius = Mathf.Max(0.05f, cap.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z)) * feetRadiusFactor;

            // World-space capsule endpoints
            Vector3 center = t.TransformPoint(cap.center);

            // Direction: 0=X,1=Y,2=Z
            Vector3 up = (cap.direction == 0) ? t.right : (cap.direction == 2 ? t.forward : t.up);

            float height = Mathf.Max(cap.height * t.lossyScale.y, radius * 2f);
            float half = Mathf.Max(0f, (height * 0.5f) - radius);

            Vector3 p1 = center + up * half;
            Vector3 p2 = center - up * half;

            int count = Physics.CapsuleCastNonAlloc(
                p1, p2,
                radius,
                -up,                    // cast "down" along capsule up axis
                hitBuffer,
                castDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                var h = hitBuffer[i].collider;
                if (!h) continue;

                // ignore self
                bool isSelf = false;
                for (int j = 0; j < selfColliders.Length; j++)
                    if (h == selfColliders[j]) { isSelf = true; break; }
                if (isSelf) continue;

                if (debugGround) Debug.Log($"[PlayerJump] Grounded hit: {h.name}", this);
                return true;
            }

            return false;
        }

        // Fallback: old bounds-based spherecast for non-capsule colliders
        Bounds b = col.bounds;
        float r = Mathf.Min(b.extents.x, b.extents.z) * feetRadiusFactor;
        r = Mathf.Max(r, 0.05f);

        Vector3 origin = new Vector3(b.center.x, b.min.y + 0.10f, b.center.z);
        int c2 = Physics.SphereCastNonAlloc(origin, r, Vector3.down, hitBuffer, castDistance, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < c2; i++)
        {
            var h = hitBuffer[i].collider;
            if (!h) continue;

            bool isSelf = false;
            for (int j = 0; j < selfColliders.Length; j++)
                if (h == selfColliders[j]) { isSelf = true; break; }
            if (isSelf) continue;

            return true;
        }

        return false;
    }

}
