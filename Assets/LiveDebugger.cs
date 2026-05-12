using UnityEngine;
using TMPro; // For your TextMeshPro UI
using Android.BLE; // To read the math

public class LiveDebugger : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public TrilaterationManager mathBrain;

    void Update()
    {
        if (mathBrain != null && debugText != null)
        {
            debugText.text = 
                $"--- LIVE BLE MATH ---\n" +
                $"Dist A: {mathBrain.distA:F2}m\n" +
                $"Dist C: {mathBrain.distC:F2}m\n" +
                $"Dist D: {mathBrain.distD:F2}m\n\n" +
                $"Calculated X: {mathBrain.currentBleX:F2}\n" +
                $"Calculated Z(Y): {mathBrain.currentBleY:F2}";
        }
    }
}