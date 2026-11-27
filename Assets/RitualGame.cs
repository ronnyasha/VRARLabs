using UnityEngine;

// Ігрова логіка "Atelier in the Chapel":
// гравець приносить на вівтар котушку, голку й тканину (3 Snap Zone).
// Коли всі три на місці - сорочка добудована, світло яскравішає, фігури повертаються до гравця.
public class RitualGame : MonoBehaviour
{
    public SnapZone[] slots;
    public TextMesh label;
    public GameObject seam;
    public Light altarLight;
    public FigureTurn[] figures;

    bool done;
    int last = -1;

    void Start()
    {
        if (seam != null) seam.SetActive(false);
        Debug.Log("Atelier in the Chapel: збери котушку, голку й тканину та поклади їх на вівтар");
    }

    void Update()
    {
        int n = 0;
        foreach (var s in slots) if (s != null && s.Occupied) n++;

        if (n != last)
        {
            last = n;
            Refresh(n);
            Debug.Log("Ритуал: " + n + " / " + slots.Length);
        }

        if (!done && n == slots.Length) Complete();
    }

    void Refresh(int n)
    {
        if (label == null) return;
        label.text = done
            ? "THE SHIRT IS FINISHED\nPERCEPTION DISTORTED"
            : "COLLECT  SPOOL  -  NEEDLE  -  FABRIC\n" + n + " / " + slots.Length;
    }

    void Complete()
    {
        done = true;
        if (seam != null) seam.SetActive(true);
        if (altarLight != null) altarLight.intensity *= 1.8f;
        foreach (var f in figures) if (f != null) f.Activate();
        Refresh(slots.Length);
        Debug.Log("Ритуал завершено: сорочку зшито, фігури повертаються до гравця");
    }
}
