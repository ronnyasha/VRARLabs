using UnityEngine;

public class GazeTeleport : MonoBehaviour
{
    public OVRCameraRig rig;
    public float dwellTime = 1.5f;
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
        bool press = OVRInput.GetDown(OVRInput.Button.One)
                  || Input.GetKeyDown(KeyCode.T);
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
        Vector3 offset = rig.centerEyeAnchor.position - rig.transform.position;
        // висота камери береться з точки телепортації (щоб можна було піднятися на платформу вівтаря)
        rig.transform.position = new Vector3(target.x - offset.x,
            target.y - 0.02f, target.z - offset.z);
        Debug.Log("Телепорт у точку " + target);
    }
}
