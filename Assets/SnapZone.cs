using UnityEngine;
using Oculus.Interaction;

// Snap Zone: місце на вівтарі, куди предмет "вставляється",
// коли гравець відпускає його поруч із зоною.
[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    public Transform snapPoint;          // куди ставити предмет (якщо порожньо - центр зони)

    private Grabbable current;           // предмет, який зараз стоїть у зоні
    private Rigidbody currentRb;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    Transform Point()
    {
        return snapPoint != null ? snapPoint : transform;
    }

    void OnTriggerStay(Collider other)
    {
        if (current != null) return;

        Grabbable grabbable = other.GetComponentInParent<Grabbable>();
        if (grabbable == null) return;
        if (grabbable.SelectingPointsCount > 0) return;   // ще в руці гравця

        current = grabbable;
        currentRb = grabbable.GetComponent<Rigidbody>();
        if (currentRb != null)
        {
            currentRb.linearVelocity = Vector3.zero;
            currentRb.angularVelocity = Vector3.zero;
            currentRb.useGravity = false;
        }
        Debug.Log("Предмет " + current.name + " став у Snap Zone " + name);
    }

    void Update()
    {
        if (current == null) return;

        if (current.SelectingPointsCount > 0)
        {
            // гравець знову взяв предмет - відпускаємо його з зони
            if (currentRb != null) currentRb.useGravity = true;
            Debug.Log("Предмет " + current.name + " забрали з Snap Zone " + name);
            current = null;
            currentRb = null;
            return;
        }

        current.transform.position = Point().position;
        current.transform.rotation = Point().rotation;
    }
}
