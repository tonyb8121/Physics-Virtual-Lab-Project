using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OfflineMenuController : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private Button btnPendulum;
    [SerializeField] private Button btnProjectile;
    [SerializeField] private Button btnAttemptQuiz;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnExit;

    [Header("Feature Panels")]
    [SerializeField] private SettingsUI settingsScript; // Drag the script from the SettingsPanel here

    [Header("Scene Configuration")]
    [SerializeField] private string mainSceneName = "MainScene";

    private void Start()
    {
        // Experiment Buttons
        btnPendulum.onClick.AddListener(() => LoadScene("Pendulum"));
        btnProjectile.onClick.AddListener(() => LoadScene("Projectile"));

        // ✅ Attempt Quiz: Loads MainScene but targets the Quiz Tab
        btnAttemptQuiz.onClick.AddListener(OnAttemptQuizClicked);

        // ✅ Settings: Opens the panel
        btnSettings.onClick.AddListener(() => {
            if (settingsScript != null) settingsScript.OpenSettings();
        });

        // Exit
        btnExit.onClick.AddListener(() => {
            LoadScene("Home"); // Or Application.Quit();
        });
    }

    private void OnAttemptQuizClicked()
    {
        // We need to tell MainScene to open the Quiz Tab immediately
        // We can use PlayerPrefs as a simple messenger

        // 1 = Quiz Tab index
        PlayerPrefs.SetInt("OpenTargetTab", 1); 
        
        // ✅ NEW: Signal to open the specific "Tiers" sub-tab within the Quiz panel
        PlayerPrefs.SetString("OpenTargetSubTab", "Tiers"); 
        
        PlayerPrefs.Save();

        LoadScene(mainSceneName);
    }

    private void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }
}