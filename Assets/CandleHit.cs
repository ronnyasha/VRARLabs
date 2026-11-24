using UnityEngine;

public class CandleHit : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 1f)
            Debug.Log(name + " вдарилась об " + collision.gameObject.name);
    }
}