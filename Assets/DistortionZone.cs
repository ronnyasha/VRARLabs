using UnityEngine;
using UnityEngine.Rendering;

public class DistortionZone : MonoBehaviour
{
    public Volume volume;

    void Start() { volume.weight = 0; }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            volume.weight = 1;
            Debug.Log("Гравець зайшов у зону спотворення");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) volume.weight = 0;
    }
}