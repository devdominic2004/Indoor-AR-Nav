using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using TMPro;
using Android.BLE; // The key to the Math Brain!

public class NativeBleScanner : MonoBehaviour
{
    [Header("Core Links")]
    public TrilaterationManager mathBrain;
    public TextMeshProUGUI debugText;

    [Header("ESP32 Filter")]
    public List<string> targetMACs = new List<string> {
        "00:70:07:24:96:D2", // Replace with ESP32 #1 MAC
        "00:70:07:7F:DE:4A", // Replace with ESP32 #2 MAC
        "00:70:07:25:A6:02"  // Replace with ESP32 #3 MAC
    };

    // Android Native Objects
    private AndroidJavaObject bluetoothAdapter;
    private BleScanCallback scanCallback;
    private bool isScanning = false;

    // The Memory Bank to hold all 3 signals at once!
    private Dictionary<string, int> activeSignals = new Dictionary<string, int>();

    // Threading queue (Android callbacks happen on a background thread, Unity UI needs the main thread)
    private readonly Queue<System.Action> executeOnMainThread = new Queue<System.Action>();

    private void Awake()
    {
        if (mathBrain == null)
        {
            mathBrain = FindObjectOfType<TrilaterationManager>();
            if (mathBrain != null) Debug.LogWarning("Math Brain Linked Automatically!");
        }
    }

    private IEnumerator Start()
    {
        if (debugText != null) debugText.text = "Starting Native Scanner...";
        yield return new WaitForSeconds(2.0f);

        // Step 1: Request Android 12 Permissions
        if (debugText != null) debugText.text = "Checking Permissions...";
        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");
            yield return new WaitForSeconds(0.5f);
            Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
            yield return new WaitForSeconds(0.5f);
            Permission.RequestUserPermission("android.permission.ACCESS_FINE_LOCATION");
        }

        while (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (debugText != null) debugText.text = "Permissions OK. Waking up Android Antenna...";

        // Step 2: Boot the native Android engine
        InitializeNativeBluetooth();
    }

    private void Update()
    {
        // Execute UI and Math updates on the main Unity thread
        lock (executeOnMainThread)
        {
            while (executeOnMainThread.Count > 0)
            {
                executeOnMainThread.Dequeue().Invoke();
            }
        }
    }

    private void InitializeNativeBluetooth()
    {
        try
        {
            // THE FIX: We bypass the generic system service and directly grab the static BluetoothAdapter!
            AndroidJavaClass bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter");
            bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");

            if (bluetoothAdapter == null)
            {
                QueueOnMainThread(() => { if (debugText != null) debugText.text = "ERROR: No Bluetooth Hardware Found!"; });
                return;
            }

            // Initialize our custom callback receiver
            scanCallback = new BleScanCallback(this);

            // Hit the gas pedal: Start the continuous native scan using the Interface API!
            // THE FIX: Tell Unity to expect a boolean back!
bool success = bluetoothAdapter.Call<bool>("startLeScan", scanCallback);
isScanning = success;

if (!success)
{
    QueueOnMainThread(() => { if (debugText != null) debugText.text = "ERROR: Failed to start scanner!"; });
    return;
}

            QueueOnMainThread(() => { if (debugText != null) debugText.text = "Innoquest Radar ONLINE! Listening for ESP32s..."; });
        }
        catch (System.Exception e)
        {
            QueueOnMainThread(() => { if (debugText != null) debugText.text = "NATIVE CRASH: " + e.Message; });
            Debug.LogError("Native BLE Error: " + e.Message);
        }
    }

    // Called by the Java background thread when a signal is caught
    public void OnDeviceFound(string macAddress, int rssi)
    {
        // THE BOUNCER: If the MAC address isn't in our list, instantly ignore it!
        if (!targetMACs.Contains(macAddress)) return;

        QueueOnMainThread(() =>
        {
            // 1. Update the memory bank with the newest RSSI for this specific MAC
            activeSignals[macAddress] = rssi;

            // 2. Build a beautiful multi-line dashboard for the UI
            string dashboard = "INNOQUEST RADAR ONLINE\n===================\n";
            
            foreach (var signal in activeSignals)
            {
                // This prints each ESP32 on its own dedicated line!
                dashboard += $"ESP [{signal.Key}] : {signal.Value} dBm\n";
            }

            if (debugText != null) debugText.text = dashboard;
            
            // 3. Keep feeding the math brain exactly like before!
            if (mathBrain != null)
            {
                mathBrain.UpdateDeviceSignal(macAddress, rssi);
            }
        });
    }

    public void QueueOnMainThread(System.Action action)
    {
        lock (executeOnMainThread)
        {
            executeOnMainThread.Enqueue(action);
        }
    }

    private void OnDestroy()
    {
        if (isScanning && bluetoothAdapter != null && scanCallback != null)
        {
            bluetoothAdapter.Call("stopLeScan", scanCallback);
        }
    }
}

// =====================================================================
// THE NATIVE BRIDGE: This intercepts the Android OS Java callbacks
// =====================================================================
public class BleScanCallback : AndroidJavaProxy
{
    private NativeBleScanner scanner;

    // We use the inner interface LeScanCallback which Unity's Proxy system perfectly supports!
    public BleScanCallback(NativeBleScanner scanner) : base("android.bluetooth.BluetoothAdapter$LeScanCallback")
    {
        this.scanner = scanner;
    }

    // This matches the exact method signature of the LeScanCallback interface
    public void onLeScan(AndroidJavaObject device, int rssi, byte[] scanRecord)
    {
        string macAddress = device.Call<string>("getAddress");
        scanner.OnDeviceFound(macAddress, rssi);
    }
}