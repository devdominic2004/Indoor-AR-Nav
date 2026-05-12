using UnityEngine;
using System.Collections.Generic;

namespace Android.BLE
{
    public class TrilaterationManager : MonoBehaviour
    {
        [Header("ESP32 MAC Addresses")]
        public string macAnchorA = "00:70:07:24:96:D2";
        public string macAnchorC = "00:70:07:7F:DE:4A";
        public string macAnchorD = "00:70:07:25:A6:02";

        [Header("Physical Anchor Positions")]
        public Vector2 posAnchorA = new Vector2(0f, 0f);   
        public Vector2 posAnchorC = new Vector2(6.71f, 0f);   
        public Vector2 posAnchorD = new Vector2(3.36f, 6.53f); 

        [Header("Live Distances")]
        public float distA = -1f;
        public float distC = -1f;
        public float distD = -1f;

        [Header("Live Calculated Coordinates")]
        public float currentBleX = 0f;
        public float currentBleY = 0f;

        [Header("Signal Smoothing (Asymmetric EMA)")]
        [Range(0.01f, 1f)] public float attackFactor = 0.30f; 
        [Range(0.01f, 1f)] public float decayFactor = 0.02f;  
        
        private Dictionary<string, float> smoothedSignals = new Dictionary<string, float>();

        public float CalculateDistance(float rssi) 
        {
            float txPower = -59f; 
            float n = 3.0f;       
            float distance = Mathf.Pow(10, (txPower - rssi) / (10 * n));
            return Mathf.Min(distance, 12f); 
        }

        public void UpdateDeviceSignal(string incomingMac, int rawRssi)
        {
            if (!smoothedSignals.ContainsKey(incomingMac))
            {
                smoothedSignals[incomingMac] = rawRssi;
            }
            else
            {
                float currentSmoothed = smoothedSignals[incomingMac];
                if (rawRssi > currentSmoothed)
                {
                    smoothedSignals[incomingMac] = (rawRssi * attackFactor) + (currentSmoothed * (1.0f - attackFactor));
                }
                else
                {
                    smoothedSignals[incomingMac] = (rawRssi * decayFactor) + (currentSmoothed * (1.0f - decayFactor));
                }
            }

            float finalSmoothedRssi = smoothedSignals[incomingMac];
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

            // Update the invisible variables instead of moving a physical object!
            currentBleX = Mathf.Clamp(calculatedX, -1f, 8.0f);
            currentBleY = Mathf.Clamp(calculatedY, -1f, 8.0f);
        }
    }
}