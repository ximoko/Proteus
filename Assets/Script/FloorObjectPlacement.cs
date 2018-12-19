using UnityEngine;
using System.Collections;
using System;

public class FloorObjectPlacement : MonoBehaviour
{

    [Tooltip("The prefab to place")]
    public GameObject prefabPlacementObject;
    [Tooltip("Placement success condition, e.g. a green transparent cube")]
    public GameObject prefabOK;
    [Tooltip("Placement fail condition, e.g. a red transparent cube")]
    public GameObject prefabFail;

    [Tooltip("Grid resolution, e.g. for 4x4 place grid 2 would allow 2x2 object to be placed")]
    public float grid = 2.0f;

    // Mask of the ground to hit
    [Tooltip("Which layer to use for raycast target")]
    public LayerMask mask = -1;

    // Store which spaces are in use
    int[,] usedSpace;

    private GameObject placementObject = null;
    private GameObject areaObject = null;

    private Bounds placementBounds;

    bool mouseClick = false;
    Vector3 lastPos;

    // Use this for initialization
    void Start()
    {

        // Check terrain first because it has usually no Renderer
        if(GetComponent<Terrain>() != null)
        {
            placementBounds = GetComponent<Terrain>().terrainData.bounds;
        }
        else if(GetComponent<Renderer>() != null)
        {
            placementBounds = GetComponent<Renderer>().bounds;
        }

        Vector3 slots = placementBounds.size / grid;
        usedSpace = new int[Mathf.CeilToInt(slots.x), Mathf.CeilToInt(slots.z)];
        for(var x = 0; x < Mathf.CeilToInt(slots.x); x++)
        {
            for(var z = 0; z < Mathf.CeilToInt(slots.z); z++)
            {
                usedSpace[x, z] = 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 point;

        // Check for mouse ray collision with this object
        if(getTargetLocation(out point))
        {
            //I'm lazy and use the object size from the renderer..
            Vector3 halfSlots = placementBounds.size / 2.0f;

            // Transform position is the center point of this object, x and z are grid slots from 0..slots-1
            int x = (int)Math.Round(Math.Round(point.x - transform.position.x + halfSlots.x - grid / 2.0f) / grid);
            int z = (int)Math.Round(Math.Round(point.z - transform.position.z + halfSlots.z - grid / 2.0f) / grid);

            // Calculate the quantized world coordinates on where to actually place the object
            point.x = (float)(x) * grid - halfSlots.x + transform.position.x + grid / 2.0f;
            point.z = (float)(z) * grid - halfSlots.z + transform.position.z + grid / 2.0f;

            // Create an object to show if this area is available for building
            // Re-instantiate only when the slot has changed or the object not instantiated at all
            if(lastPos.x != x || lastPos.z != z || areaObject == null)
            {
                lastPos.x = x;
                lastPos.z = z;
                if(areaObject != null)
                {
                    Destroy(areaObject);
                }
                areaObject = (GameObject)Instantiate(usedSpace[x, z] == 0 ? prefabOK : prefabFail, point, Quaternion.identity);
            }

            // Create or move the object
            if(!placementObject)
            {
                placementObject = (GameObject)Instantiate(prefabPlacementObject, point, Quaternion.identity);
            }
            else
            {
                placementObject.transform.position = point;
            }

            // On left click, insert the object to the area and mark it as "used"
            if(Input.GetMouseButtonDown(0) && mouseClick == false)
            {
                mouseClick = true;
                // Place the object
                if(usedSpace[x, z] == 0)
                {
                    Debug.Log("Placement Position: " + x + ", " + z);
                    usedSpace[x, z] = 1;

                    // ToDo: place the result somewhere..
                    Instantiate(prefabPlacementObject, point, Quaternion.identity);
                }
            }
            else if(!Input.GetMouseButtonDown(0))
            {
                mouseClick = false;
            }

        }
        else
        {
            if(placementObject)
            {
                Destroy(placementObject);
                placementObject = null;
            }
            if(areaObject)
            {
                Destroy(areaObject);
                areaObject = null;
            }
        }
    }

    bool getTargetLocation(out Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hitInfo = new RaycastHit();
        if(Physics.Raycast(ray, out hitInfo, Mathf.Infinity, mask))
        {
            if(hitInfo.collider == GetComponent<Collider>())
            {
                point = hitInfo.point;
                return true;
            }
        }
        point = Vector3.zero;
        return false;
    }
}