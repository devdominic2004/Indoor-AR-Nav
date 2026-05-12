using UnityEngine;
using UnityEngine.AI; // CRITICAL: This is the brain that reads the blue floor plan

[RequireComponent(typeof(LineRenderer))]
public class ARPathNavigator : MonoBehaviour
{
    public Transform arCamera; 
    
    private Vector3 currentDestinationCoordinate;
    private bool hasActiveRoute = false;
    
    private LineRenderer pathLine;
    private NavMeshPath calculatedRoute; // Holds the corners of the smart path

    void Start()
    {
        pathLine = GetComponent<LineRenderer>();
        pathLine.startWidth = 0.15f; 
        pathLine.endWidth = 0.15f;
        pathLine.numCapVertices = 5; 
        pathLine.numCornerVertices = 5;
        
        calculatedRoute = new NavMeshPath(); // Initialize the brain
    }

    void Update()
    {
        if (hasActiveRoute && arCamera != null)
        {
            Vector3 startPoint = new Vector3(arCamera.position.x, 0f, arCamera.position.z);
            Vector3 endPoint = new Vector3(currentDestinationCoordinate.x, 0f, currentDestinationCoordinate.z);

            // Ask Unity's AI to find the best route dodging the shelves
            NavMesh.CalculatePath(startPoint, endPoint, NavMesh.AllAreas, calculatedRoute);

            // If the AI successfully found a route, draw the line through all the corners!
            if (calculatedRoute.status == NavMeshPathStatus.PathComplete)
            {
                pathLine.positionCount = calculatedRoute.corners.Length;
                pathLine.SetPositions(calculatedRoute.corners);
            }
            else
            {
                // Backup Plan: If the AI fails (e.g. target is off the grid), draw a straight line
                pathLine.positionCount = 2;
                pathLine.SetPosition(0, startPoint);
                pathLine.SetPosition(1, endPoint);
            }
        }
        else
        {
            pathLine.positionCount = 0; 
        }
    }

    public void SetNewRoute(float targetX, float targetZ)
    {
        currentDestinationCoordinate = new Vector3(targetX, 0f, targetZ);
        hasActiveRoute = true;
        Debug.Log($"AI Route dynamically set to X: {targetX}, Z: {targetZ}");
    }
}