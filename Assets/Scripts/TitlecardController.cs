using System.Reflection;
using UnityEngine;

public class TitlecardController : MonoBehaviour


{
    
    public Animator BlackTitleAnim; // Reference the Animator from Blacktitle canvas child object

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            BlackTitleAnim.SetBool("startCredits", false);
    }

    // Update is called once per frame
    void Update()
    {
         //For testing animation, press T to start credits animation

        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCredits();
            Debug.Log("T key pressed - Starting Credits Animation");
        }
    }

    public void StartCredits()
    {
        BlackTitleAnim.SetBool("startCredits", true);
    }

    
}
