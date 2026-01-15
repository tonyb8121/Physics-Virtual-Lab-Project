using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

/// <summary>
/// Professional Login UI - Handles authentication flow
/// Routing is delegated to AuthManager for correct dashboard selection (MainScene or TeacherDashboard).
/// </summary>
public class ProfessionalLoginUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject landingPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject roleSelectionPanel;
    [SerializeField] private GameObject studentSignupPanel;
    [SerializeField] private GameObject teacherSignupPanel;
    [SerializeField] private GameObject forgotPasswordPanel;

    [Header("Login Inputs")]
    [SerializeField] private TMP_InputField loginEmail;
    [SerializeField] private TMP_InputField loginPass;
    [SerializeField] private TMP_Text loginError;

    [Header("Student Signup Inputs")]
    [SerializeField] private TMP_InputField studentName;
    [SerializeField] private TMP_InputField studentEmail;
    [SerializeField] private TMP_InputField studentPass;
    [SerializeField] private TMP_InputField studentConfirmPass;
    [SerializeField] private TMP_Text studentError;

    [Header("Teacher Signup Inputs")]
    [SerializeField] private TMP_InputField teacherName;
    [SerializeField] private TMP_InputField teacherEmail;
    [SerializeField] private TMP_InputField teacherSchool;
    [SerializeField] private TMP_InputField teacherPass;
    [SerializeField] private TMP_InputField teacherConfirmPass;
    [SerializeField] private TMP_Text teacherError;

    [Header("Forgot Password")]
    [SerializeField] private TMP_InputField forgotEmail;
    [SerializeField] private TMP_Text forgotStatus;
    
    [Header("Back Button (Optional)")]
    [SerializeField] private Button backToHomeButton;

    private void Start()
    {
        ShowPanel(landingPanel);
        
        // Setup back to home button if exists
        if (backToHomeButton != null)
        {
            backToHomeButton.onClick.AddListener(BackToHome);
        }
    }

    // --- NAVIGATION LOGIC ---
    public void GoToLogin() => ShowPanel(loginPanel);
    public void GoToRoleSelect() => ShowPanel(roleSelectionPanel);
    public void GoToStudentSignup() => ShowPanel(studentSignupPanel);
    public void GoToTeacherSignup() => ShowPanel(teacherSignupPanel);
    public void GoToForgotPass() => ShowPanel(forgotPasswordPanel);
    public void BackToLanding() => ShowPanel(landingPanel);
    
    /// <summary>
    /// Back button - Returns to Home scene
    /// </summary>
    public void BackToHome()
    {
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene("Home");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
        }
    }

    private void ShowPanel(GameObject panelToShow)
    {
        landingPanel.SetActive(false);
        loginPanel.SetActive(false);
        roleSelectionPanel.SetActive(false);
        studentSignupPanel.SetActive(false);
        teacherSignupPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);

        panelToShow.SetActive(true);
        ClearErrors();
    }

    private void ClearErrors()
    {
        if (loginError) loginError.text = "";
        if (studentError) studentError.text = "";
        if (teacherError) teacherError.text = "";
        if (forgotStatus) forgotStatus.text = "";
    }

    // --- EMAIL VALIDATION ---
    /// <summary>
    /// Validates email format using regex
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        // Email validation regex pattern
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    // --- ACTION LOGIC ---

    public async void SubmitLogin()
    {
        if(string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPass.text)) {
            loginError.text = "Fields required."; 
            return;
        }

        // Validate email format
        if (!IsValidEmail(loginEmail.text)) {
            loginError.text = "Invalid email format."; 
            return;
        }

        // AuthManager handles logging in AND routing on success.
        bool success = await AuthManager.Instance.LoginAsync(loginEmail.text, loginPass.text);
        
        if (!success) 
        {
            // Only update local UI error message if login failed
            loginError.text = "Invalid credentials.";
        }
    }

    public async void SubmitStudentSignup()
    {
        if(!ValidateSignup(studentName, studentEmail, studentPass, studentConfirmPass, studentError)) 
            return;

        // AuthManager handles signup AND routing on success.
        await AuthManager.Instance.SignupAsync(
            studentEmail.text, 
            studentPass.text, 
            studentName.text, 
            "student"
        );
    }

    public async void SubmitTeacherSignup()
    {
        if(!ValidateSignup(teacherName, teacherEmail, teacherPass, teacherConfirmPass, teacherError)) 
            return;
        
        if(string.IsNullOrEmpty(teacherSchool.text)) {
            teacherError.text = "School Name required."; 
            return;
        }

        // AuthManager handles signup AND routing on success.
        await AuthManager.Instance.SignupAsync(
            teacherEmail.text, 
            teacherPass.text, 
            teacherName.text, 
            "teacher", 
            teacherSchool.text
        );
    }

    public async void SubmitForgotPassword()
    {
        if(string.IsNullOrEmpty(forgotEmail.text)) {
            forgotStatus.text = "Enter email."; 
            return;
        }
        
        // Validate email format
        if (!IsValidEmail(forgotEmail.text)) {
            forgotStatus.text = "Invalid email format."; 
            return;
        }
        
        forgotStatus.text = "Sending...";
        bool sent = await AuthManager.Instance.ResetPasswordAsync(forgotEmail.text);
        
        if (sent) 
        {
            forgotStatus.text = "Reset link sent! Check email.";
        }
        else 
        {
            forgotStatus.text = "Error sending reset email.";
        }
    }

    // --- HELPER ---
    private bool ValidateSignup(TMP_InputField name, TMP_InputField email, 
                                TMP_InputField pass, TMP_InputField confirm, TMP_Text error)
    {
        if(string.IsNullOrEmpty(name.text) || string.IsNullOrEmpty(email.text)) {
            error.text = "Fill all fields."; 
            return false;
        }
        
        // Validate email format
        if (!IsValidEmail(email.text)) {
            error.text = "Invalid email format."; 
            return false;
        }
        
        if(pass.text.Length < 6) {
            error.text = "Password must be 6+ chars."; 
            return false;
        }
        if(pass.text != confirm.text) {
            error.text = "Passwords do not match."; 
            return false;
        }
        return true;
    }
}