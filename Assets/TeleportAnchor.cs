using UnityEngine;

// Точка телепортації (Teleportation Anchor): ставиться на підлогу.
// Сам по собі нічого не робить - на неї дивиться GazeTeleport.
[RequireComponent(typeof(Collider))]
public class TeleportAnchor : MonoBehaviour
{
    public Color idleColor = new Color(0.4f, 0.8f, 1f);
    public Color aimedColor = Color.white;

    Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        SetAimed(false);
    }

    public void SetAimed(bool aimed)
    {
        if (rend != null) rend.material.color = aimed ? aimedColor : idleColor;
    }
}
