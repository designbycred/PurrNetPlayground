using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float accel = 25f;
    [SerializeField] private float brake = 40f;
    [SerializeField] private float stopEpsilon = 0.05f;

    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// Computes new planar (x/z) velocity from input.
    /// Input is local: x=strafe, y=forward.
    /// </summary>
    public Vector3 SimulatePlanar(Vector2 inputDir, Vector3 currentVelocity, Transform basis, float dt)
    {
        Vector3 horiz = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        bool hasInput = inputDir.sqrMagnitude > 0.0001f;

        if (hasInput)
        {
            Vector3 local = new Vector3(inputDir.x, 0f, inputDir.y);
            Vector3 wish = basis.TransformDirection(local);
            wish.y = 0f;

            Vector3 target = wish.normalized * moveSpeed;
            horiz = Vector3.MoveTowards(horiz, target, accel * dt);
        }
        else
        {
            horiz = Vector3.MoveTowards(horiz, Vector3.zero, brake * dt);

            float eps2 = stopEpsilon * stopEpsilon;
            if (horiz.sqrMagnitude < eps2)
                horiz = Vector3.zero;
        }

        return new Vector3(horiz.x, currentVelocity.y, horiz.z);
    }
}
