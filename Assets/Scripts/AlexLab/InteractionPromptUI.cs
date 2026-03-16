using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance;

    public GameObject promptObject;
    public TMP_Text promptText;

    void Awake()
    {
        Instance = this;
        promptObject.SetActive(false);
    }

    public void ShowPrompt(string text)
    {
        promptObject.SetActive(true);
        promptText.text = text;
    }

    public void HidePrompt()
    {
        promptObject.SetActive(false);
    }
}