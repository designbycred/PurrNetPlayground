using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : PredictedIdentity<PlayerMovement.Input, PlayerMovement.State>
{
    [Header("Refs")]
    [SerializeField] private PredictedRigidbody _predictedRb;
    [SerializeField] private PlayerInput _playerInput; // assign PlayerInput on the prefab (recommended)

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float accel = 25f;

    private Rigidbody _rb;
    private Vector2 _moveInput;

    void Awake()
    {
        if (_predictedRb == null)
            _predictedRb = GetComponentInChildren<PredictedRigidbody>();

        if (_playerInput == null)
            _playerInput = GetComponentInChildren<PlayerInput>(true);

        // PredictedRigidbody sits on (or references) a Rigidbody. Grab it.
        _rb = _predictedRb != null
            ? _predictedRb.GetComponent<Rigidbody>()       // if PredictedRigidbody is on same GO as RB
            : GetComponentInChildren<Rigidbody>();         // fallback

        if (_rb == null)
            Debug.LogError("No Rigidbody found. Assign PredictedRigidbody or add a Rigidbody.");
    }

    void Start()
    {
        // IMPORTANT: only the controlling instance should have local input enabled
        if (_playerInput != null)
            _playerInput.enabled = isController;
    }

    // Called by PlayerInput (Invoke Unity Events)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        // extra safety: ignore input if this isn't the controller
        if (!isController) return;
        _moveInput = ctx.ReadValue<Vector2>();
    }

    protected override void GetFinalInput(ref Input input)
    {
        // only controller provides input, everyone else sends zero
        input.direction = isController ? _moveInput : Vector2.zero;
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (_rb == null) return;

        Vector3 local = new Vector3(input.direction.x, 0f, input.direction.y);
        Vector3 wish = transform.TransformDirection(local);
        wish.y = 0f;

        Vector3 v = _rb.linearVelocity;
        Vector3 horiz = new Vector3(v.x, 0f, v.z);

        Vector3 targetHoriz = wish.sqrMagnitude > 0.0001f ? wish.normalized * moveSpeed : Vector3.zero;
        Vector3 newHoriz = Vector3.MoveTowards(horiz, targetHoriz, accel * delta);

        _rb.linearVelocity = new Vector3(newHoriz.x, v.y, newHoriz.z);
    }

    public struct State : IPredictedData<State> { public void Dispose() { } }
    public struct Input : IPredictedData { public Vector2 direction; public void Dispose() { } }
}
