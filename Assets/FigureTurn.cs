using UnityEngine;

// Фігура в балахоні: після завершення ритуалу повільно повертається до гравця.
public class FigureTurn : MonoBehaviour
{
    public float speed = 18f;       // градусів за секунду
    Transform target;
    bool active;

    public void Activate()
    {
        active = true;
        if (Camera.main != null) target = Camera.main.transform;
    }

    void Update()
    {
        if (!active) return;
        if (target == null)
        {
            if (Camera.main != null) target = Camera.main.transform;
            return;
        }
        Vector3 d = target.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        Quaternion want = Quaternion.LookRotation(d, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, speed * Time.deltaTime);
    }
}
