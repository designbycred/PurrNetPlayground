using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : PredictedIdentity<PlayerMovement.Input, PlayerMovement.State>
{
    [Header("Refs")]
    [SerializeField] private PredictedRigidbody predictedRb;
    [SerializeField] private PlayerInput playerInput;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float accel = 25f;
    [SerializeField] private float brake = 40f;
    [SerializeField] private float stopEpsilon = 0.05f;
    [SerializeField] private float inputDeadzone = 0.05f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool lastController;

    // Exposed for PlayerAnimDriver (works for owner + proxies)
    public Vector2 LastSimulatedMove { get; private set; }

    private void Awake()
    {
        if (!predictedRb) predictedRb = GetComponentInChildren<PredictedRigidbody>(true);
        if (!playerInput) playerInput = GetComponentInChildren<PlayerInput>(true);

        rb = predictedRb ? predictedRb.GetComponent<Rigidbody>() : GetComponentInChildren<Rigidbody>(true);

        if (!predictedRb) Debug.LogError("[PM] Missing PredictedRigidbody.", this);
        if (!rb) Debug.LogError("[PM] Missing Rigidbody.", this);
        if (!playerInput) Debug.LogError("[PM] Missing PlayerInput.", this);
    }

    // PlayerInput (Invoke Unity Events) -> Move
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!isController) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

    protected override void GetFinalInput(ref Input input)
    {
        Vector2 d = isController ? moveInput : Vector2.zero;

        float dz2 = inputDeadzone * inputDeadzone;
        if (d.sqrMagnitude < dz2) d = Vector2.zero;

        if (d.sqrMagnitude > 1f) d.Normalize();

        input.direction = d;
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (!rb) return;

        // cache the input that actually drove this tick (owner + proxies)
        LastSimulatedMove = input.direction;

        Vector3 v = rb.linearVelocity;
        Vector3 horiz = new Vector3(v.x, 0f, v.z);

        bool hasInput = input.direction.sqrMagnitude > 0.0001f;

        if (hasInput)
        {
            Vector3 local = new Vector3(input.direction.x, 0f, input.direction.y);
            Vector3 wish = transform.TransformDirection(local);
            wish.y = 0f;

            Vector3 targetHoriz = wish.normalized * moveSpeed;
            horiz = Vector3.MoveTowards(horiz, targetHoriz, accel * delta);
        }
        else
        {
            horiz = Vector3.MoveTowards(horiz, Vector3.zero, brake * delta);

            float eps2 = stopEpsilon * stopEpsilon;
            if (horiz.sqrMagnitude < eps2)
                horiz = Vector3.zero;
        }

        rb.linearVelocity = new Vector3(horiz.x, v.y, horiz.z);
    }

    private void LateUpdate()
    {
        // Only controller should have PlayerInput enabled (important for clones)
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
        public void Dispose() { }
    }
}
