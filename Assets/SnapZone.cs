using UnityEngine;
using Oculus.Interaction;

// Snap Zone: місце на вівтарі, куди предмет "вставляється",
// коли гравець відпускає його поруч із зоною.
[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    public string requiredName = "";     // якщо задано - приймає тільки предмет з такою назвою
    public float pullRadius = 0.3f;      // відстань, з якої відпущений предмет "притягується" до зони
    public Transform snapPoint;          // куди ставити предмет (якщо порожньо - центр зони)

    private Grabbable current;           // предмет, який зараз стоїть у зоні
    private Rigidbody currentRb;
    private Grabbable wanted;            // предмет, який ця зона чекає (за requiredName)

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (requiredName != "")
        {
            GameObject g = GameObject.Find(requiredName);
            if (g != null) wanted = g.GetComponent<Grabbable>();
        }
    }

    public bool Occupied { get { return current != null; } }

    Transform Point()
    {
        return snapPoint != null ? snapPoint : transform;
    }

    void Take(Grabbable grabbable)
    {
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

    void OnTriggerStay(Collider other)
    {
        if (current != null) return;

        Grabbable grabbable = other.GetComponentInParent<Grabbable>();
        if (grabbable == null) return;
        if (requiredName != "" && grabbable.name != requiredName) return;   // не той предмет
        if (grabbable.SelectingPointsCount > 0) return;   // ще в руці гравця

        Take(grabbable);
    }

    void Update()
    {
        if (current == null)
        {
            // страховка: якщо відпущений предмет лежить поруч із зоною - беремо його теж
            if (wanted != null && wanted.SelectingPointsCount == 0 &&
                Vector3.Distance(wanted.transform.position, Point().position) < pullRadius)
                Take(wanted);
            return;
        }

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
