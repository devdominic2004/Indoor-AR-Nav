using UnityEngine;
using Android.BLE; 

public class ManualARAligner : MonoBehaviour
{
    [Header("Core Links")]
    public Transform xrOrigin;
    public Camera arCamera;
    public TrilaterationManager mathBrain;

    public void CalibrateWorld()
    {
        // 1. ROTATION OVERRIDE
        // We assume you are physically looking at Anchor D when you press Calibrate.
        // This spins the entire XR Origin opposite to your camera's local rotation,
        // permanently forcing Unity's +Z axis to point straight down your physical hall.
        float cameraLocalY = arCamera.transform.localEulerAngles.y;
        xrOrigin.rotation = Quaternion.Euler(0, -cameraLocalY, 0);

        // 2. POSITION SNAP
        // Now that the grid is facing the correct way, we snap the position.
        float bleX = mathBrain.currentBleX;
        float bleZ = mathBrain.currentBleY;

        float offsetX = bleX - arCamera.transform.position.x;
        float offsetZ = bleZ - arCamera.transform.position.z;

        xrOrigin.position += new Vector3(offsetX, 0, offsetZ);

        Debug.Log("World Aligned! Rotation and Position are perfectly synced.");
    }
}