using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera cam;
    private Transform targetTransform;
    private Transform cameraTransform;

    [Header("Permissions")]
    public bool isPanning;
    public bool isZoom;
    public bool isRotate;

    [Header("Panning")]
    public Vector3 startPanning;
    public Vector3 offsetPanning;

    [Header("Zoom")]
    public float minZoom;
    public float maxZoom;
    public float zoom;
    private float scroll;
    public float scrollDistance = 0.1f;

    [Header("Rotation")]
    public Vector2 mouseAxis;
    public float sensitiveRot = 2.0f;
	
	void Start ()
    {
        cam = GetComponentInChildren<Camera>();
        cameraTransform = cam.transform;
		targetTransform = transform;

        zoom = targetTransform.position.z - cameraTransform.position.z;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if(!isPanning) Orbital();
        Zoom();
        if (!isRotate) Panning();
	}

    void Panning()
    {
        Vector3 worldPoint = Input.mousePosition;
        worldPoint.z = zoom * 1.5f;

        if (Input.GetMouseButtonDown(2))
        {
            startPanning = cam.ScreenToWorldPoint(worldPoint);

            isPanning = true;
        }
        else if (Input.GetMouseButton(2))
        {
            offsetPanning = startPanning - cam.ScreenToWorldPoint(worldPoint);
            targetTransform.localPosition = targetTransform.localPosition + offsetPanning;
        }
        else if (Input.GetMouseButtonUp(2)) isPanning = false;
    }

    void Zoom()
    {
        scroll = Input.mouseScrollDelta.y;

        zoom -= scroll * scrollDistance;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, -zoom);
    }

    void Orbital()
    {

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            isRotate = true;
            mouseAxis.x = Input.GetAxis("Mouse X") * sensitiveRot;
            mouseAxis.y = Input.GetAxis("Mouse Y") * sensitiveRot;

            Vector3 localEulerRot = targetTransform.localEulerAngles;

            localEulerRot.y += mouseAxis.x;
            localEulerRot.x -= mouseAxis.y;

            targetTransform.localRotation = Quaternion.Euler(localEulerRot);
        }
        if (Input.GetMouseButtonUp(0)) isRotate = false;
    }

}
