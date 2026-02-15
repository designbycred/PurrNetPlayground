using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMotor : PredictedIdentity<PlayerMotor.Input, PlayerMotor.State>
{
    [Header("Refs")]
    [SerializeField] private PredictedRigidbody predictedRb;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMove move;
    [SerializeField] private PlayerJump jump;

    [Header("Input")]
    [SerializeField] private float inputDeadzone = 0.05f;

    [Header("Anim Feed")]
    [Tooltip("Used to normalize planar velocity into [-1..1] for MoveX/MoveY.")]

    // Exposed for anim drivers (works for owner + proxies)
    public Vector2 SimMoveLocal01 { get; private set; }   // (x=strafe, y=forward) normalized [-1..1]
    public bool SimGrounded { get; private set; }
    public float SimYVel { get; private set; }
    public int SimJumpSeq { get; private set; }          // increments on jump start

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool lastController;

    private void Awake()
    {
        if (!predictedRb) predictedRb = GetComponentInChildren<PredictedRigidbody>(true);
        if (!playerInput) playerInput = GetComponentInChildren<PlayerInput>(true);
        if (!move) move = GetComponentInChildren<PlayerMove>(true);
        if (!jump) jump = GetComponentInChildren<PlayerJump>(true);

        rb = predictedRb ? predictedRb.GetComponent<Rigidbody>() : GetComponentInChildren<Rigidbody>(true);

        if (!rb) Debug.LogError("[Motor] Missing Rigidbody.", this);
        if (!move) Debug.LogError("[Motor] Missing PlayerMove.", this);
        if (!jump) Debug.LogWarning("[Motor] Missing PlayerJump -> jump will do nothing.", this);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!isController) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!isController) return;

        if (ctx.performed)
        {
            jumpPressed = true;
            // Debug.Log($"[Motor] Jump pressed on {name}", this);
        }
    }

    protected override void GetFinalInput(ref Input input)
    {
        Vector2 d = isController ? moveInput : Vector2.zero;

        float dz2 = inputDeadzone * inputDeadzone;
        if (d.sqrMagnitude < dz2) d = Vector2.zero;
        if (d.sqrMagnitude > 1f) d.Normalize();

        input.direction = d;
        input.jumpPressed = isController && jumpPressed;

        // consume one-tick press
        jumpPressed = false;
    }

    protected override void Simulate(Input input, ref State state, float dt)
    {
        if (!rb) return;

        Vector3 v = rb.linearVelocity;

        if (move != null)
            v = move.SimulatePlanar(input.direction, v, transform, dt);

        bool jumpStartedThisTick = false;
        if (jump != null)
            jumpStartedThisTick = jump.Simulate(ref v, input.jumpPressed, dt);

        rb.linearVelocity = v;

        // Expose predicted values for anim (owner + proxies)
        SimGrounded = (jump != null) && jump.IsGrounded;
        SimYVel = v.y;

        if (jumpStartedThisTick)
            SimJumpSeq++;
    }


    private void LateUpdate()
    {
        if (playerInput && lastController != isController)
        {
            lastController = isController;
            playerInput.enabled = isController;
        }
    }

    public struct State : IPredictedData<State> { public void Dispose() { } }
    public struct Input : IPredictedData
    {
        public Vector2 direction;
        public bool jumpPressed;
        public void Dispose() { }
    }
}
