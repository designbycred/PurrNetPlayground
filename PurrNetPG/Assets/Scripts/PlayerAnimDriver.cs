using UnityEngine;

public class PlayerAnimDriver : MonoBehaviour
{
    [Header("Refs (auto-found if empty)")]
    [SerializeField] private Animator anim;            // on HumanM_Model
    [SerializeField] private Transform motionRoot;     // Player root (the thing that actually moves)

    [Header("Animator Params")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";

    [Header("Tuning")]
    [SerializeField] private float maxSpeed = 6f;      // match moveSpeed
    [SerializeField] private float smooth = 12f;
    [SerializeField] private float moveDeadzone = 0.05f;

    [Tooltip("How long to keep last direction after movement stops (helps when extrapolate is OFF and updates are tick-based).")]
    [SerializeField] private float holdTime = 0.12f;

    private Vector3 lastPos;
    private float sx, sy;
    private float holdTimer;
    private Vector2 lastDir;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!motionRoot) motionRoot = transform.root;

        if (!anim) Debug.LogError("[AnimDriver] Missing Animator. Put this on HumanM_Model.", this);
        if (anim && anim.runtimeAnimatorController == null)
            Debug.LogError("[AnimDriver] Animator has NO controller assigned!", this);

        lastPos = motionRoot.position;
    }

    private void LateUpdate()
    {
        if (!anim || !motionRoot) return;

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
            // hold last direction briefly to cover tick gaps
            holdTimer -= Time.deltaTime;
            dir = (holdTimer > 0f) ? lastDir : Vector2.zero;
        }

        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        sx = Mathf.Lerp(sx, dir.x, t);
        sy = Mathf.Lerp(sy, dir.y, t);

        anim.SetFloat(moveXParam, sx);
        anim.SetFloat(moveYParam, sy);
    }
}
