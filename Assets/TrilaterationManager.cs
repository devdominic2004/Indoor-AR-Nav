using UnityEngine;
using System.Collections.Generic; // ADDED: Required for the Dictionary memory bank!

namespace Android.BLE
{
    public class TrilaterationManager : MonoBehaviour
    {
        [Header("ESP32 MAC Addresses")]
        public string macAnchorA = "00:70:07:24:96:D2";
        public string macAnchorC = "00:70:07:7F:DE:4A";
        public string macAnchorD = "00:70:07:25:A6:02";

        [Header("Physical Anchor Positions (in Meters)")]
        // Anchor A: Bottom-Left Corner (Your Starting Point / Origin)
        public Vector2 posAnchorA = new Vector2(0f, 0f);   
        
        // Anchor C: Bottom-Right Corner (Across the 3.05m breadth)
        public Vector2 posAnchorC = new Vector2(3.05f, 0f);   
        
        // Anchor D: Top-Center (7.74m down the room, halfway across the 3.05m breadth)
        public Vector2 posAnchorD = new Vector2(1.525f, 7.74f); 

        [Header("Live Distances")]
        public float distA = -1f;
        public float distC = -1f;
        public float distD = -1f;

        [Header("The Object That Moves")]
        public Transform playerMarker;

        [Header("Signal Smoothing (Asymmetric EMA)")]
        [Range(0.01f, 1f)]
        public float attackFactor = 0.30f; // 30% trust when getting STRONGER (Unblocked)
        [Range(0.01f, 1f)]
        public float decayFactor = 0.02f;  // 2% trust when getting WEAKER (Human blocking)
        
        // Memory bank for the smoothed values
        private Dictionary<string, float> smoothedSignals = new Dictionary<string, float>();

        // CHANGED: Now accepts a 'float' instead of an 'int' for better precision!
        public float CalculateDistance(float rssi) 
        {
            float txPower = -59f; 
            
            // 1. TUNE THE ENVIRONMENT: 3.0 is much more realistic for indoor spaces
            float n = 3.0f;       
            float distance = Mathf.Pow(10, (txPower - rssi) / (10 * n));
            
            // 2. CAP THE DISTANCE: If the signal drops hard, don't let it exceed 12 meters!
            return Mathf.Min(distance, 12f); 
        }

        public void UpdateDeviceSignal(string incomingMac, int rawRssi)
        {
            // --- THE ASYMMETRIC EMA BOUNCER ---
            // 1. If we have never seen this MAC before, just log its raw number
            if (!smoothedSignals.ContainsKey(incomingMac))
            {
                smoothedSignals[incomingMac] = rawRssi;
            }
            else
            {
                float currentSmoothed = smoothedSignals[incomingMac];

                // 2. THE ASYMMETRIC FILTER
                if (rawRssi > currentSmoothed)
                {
                    // Signal is STRONGER: The path is clear! React quickly.
                    smoothedSignals[incomingMac] = (rawRssi * attackFactor) + (currentSmoothed * (1.0f - attackFactor));
                }
                else
                {
                    // Signal is WEAKER: Someone is blocking it! React very slowly.
                    smoothedSignals[incomingMac] = (rawRssi * decayFactor) + (currentSmoothed * (1.0f - decayFactor));
                }
            }

            // 3. Grab the highly precise, smoothed decimal
            float finalSmoothedRssi = smoothedSignals[incomingMac];

            // 4. Calculate the distance using the SMOOTHED number instead of the raw one
            float distanceInMeters = CalculateDistance(finalSmoothedRssi);

            if (incomingMac == macAnchorA) distA = distanceInMeters;
            else if (incomingMac == macAnchorC) distC = distanceInMeters;
            else if (incomingMac == macAnchorD) distD = distanceInMeters;

            if (distA > 0 && distC > 0 && distD > 0)
            {
                PerformTrilateration();
            }
        }

        void PerformTrilateration()
        {
            float x1 = posAnchorA.x, y1 = posAnchorA.y;
            float x2 = posAnchorC.x, y2 = posAnchorC.y;
            float x3 = posAnchorD.x, y3 = posAnchorD.y;

            float A = 2 * x2 - 2 * x1;
            float B = 2 * y2 - 2 * y1;
            float C_val = (distA * distA) - (distC * distC) - (x1 * x1) + (x2 * x2) - (y1 * y1) + (y2 * y2);

            float D_val = 2 * x3 - 2 * x2;
            float E = 2 * y3 - 2 * y2;
            float F = (distC * distC) - (distD * distD) - (x2 * x2) + (x3 * x3) - (y2 * y2) + (y3 * y3);

            float denominator = (A * E - B * D_val);
            if (Mathf.Abs(denominator) < 0.001f) return; 

            float calculatedX = (C_val * E - F * B) / denominator;
            float calculatedY = (A * F - C_val * D_val) / denominator; 

            // 3. THE INVISIBLE FENCE: Clamp the final coordinates to your physical room!
            // We know your room goes from X: 0 to 3.05, and Y: 0 to 7.74. 
            // We add a tiny 1-meter buffer just in case you step outside the boundaries.
            calculatedX = Mathf.Clamp(calculatedX, -1f, 4.0f);
            calculatedY = Mathf.Clamp(calculatedY, -1f, 9.0f);

            Vector3 newPosition = new Vector3(calculatedX, playerMarker.position.y, calculatedY);
            
            playerMarker.position = Vector3.Lerp(playerMarker.position, newPosition, Time.deltaTime * 3f);
        }
    }
}