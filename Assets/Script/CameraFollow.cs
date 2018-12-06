using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //public float CameraMovSpeed;
    //public GameObject CameraFollowObject;
    //private Vector3 FollowPos;
    public float maxAngle;
    public float minAngle;
    public float inputSens;
    //public GameObject Cameraobj;
    //public GameObject Playerobj;
    //public float camDistanceX;
    //public float camDistanceY;
    //public float camDistanceZ;
    public float mouseX;
    public float mouseY;
    public float finalInputX;
    public float finalInputZ;
    //public float smoothX;
    //public float smoothY;
    private float rotY = 0.0f;
    private float rotX = 0.0f;

    void Start ()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
	}
	
	void Update ()
    {
        //Configurando los Inputs de rotación
        //float inputX = Input.GetAxis("RigthStickHorizontal");
        //float inputZ = Input.GetAxis("RightStickVertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        //finalInputX = inputX + mouseX;
        //finalInputZ = inputZ + mouseY;

        //Calculando los Inputs de rotación
        rotY += finalInputX * inputSens * Time.deltaTime;
        rotX += finalInputZ * inputSens * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -minAngle, maxAngle);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        transform.rotation = localRotation;
    }

    /*private void LateUpdate()
    {
        FixedUpdate ();
    }

    private void FixedUpdate()
    {
        //Dar el "target" a seguir
        Transform target = CameraFollowObject.transform;

        //La camara se pone detrás del "target" a seguir
        float step = CameraMovSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);
    } */
}
