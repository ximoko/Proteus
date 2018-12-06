using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class ShowDescription : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler {

	//not visible in the inspector
	private GameObject description;
	
	void Start(){
		//find button description object and turn it off
		description = transform.Find("Description").gameObject;
		description.SetActive(false);
	}
	
    public void OnPointerEnter (PointerEventData eventData) {
		//set description active on hover
        description.SetActive(true);
    }
 
    public void OnPointerExit (PointerEventData eventData) {
		//hide description on exit
        description.SetActive(false);
    }
	
	public void OnPointerDown (PointerEventData eventData) {
		//hide description on click
        description.SetActive(false);
    }
}
