using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalCamera : MonoBehaviour {

    public float speed = 8f;
    public float distance = 3f;

    public Transform target;

    private Vector2 input;

	// Update is called once per frame
	void Update () {

        if (Input.GetKey("o"))
        {
            input += new Vector2(Input.GetAxis("Mouse X") * speed, Input.GetAxis("Mouse Y") * speed);
            
            transform.localRotation = Quaternion.Euler(input.y, input.x, 0); ;
            transform.localPosition = target.position - (transform.localRotation * Vector3.forward * distance);
        }
    }
}
