using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCamera : MonoBehaviour {

	public GameObject firstcamera;
	public GameObject thirdcamera;
	public bool free = false;


	// Use this for initialization
	void Start () {
		firstcamera = GameObject.FindGameObjectWithTag("MainCamera");
	
	}
	
	// Update is called once per frame
	public void Update () 
	{
		if (Input.GetKeyDown(KeyCode.Tab)) free = !free;

		if (free==true)
		{
			firstcamera.SetActive(false); 
			thirdcamera.SetActive(true);
		}

		if (free==false)
		{
			firstcamera.SetActive(true); 
			thirdcamera.SetActive(false);
		}		
	}
}
