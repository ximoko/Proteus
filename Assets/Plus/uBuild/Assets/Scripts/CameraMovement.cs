using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CameraMovement : MonoBehaviour {
	
	//variables visible in the inspector
	public float movespeed;
	public float zoomSpeed;
	public float mouseSensitivity;
    public float clampAngle;
	
	//not visible in the inspector
    float rotationY = 0.0f;
    float rotationX = 0.0f;
 
    void OnEnable()
	{
    Vector3 rot = transform.localRotation.eulerAngles;
    rotationY = rot.y;
    rotationX = rot.z;
    }
	
	void Update(){
	//if key gets pressed move left/right
	if(Input.GetKey("a") && !EventSystem.current.IsPointerOverGameObject()){
	transform.Translate(Vector3.right * Time.deltaTime * -movespeed);
	}
	if(Input.GetKey("d") && !EventSystem.current.IsPointerOverGameObject()){
	transform.Translate(Vector3.right * Time.deltaTime * movespeed);
	}
	
	//if key gets pressed move up/down
	if(Input.GetKey("w") && !EventSystem.current.IsPointerOverGameObject()){
	transform.Translate(Vector3.forward * Time.deltaTime * movespeed);
	}
	if(Input.GetKey("s") && !EventSystem.current.IsPointerOverGameObject()){
	transform.Translate(Vector3.forward * Time.deltaTime * -movespeed);
	}
	
	//if scrollwheel is down...
	if(Input.GetMouseButton(2)){
	//get mouse position
    float mouseX = Input.GetAxis("Mouse X");
    float mouseY = -Input.GetAxis("Mouse Y");
	
	//set x and y rotation based on mouse position
    rotationY += mouseX * mouseSensitivity * Time.deltaTime;
    rotationX += mouseY * mouseSensitivity * Time.deltaTime;
	
	//clamp x rotation to limit it
    rotationX = Mathf.Clamp(rotationX, -clampAngle, clampAngle);
	
	//apply rotation
    transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
	}
	
	//move camera when you scroll
	transform.Translate(new Vector3(0, 0, Input.GetAxis("Mouse ScrollWheel")) * Time.deltaTime * zoomSpeed);
	}
}
