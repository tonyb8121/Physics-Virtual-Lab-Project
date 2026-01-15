using UnityEngine;

public class GlobalSettingsManager : MonoBehaviour
{
    public static GlobalSettingsManager Instance;

    private const string KEY_SOUND = "Setting_Sound";
    private const string KEY_NOTIFS = "Setting_Notifs";
    // 🚩 Renamed Key to be positive, but keeping old key value for continuity:
    private const string KEY_TIMER_VISIBILITY = "Setting_HideTimer"; 

    public bool IsSoundEnabled { get; private set; }
    public bool AreNotificationsEnabled { get; private set; }
    // ✅ Renamed Property: Default is now TRUE (1)
    public bool IsPracticeTimerVisible { get; private set; } 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else Destroy(gameObject);
    }

    private void LoadSettings()
    {
        IsSoundEnabled = PlayerPrefs.GetInt(KEY_SOUND, 1) == 1;
        AreNotificationsEnabled = PlayerPrefs.GetInt(KEY_NOTIFS, 1) == 1;
        // ⭐ FIX: Default to Visible (1) as requested by user
        IsPracticeTimerVisible = PlayerPrefs.GetInt(KEY_TIMER_VISIBILITY, 1) == 1; 
        
        ApplySettings();
    }

    public void ToggleSound(bool isEnabled)
    {
        IsSoundEnabled = isEnabled;
        PlayerPrefs.SetInt(KEY_SOUND, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettings();
    }

    public void ToggleNotifications(bool isEnabled)
    {
        AreNotificationsEnabled = isEnabled;
        PlayerPrefs.SetInt(KEY_NOTIFS, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"Notifications set to: {isEnabled}");
    }

    // ✅ Updated Method: Accepts isVisible
    public void TogglePracticeTimer(bool isVisible) 
    {
        IsPracticeTimerVisible = isVisible;
        PlayerPrefs.SetInt(KEY_TIMER_VISIBILITY, isVisible ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        // Controls the global audio setting
        AudioListener.volume = IsSoundEnabled ? 1.0f : 0.0f;
    }
}