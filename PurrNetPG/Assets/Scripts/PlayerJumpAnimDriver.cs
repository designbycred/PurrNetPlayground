using UnityEngine;

public class PlayerJumpAnimDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator anim;     // HumanM_Model animator
    [SerializeField] private Rigidbody rb;      // Player root rigidbody
    [SerializeField] private PlayerJump jump;   // PlayerJump on Player root

    [Header("Params")]
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string yVelParam = "YVel";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string landTrigger = "Land";   // NEW

    [Header("Tuning")]
    [SerializeField] private float takeoffYVel = 0.5f;
    [SerializeField] private float landMinDownVel = -0.2f;  // must be falling to land-trigger
    [SerializeField] private float yVelSmooth = 8f;

    private float yVelSmoothed;
    private bool lastGrounded;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!rb) rb = GetComponentInParent<Rigidbody>();
        if (!jump) jump = GetComponentInParent<PlayerJump>();

        lastGrounded = true;
    }

    private void LateUpdate()
    {
        if (!anim || !rb || !jump) return;

        bool grounded = jump.VisualGrounded;
        float yVel = rb.linearVelocity.y;

        if (yVelSmooth > 0f)
        {
            float t = 1f - Mathf.Exp(-yVelSmooth * Time.deltaTime);
            yVelSmoothed = Mathf.Lerp(yVelSmoothed, yVel, t);
            yVel = yVelSmoothed;
        }

        // takeoff
        if (lastGrounded && !grounded && yVel > takeoffYVel)
            anim.SetTrigger(jumpTrigger);

        // touchdown (fires land immediately)
        if (!lastGrounded && grounded && yVel < landMinDownVel)
            anim.SetTrigger(landTrigger);

        anim.SetBool(groundedParam, grounded);
        anim.SetFloat(yVelParam, yVel);

        lastGrounded = grounded;
    }
}
