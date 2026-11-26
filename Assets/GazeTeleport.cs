using UnityEngine;

// Телепорт: дивишся на TeleportAnchor на підлозі і
// або тримаєш погляд ~1.5 с, або тиснеш кнопку A контролера (OVRInput.Button.One)
// - Camera Rig переноситься на цю точку.
public class GazeTeleport : MonoBehaviour
{
    public OVRCameraRig rig;          // сюди перетягнути OVRCameraRig
    public float dwellTime = 1.5f;    // скільки секунд дивитися
    public float maxDistance = 15f;

    TeleportAnchor aimed;
    float timer;

    void Update()
    {
        if (rig == null) return;
        Transform eye = rig.centerEyeAnchor;

        TeleportAnchor hitAnchor = null;
        RaycastHit hit;
        if (Physics.Raycast(eye.position, eye.forward, out hit, maxDistance,
                            ~0, QueryTriggerInteraction.Collide))
            hitAnchor = hit.collider.GetComponentInParent<TeleportAnchor>();

        if (hitAnchor != aimed)
        {
            if (aimed != null) aimed.SetAimed(false);
            aimed = hitAnchor;
            timer = 0f;
            if (aimed != null) aimed.SetAimed(true);
        }

        if (aimed == null) return;

        timer += Time.deltaTime;
        bool press = OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.T);
        if (timer >= dwellTime || press)
        {
            Teleport(aimed.transform.position);
            aimed.SetAimed(false);
            aimed = null;
            timer = 0f;
        }
    }

    void Teleport(Vector3 target)
    {
        // переносимо так, щоб голова опинилась над точкою (висоту не змінюємо)
        Vector3 offset = rig.centerEyeAnchor.position - rig.transform.position;
        Vector3 pos = new Vector3(target.x - offset.x, rig.transform.position.y, target.z - offset.z);
        rig.transform.position = pos;
        Debug.Log("Телепорт у точку " + target);
    }
}
