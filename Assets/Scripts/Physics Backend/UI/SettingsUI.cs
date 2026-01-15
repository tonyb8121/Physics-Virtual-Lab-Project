using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Toggle notificationToggle;
    [SerializeField] private Toggle timerToggle; // Represents the Quiz Timer Visibility
    [SerializeField] private Button closeButton;

    private void Start()
    {
        // Add Listeners ONCE in Start
        soundToggle.onValueChanged.AddListener(OnSoundChanged);
        notificationToggle.onValueChanged.AddListener(OnNotifChanged);
        closeButton.onClick.AddListener(CloseSettings);
        
        // Timer listener: The toggle state (val) now represents IsVisible (TRUE/FALSE)
        if (timerToggle) timerToggle.onValueChanged.AddListener((isVisible) => {
            if (GlobalSettingsManager.Instance != null)
                // Use the new method signature with 'isVisible'
                GlobalSettingsManager.Instance.TogglePracticeTimer(isVisible);
        });
        
        settingsPanel.SetActive(false);
    }

    // ✅ FIX: Use OnEnable to refresh UI every time panel opens
    private void OnEnable()
    {
        if (GlobalSettingsManager.Instance != null)
        {
            // Sync UI with actual saved data
            soundToggle.isOn = GlobalSettingsManager.Instance.IsSoundEnabled;
            notificationToggle.isOn = GlobalSettingsManager.Instance.AreNotificationsEnabled;
            
            if (timerToggle) 
            {
                // ⭐ FIX: Changed IsPracticeTimerHidden to IsPracticeTimerVisible ⭐
                // The toggle is ON if the timer is visible, as per the user's default request.
                timerToggle.isOn = GlobalSettingsManager.Instance.IsPracticeTimerVisible;
            }
        }
    }

    public void OpenSettings()
    {
        // When opening the panel, ensure the UI is synced with saved settings
        OnEnable(); 
        settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    private void OnSoundChanged(bool isOn)
    {
        if (GlobalSettingsManager.Instance != null)
            GlobalSettingsManager.Instance.ToggleSound(isOn);
    }

    private void OnNotifChanged(bool isOn)
    {
        if (GlobalSettingsManager.Instance != null)
            // Ensure GlobalSettingsManager has the ToggleNotifications method (which we added previously)
            GlobalSettingsManager.Instance.ToggleNotifications(isOn);
    }
}