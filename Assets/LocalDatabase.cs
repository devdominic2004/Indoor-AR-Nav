using UnityEngine;
using System.Collections.Generic;

// 1. These classes exactly match the structure of your JSON file
[System.Serializable]
public class TargetLocation
{
    public string targetName;
    public float x;
    public float z;
}

[System.Serializable]
public class LocationList
{
    public TargetLocation[] locations;
}

public class LocalDatabase : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Drag your LibraryData.json file here")]
    public TextAsset jsonFile;

    [Header("Navigation Link")]
    public ARPathNavigator navigator;

    private List<TargetLocation> database = new List<TargetLocation>();

    void Start()
    {
        // 2. Automatically load and parse the JSON when the app starts
        if (jsonFile != null)
        {
            LocationList loadedData = JsonUtility.FromJson<LocationList>(jsonFile.text);
            database.AddRange(loadedData.locations);
            Debug.Log($"Database Loaded: Found {database.Count} locations.");
        }
        else
        {
            Debug.LogError("No JSON file attached!");
        }
    }

    // 3. The UI Buttons will call this function
    public void SearchAndNavigate(string searchName)
    {
        // Look through the database for the requested target
        foreach (TargetLocation loc in database)
        {
            if (loc.targetName == searchName)
            {
                // Found it! Send the raw X and Z to the line renderer
                navigator.SetNewRoute(loc.x, loc.z);
                return;
            }
        }

        Debug.LogWarning("Location not found in database: " + searchName);
    }
}