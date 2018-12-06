using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour {
	
	//variables visible in the inspector
	public float smooth;
    public int angleOpen;
	
	//variables not visible in the inspector
	[HideInInspector]
	public bool open;
	[HideInInspector]
	public bool possible;
	
    private int angleClosed = 0; 
	
    void Update(){
	//if player is in range and we're not in build mode
	if(possible && !uBuildManager.buildMode){
		//check door key to open/close door
        if(Input.GetKeyDown(GameObject.Find("uBuildManager").GetComponent<uBuildManager>().doorKey)){
		open = !open;
        }
    }
       
	//if door should be open
    if(open){ 
	//get the target angle
    Quaternion openangle = Quaternion.Euler(0, angleOpen, 0);
	//rotate towards the target angle
    transform.localRotation = Quaternion.Slerp(transform.localRotation, openangle, Time.deltaTime * smooth);
	}
    else{
	//get another target angle
    Quaternion closedangle = Quaternion.Euler (0, angleClosed, 0);
	//rotate towards the target angle
	transform.localRotation = Quaternion.Slerp(transform.localRotation, closedangle, Time.deltaTime * smooth);
    }
    }
	
	//when door is triggered by the player, make opening/closing the door possible
    void OnTriggerEnter(Collider other){
	if(other.gameObject == GameObject.Find("uBuildManager").GetComponent<uBuildManager>().character){
    possible = true;
    }
    }
    
	//when door is triggered by the player again, make opening/closing the door impossible
	void OnTriggerExit (Collider other){
    if(other.gameObject == GameObject.Find("uBuildManager").GetComponent<uBuildManager>().character){
    possible = false;
    }
    }
}
