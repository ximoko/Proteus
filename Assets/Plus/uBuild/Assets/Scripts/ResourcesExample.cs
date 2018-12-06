using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ResourcesExample : MonoBehaviour {
	
	public float amount;
	public string resourceName;
	public string addKey;
	public string removeKey;
	
	public GameObject resourcesLabel;
	Text resourcesText;
	
	void Start(){
		resourcesText = resourcesLabel.GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		
		//Example of how to use resources for building
		//You can for example change Input.GetKeyDown to void OnTriggerEnter to collect resources
		
		if(Input.GetKeyDown(addKey)){
			PlayerPrefs.SetFloat(resourceName, PlayerPrefs.GetFloat(resourceName) + amount);
		}
		if(Input.GetKeyDown(removeKey)){
			PlayerPrefs.SetFloat(resourceName, PlayerPrefs.GetFloat(resourceName) - amount);
		}
		
		//display amount
		resourcesText.text = PlayerPrefs.GetFloat(resourceName) + " " + resourceName;
	}
}
