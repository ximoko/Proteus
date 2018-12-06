using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class destroyer : MonoBehaviour {
	GameObject cosa;
	// Use this for initialization
	public void Start () {
		cosa = GameObject.Find("Intro");
		Destroy(cosa,(float)3);
		Destroy (this.gameObject,(float)5); 
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
