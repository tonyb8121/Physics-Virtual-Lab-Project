using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField firstNameInput; // NEW
    [SerializeField] private TMP_InputField lastNameInput;  // NEW
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    
    [Header("Buttons")]
    [SerializeField] private Button actionButton;          
    [SerializeField] private Button toggleModeButton;      
    
    [Header("Role Selection")]
    [SerializeField] private Toggle teacherToggle;         
    [SerializeField] private TMP_Text teacherLabel;        

    [Header("Feedback")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text errorText;

    private bool _isSignup = false;

    private void Start()
    {
        actionButton.onClick.AddListener(OnSubmit);
        toggleModeButton.onClick.AddListener(OnToggleMode);
        
        emailInput.onValueChanged.AddListener((s) => errorText.text = "");
        UpdateUI();
    }

    private void OnToggleMode()
    {
        _isSignup = !_isSignup;
        UpdateUI();
    }

    private void UpdateUI()
    {
        titleText.text = _isSignup ? "Create Account" : "Welcome Back";
        actionButton.GetComponentInChildren<TMP_Text>().text = _isSignup ? "SIGN UP" : "LOGIN";
        toggleModeButton.GetComponentInChildren<TMP_Text>().text = _isSignup ? "Have an account? Login" : "New here? Create Account";

        // Toggle Visibility of Sign-Up Fields
        firstNameInput.gameObject.SetActive(_isSignup);
        lastNameInput.gameObject.SetActive(_isSignup);
        teacherToggle.gameObject.SetActive(_isSignup);
        if(teacherLabel) teacherLabel.gameObject.SetActive(_isSignup);

        errorText.text = "";
    }

    private async void OnSubmit()
    {
        string email = emailInput.text.Trim();
        string pass = passwordInput.text.Trim();
        string first = firstNameInput.text.Trim();
        string last = lastNameInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) {
            errorText.text = "Email/Password required."; return;
        }

        actionButton.interactable = false;

        bool success = false;
        if (_isSignup)
        {
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(last)) { 
                errorText.text = "Names required."; 
                actionButton.interactable = true;
                return; 
            }

            // Combine names for Display Name
            string fullName = $"{first} {last}";
            string role = teacherToggle.isOn ? "teacher" : "student";
            
            success = await AuthManager.Instance.SignupAsync(email, pass, fullName, role);
        }
        else
        {
            success = await AuthManager.Instance.LoginAsync(email, pass);
        }

        actionButton.interactable = true;

        if (success)
        {
            string role = AuthManager.Instance.UserRole;
            if (role == "teacher") SceneFlowManager.Instance.LoadScene("TeacherDashboardScene");
            else SceneFlowManager.Instance.LoadScene("MainScene");
        }
        else
        {
            errorText.text = "Authentication Failed.";
        }
    }
}