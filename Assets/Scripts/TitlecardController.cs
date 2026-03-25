using System.Reflection;
using UnityEngine;

public class TitlecardController : MonoBehaviour


{
    
    public Animator BlackTitleAnim; // Reference the Animator from Blacktitle canvas child object


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            titleAnim.SetBool("startCredits", false);
    }

    // Update is called once per frame
    void Update()
    {
        // For testing animation, press T to start credits animation
        
        //if (Input.GetKeyDown(KeyCode.T))
        //{
            //StartCredits();
        //}
    }

    public void StartCredits()
    {
        titleAnim.SetBool("startCredits", true);
    }

    
}
