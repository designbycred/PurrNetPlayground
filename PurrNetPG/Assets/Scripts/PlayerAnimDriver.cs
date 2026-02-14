using UnityEngine;

public class PlayerAnimDriver : MonoBehaviour
{
    [Header("Refs (auto-found if empty)")]
    [SerializeField] private Animator anim;            // put this on HumanM_Model
    [SerializeField] private Transform motionRoot;     // Player root (the thing that moves in world)

    [Header("Animator Params")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";

    [Header("Tuning (base)")]
    [SerializeField] private float maxSpeed = 6f;      // match movement moveSpeed
    [SerializeField] private float smooth = 12f;       // higher = snappier
    [SerializeField] private float moveDeadzone = 0.05f;
    [SerializeField] private float holdTime = 0.12f;

    [Header("Anti-snap (recommended ON)")]
    [Tooltip("Limits how fast MoveX/MoveY can change (good for proxies / network ticky motion). 0 = off.")]
    [SerializeField] private float maxParamChangePerSec = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private int debugEveryNFrames = 120;

    private Vector3 lastPos;
    private float sx, sy;
    private float holdTimer;
    private Vector2 lastDir;
    private bool inited;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!motionRoot) motionRoot = FindMotionRoot();

        if (!anim) Debug.LogError("[AnimDriver] Missing Animator. Put this on HumanM_Model.", this);
        if (anim && anim.runtimeAnimatorController == null)
            Debug.LogError("[AnimDriver] Animator has NO controller assigned!", this);

        if (!motionRoot)
            Debug.LogError("[AnimDriver] Missing MotionRoot. Drag Player root into MotionRoot.", this);

        InitLastPos();
    }

    private Transform FindMotionRoot()
    {
        // Best-effort: go UP and find something that usually lives on the Player root.
        // If you have Rigidbody on Player, this will grab it.
        var rb = GetComponentInParent<Rigidbody>();
        if (rb) return rb.transform;

        // Fallback: root (works in simple prefabs)
        return transform.root;
    }

    private void OnEnable()
    {
        // When objects spawn / enable, make sure we don't get a huge delta on first frame.
        InitLastPos();
    }

    private void InitLastPos()
    {
        if (motionRoot)
        {
            lastPos = motionRoot.position;
            inited = true;
        }
    }

    private void LateUpdate()
    {
        if (!anim || !motionRoot) return;

        // If motionRoot just became valid or we were disabled, avoid 1-frame pop.
        if (!inited)
            InitLastPos();

        Vector3 pos = motionRoot.position;
        Vector3 delta = pos - lastPos;
        lastPos = pos;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 worldVel = delta / dt;
        worldVel.y = 0f;

        Vector3 localVel = motionRoot.InverseTransformDirection(worldVel);

        float speed = new Vector2(localVel.x, localVel.z).magnitude;

        Vector2 dir;
        if (speed > moveDeadzone)
        {
            dir = new Vector2(localVel.x, localVel.z) / Mathf.Max(maxSpeed, 0.001f);
            dir.x = Mathf.Clamp(dir.x, -1f, 1f);
            dir.y = Mathf.Clamp(dir.y, -1f, 1f);

            lastDir = dir;
            holdTimer = holdTime;
        }
        else
        {
            holdTimer -= Time.deltaTime;
            dir = (holdTimer > 0f) ? lastDir : Vector2.zero;
        }

        // Base smoothing
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        float tx = Mathf.Lerp(sx, dir.x, t);
        float ty = Mathf.Lerp(sy, dir.y, t);

        // Anti-snap rate limiter (helps proxies a TON)
        if (maxParamChangePerSec > 0f)
        {
            float maxStep = maxParamChangePerSec * Time.deltaTime;
            tx = Mathf.MoveTowards(sx, tx, maxStep);
            ty = Mathf.MoveTowards(sy, ty, maxStep);
        }

        sx = tx;
        sy = ty;

        anim.SetFloat(moveXParam, sx);
        anim.SetFloat(moveYParam, sy);

        if (debugLogs && Time.frameCount % debugEveryNFrames == 0)
        {
            Debug.Log($"[AnimDriver] root={motionRoot.name} pos={pos} vLocal=({localVel.x:F2},{localVel.z:F2}) " +
                      $"dir=({dir.x:F2},{dir.y:F2}) -> ({sx:F2},{sy:F2})", this);
        }
    }
}
