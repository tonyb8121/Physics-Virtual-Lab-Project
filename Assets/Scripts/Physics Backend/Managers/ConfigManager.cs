using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Loads and holds global app configuration.
/// Uses UnityWebRequest to safely read StreamingAssets on Android.
/// </summary>
public class ConfigManager : MonoBehaviour
{
    private static ConfigManager _instance;
    public static ConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ConfigManager");
                _instance = go.AddComponent<ConfigManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public AppConfigData Config { get; private set; }

    public IEnumerator LoadConfigAsync()
    {
        string fileName = "AppConfig.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, "Configs", fileName);
        string jsonContent = "";

        Logger.Log($"Loading config from: {filePath}");

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android requires UnityWebRequest to read from APK (jar:file://)
        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Logger.LogError($"Failed to load config: {request.error}. Using defaults.");
                UseDefaultConfig();
                yield break;
            }
            jsonContent = request.downloadHandler.text;
        }
#else
        // Editor/Windows/iOS
        if (File.Exists(filePath))
        {
            jsonContent = File.ReadAllText(filePath);
        }
        else
        {
            Logger.LogError("Config file not found. Using defaults.");
            UseDefaultConfig();
            yield break;
        }
#endif

        try
        {
            Config = JsonConvert.DeserializeObject<AppConfigData>(jsonContent);
            Logger.Log("Config loaded successfully.", Color.green);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"JSON Parse Error: {ex.Message}");
            UseDefaultConfig();
        }
        
        yield return null;
    }

    private void UseDefaultConfig()
    {
        Config = new AppConfigData
        {
            offlineMode = true,
            defaultQualityLevel = "Medium",
            enableAnalytics = true,
            availableModules = new string[] { "ProjectileMotion", "PendulumSystem" },
            syncInterval = 30
        };
    }
}

[System.Serializable]
public class AppConfigData
{
    public bool offlineMode;
    public string defaultLanguage;
    public string defaultQualityLevel;
    public bool enableAnalytics;
    public string[] availableModules;
    public int syncInterval;
    public int maxOfflineQueueSize;
}