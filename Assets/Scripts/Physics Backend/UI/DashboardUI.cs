using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main menu dashboard. Generates module cards.
/// </summary>
public class DashboardUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private Transform moduleListContainer; // Object with GridLayoutGroup
    [SerializeField] private GameObject moduleCardPrefab; // Needs a button and text
    
    [Header("User Info")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private Button logoutButton;
    
    private void Start()
    {
        if (logoutButton) logoutButton.onClick.AddListener(OnLogoutClicked);
        
        LoadUserData();
        GenerateModuleCards();
    }
    
    private void LoadUserData()
    {
        // Check if AuthManager exists before using it
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
        {
            string name = AuthManager.Instance.CurrentUser.DisplayName;
            if (string.IsNullOrEmpty(name)) name = AuthManager.Instance.CurrentUser.Email;
            welcomeText.text = $"Hi, {name}!";
        }
        else
        {
            welcomeText.text = "Offline Mode";
        }
    }
    
    private void GenerateModuleCards()
    {
        // clear existing
        foreach (Transform child in moduleListContainer) 
        {
            Destroy(child.gameObject);
        }
        
        // Check if ConfigManager exists
        if (ConfigManager.Instance == null)
        {
            Debug.LogWarning("ConfigManager not found. Cannot load modules.");
            return;
        }
        
        // Get modules from Config
        string[] modules = ConfigManager.Instance.Config.availableModules;
        if (modules == null || modules.Length == 0)
        {
            Debug.LogWarning("No modules found in config.");
            return;
        }
        
        foreach (string moduleId in modules)
        {
            GameObject card = Instantiate(moduleCardPrefab, moduleListContainer);
            
            // Setup Text
            TMP_Text btnText = card.GetComponentInChildren<TMP_Text>();
            if (btnText) 
            {
                btnText.text = FormatModuleName(moduleId);
            }
            
            // Setup Click
            Button btn = card.GetComponent<Button>();
            if (btn)
            {
                string id = moduleId; // Capture for lambda
                btn.onClick.AddListener(() => OnModuleClicked(id));
            }
        }
    }
    
    private string FormatModuleName(string moduleId)
    {
        // Simple formatting - replace underscores with spaces and uppercase
        return moduleId.Replace("_", " ").ToUpper();
    }
    
    private void OnModuleClicked(string moduleId)
    {
        Debug.Log($"Selected module: {moduleId}");
        
        // Show toast if UIManager exists
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"Loading {moduleId}...");
        }
        
        // TODO: Implement module loading when ModuleLoader is created
        // For now, just log the selection
        Debug.Log($"Module {moduleId} selected - implement loading logic here");
    }
    
    private void OnLogoutClicked()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.Logout();
        }
        else
        {
            Debug.LogWarning("AuthManager not found");
        }
    }
}