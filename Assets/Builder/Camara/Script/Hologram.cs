//Attach this script to a GameObject with a Renderer (go to Create>3D Object and select one of the first 6 options to create a GameObject with a Renderer automatically attached).
//This script changes the Color of your GameObject’s Material when your mouse hovers over it in Play Mode.

using UnityEngine;

public class Hologram : MonoBehaviour
{
    Renderer m_Renderer;

    void Start()
    {
        //Fetch the Renderer component of the GameObject
        m_Renderer = GetComponent<Renderer>();
    }

    //Run your mouse over the GameObject to change the Renderer's material color to clear
    //void OnMouseOver()
    //{
        
    //}

    //Change the Material's Color back to white when the mouse exits the GameObject
    void OnMouseExit()
    {        
        m_Renderer.material.color = Color.clear;
    }
}