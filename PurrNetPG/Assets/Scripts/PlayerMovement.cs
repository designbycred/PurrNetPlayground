using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : PredictedIdentity<PlayerMovement.Input, PlayerMovement.State>
{
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private float _moveForce = 20f; // higher for testing

    private Vector2 _moveInput;

    // PlayerInput (Invoke Unity Events) -> Move
    public void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
        // This only proves input is received by THIS instance
        // Debug.Log($"MOVE: {_moveInput}");
    }

    // This is called on the controlling instance to fill the input that gets predicted/sent
    protected override void GetFinalInput(ref Input input)
    {
        input.direction = _moveInput;

        // Ownership/debug (PurrNet uses lowercase fields)
        if (isOwner && input.direction != Vector2.zero)
            Debug.Log($"OWNER INPUT: {input.direction}  owner={owner} isOwner={isOwner} isController={isController}");
    }

    // This is the predicted simulation step
    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (input.direction != Vector2.zero)
            Debug.Log($"SIMULATE: {input.direction}  owner={owner} isOwner={isOwner} isController={isController}");

        Vector3 local = new Vector3(input.direction.x, 0f, input.direction.y);
        Vector3 world = transform.TransformDirection(local);
        world.y = 0f;

        // ForceMode.Acceleration style feel (mass independent) – PredictedRigidbody usually handles mass,
        // so we just scale by delta here.
        _rigidbody.AddForce(world.normalized * _moveForce * delta);
    }

    // Optional: periodic status log so you can see which instance owns what
    private void Update()
    {
        if (Time.frameCount % 120 == 0)
            Debug.Log($"STATUS: owner={owner} isOwner={isOwner} isController={isController}");
    }

    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public Vector2 direction;
        public void Dispose() { }
    }
}
