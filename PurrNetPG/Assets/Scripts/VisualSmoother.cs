using UnityEngine;

public class VisualSmoother : MonoBehaviour
{
    [SerializeField] private Transform targetRoot; // drag Player root
    [SerializeField] private float posSmooth = 25f;
    [SerializeField] private float rotSmooth = 25f;

    void Awake()
    {
        if (!targetRoot) targetRoot = transform.parent;
    }

    void LateUpdate()
    {
        if (!targetRoot) return;

        float pt = 1f - Mathf.Exp(-posSmooth * Time.deltaTime);
        float rt = 1f - Mathf.Exp(-rotSmooth * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetRoot.position, pt);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRoot.rotation, rt);
    }
}
