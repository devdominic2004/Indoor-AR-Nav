using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceObject : MonoBehaviour
{
    public GameObject objectToPlace;
    
    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    // This will keep track of our single cube
    private GameObject spawnedCube = null;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // CHANGED: "TrackableType.Planes" makes the hit area much larger and more forgiving
            if (raycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;
                Vector3 hoverPos = new Vector3(hitPose.position.x, hitPose.position.y + 0.15f, hitPose.position.z);
                
                if (spawnedCube == null)
                {
                    // Spawn it the first time you tap
                    spawnedCube = Instantiate(objectToPlace, hoverPos, hitPose.rotation);
                }
                else
                {
                    // If it already exists, just teleport it to the new tap location
                    spawnedCube.transform.position = hoverPos;
                }
            }
        }
    }
}