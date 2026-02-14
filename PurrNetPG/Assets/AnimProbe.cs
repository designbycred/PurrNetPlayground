using UnityEngine;

public class AnimProbe : MonoBehaviour
{
    Animator a;
    void Awake() => a = GetComponent<Animator>();
    void Update()
    {
        a.SetFloat("MoveX", 0f);
        a.SetFloat("MoveY", 1f);
    }
}
