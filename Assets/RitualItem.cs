using UnityEngine;
using Oculus.Interaction;

// Предмет ритуалу (котушка, голка, тканина).
// Якщо предмет упав під підлогу або лежить на підлозі/платформі, а не в руці -
// він повертається на свою тумбу.
public class RitualItem : MonoBehaviour
{
    Vector3 startPos;
    Quaternion startRot;
    Rigidbody rb;
    Grabbable grabbable;
    float lowTime;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
    }

    void Update()
    {
        bool held = grabbable != null && grabbable.SelectingPointsCount > 0;
        bool snapped = rb != null && !rb.useGravity;

        if (!held && !snapped && transform.position.y < 0.9f) lowTime += Time.deltaTime;
        else lowTime = 0f;

        if (transform.position.y < -0.5f || lowTime > 2f)
        {
            lowTime = 0f;
            transform.SetPositionAndRotation(startPos, startRot);
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Debug.Log("Предмет " + name + " впав і повернувся на свою тумбу");
        }
    }
}
