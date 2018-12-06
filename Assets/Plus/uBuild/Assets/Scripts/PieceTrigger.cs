using UnityEngine;

public class PieceTrigger : MonoBehaviour {
	
	//visible in the inspector
	public Vector3 scale;
	
	//not visible in the inspector
	[HideInInspector]
	public bool triggered;
	[HideInInspector]
	public int type;
	[HideInInspector]
	public int layer;
 
    void FixedUpdate(){
		//normaly piece is not triggered
        triggered = false;
    }
	
	//when a collider stays in the trigger, the piece is triggered and not placeable
    void OnTriggerStay(Collider other){
		if(other.gameObject.tag != "Ground" && !other.isTrigger && !other.gameObject.transform.IsChildOf(transform)){
        triggered = true;
		}
    }
}
