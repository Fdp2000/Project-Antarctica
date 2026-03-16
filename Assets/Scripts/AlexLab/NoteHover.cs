using UnityEngine;

public class NoteHover : MonoBehaviour
{
    public GameObject glowEffect;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                glowEffect.SetActive(true);
            }
            else
            {
                glowEffect.SetActive(false);
            }
        }
        else
        {
            glowEffect.SetActive(false);
        }
    }
}