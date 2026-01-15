using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class StudentProfileController : MonoBehaviour
{
    // Ensure all necessary supporting classes like AuthManager, UIManager, FirestoreHelper,
    // FirebaseManager, and the Submission class are available in your project.

    [Header("Display Stats")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text emailText; 
    [SerializeField] private TMP_Text classText;
    [SerializeField] private TMP_Text pointsText;

    [Header("Join Class Form")]
    [SerializeField] private TMP_InputField classCodeInput;
    [SerializeField] private TMP_InputField admissionNumberInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Profile Actions")]
    [SerializeField] private Button editProfileButton;
    [SerializeField] private Button viewHistoryButton; 
    [SerializeField] private Button logoutButton;

    [Header("Edit Popup")]
    [SerializeField] private GameObject editPopup;
    [SerializeField] private TMP_InputField editNameInput;
    [SerializeField] private TMP_InputField editNewPasswordInput;
    [SerializeField] private TMP_InputField editConfirmPasswordInput;
    [SerializeField] private Button saveProfileButton;
    [SerializeField] private Button cancelProfileButton;

    [Header("History Popup")]
    [SerializeField] private GameObject historyPopup;
    [SerializeField] private Transform historyContainer; // Content inside ScrollView
    [SerializeField] private GameObject historyRowPrefab; // Prefab showing: Assignment title, Score, Date, Grade
    [SerializeField] private Button closeHistoryButton;
    [SerializeField] private TMP_Text historyEmptyText; // Shows "No submissions yet"

    private async void OnEnable()
    {
        var user = AuthManager.Instance.CurrentUser;
        if (user == null) return;

        // Display current info
        nameText.text = user.DisplayName;
        emailText.text = user.Email;

        // Load Firestore Data
        var userData = await FirestoreHelper.GetUserData(user.UserId);
        if (userData != null)
        {
            string cls = userData.ContainsKey("classId") ? userData["classId"].ToString() : "None";
            string adm = userData.ContainsKey("admissionNumber") ? userData["admissionNumber"].ToString() : "---";
            classText.text = $"Class: {cls} | Adm: {adm}";
            string pts = userData.ContainsKey("totalPoints") ? userData["totalPoints"].ToString() : "0";
            pointsText.text = $"{pts} XP";
        }

        // Setup Buttons
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(OnJoinClicked);
        
        logoutButton.onClick.RemoveAllListeners();
        logoutButton.onClick.AddListener(() => AuthManager.Instance.Logout());

        // Edit Profile Listeners
        editProfileButton.onClick.RemoveAllListeners();
        editProfileButton.onClick.AddListener(OpenEditPopup);
        
        saveProfileButton.onClick.RemoveAllListeners();
        saveProfileButton.onClick.AddListener(OnSaveProfile);
        
        cancelProfileButton.onClick.RemoveAllListeners();
        cancelProfileButton.onClick.AddListener(() => editPopup.SetActive(false));

        // History Button Listeners (Now integrated from StudentHistoryUI purpose)
        if (viewHistoryButton)
        {
            viewHistoryButton.onClick.RemoveAllListeners();
            viewHistoryButton.onClick.AddListener(OpenHistoryPopup);
        }

        if (closeHistoryButton)
        {
            closeHistoryButton.onClick.RemoveAllListeners();
            closeHistoryButton.onClick.AddListener(CloseHistoryPopup);
        }

        // Hide popups by default
        if (editPopup) editPopup.SetActive(false);
        if (historyPopup) historyPopup.SetActive(false);
    }

    private void OpenEditPopup()
    {
        var user = AuthManager.Instance.CurrentUser;
        editNameInput.text = user.DisplayName;
        
        editNewPasswordInput.text = "";
        editConfirmPasswordInput.text = "";
        
        editPopup.SetActive(true);
    }

    private async void OnSaveProfile()
    {
        string newName = editNameInput.text.Trim();
        string newPassword = editNewPasswordInput.text.Trim();
        string confirmPassword = editConfirmPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            UIManager.Instance.ShowToast("❌ Name cannot be empty.");
            return;
        }

        if (!string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword))
        {
            if (newPassword.Length < 6)
            {
                UIManager.Instance.ShowToast("❌ Password must be at least 6 characters.");
                return;
            }

            if (newPassword != confirmPassword)
            {
                UIManager.Instance.ShowToast("❌ Passwords do not match!");
                return;
            }
        }

        UIManager.Instance.ShowToast("Updating Profile...");
        saveProfileButton.interactable = false;

        string result = await AuthManager.Instance.UpdateProfileAsync(newName, newPassword);

        if (result == "Success")
        {
            UIManager.Instance.ShowToast("✅ Profile Updated!");
            nameText.text = newName;
            editPopup.SetActive(false);
        }
        else if (result == "PasswordChanged")
        {
            UIManager.Instance.ShowToast("✅ Password Changed! Please log in again.");
            await Task.Delay(2000);
            AuthManager.Instance.Logout();
        }
        else if (result.Contains("recent login"))
        {
            UIManager.Instance.ShowToast("⚠️ " + result);
            await Task.Delay(3000);
            AuthManager.Instance.Logout();
        }
        else
        {
            UIManager.Instance.ShowToast(result); 
        }

        saveProfileButton.interactable = true;
    }

    private async void OnJoinClicked()
    {
        string code = classCodeInput.text.Trim();
        string adm = admissionNumberInput.text.Trim();
        
        if (feedbackText != null) 
        {
            feedbackText.text = "Verifying...";
            feedbackText.color = Color.yellow;
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(adm)) 
        {
            if (feedbackText != null) 
            { 
                feedbackText.text = "Fill all fields."; 
                feedbackText.color = Color.red; 
            }
            return;
        }
        
        joinButton.interactable = false;

        bool isValid = await FirestoreHelper.VerifyClassCode(code);
        if(!isValid) 
        {
            if (feedbackText != null) 
            { 
                feedbackText.text = "❌ Class not found."; 
                feedbackText.color = Color.red; 
            }
            joinButton.interactable = true;
            return;
        }

        bool success = await FirestoreHelper.JoinClass(AuthManager.Instance.CurrentUser.UserId, code, adm);
        if (success) 
        {
            if (feedbackText != null) 
            { 
                feedbackText.text = "✅ Joined!"; 
                feedbackText.color = Color.green; 
            }
            classText.text = $"Class: {code} | Adm: {adm}";
        } 
        else 
        {
            if (feedbackText != null) 
            { 
                feedbackText.text = "❌ Error joining class."; 
                feedbackText.color = Color.red; 
            }
        }
        
        joinButton.interactable = true;
    }

    // ========== HISTORY POPUP METHODS (Integrated from StudentHistoryUI) ==========

    private async void OpenHistoryPopup()
    {
        if (historyPopup == null)
        {
            Debug.LogWarning("[StudentProfile] History popup not assigned!");
            return;
        }

        historyPopup.SetActive(true);
        UIManager.Instance.ShowToast("Loading submission history...");

        // Ensure the data loads before the popup is fully displayed/interacted with
        await LoadSubmissionHistory();
    }

    private void CloseHistoryPopup()
    {
        if (historyPopup) historyPopup.SetActive(false);
    }

    private async Task LoadSubmissionHistory()
    {
        if (historyContainer == null || historyRowPrefab == null)
        {
            Debug.LogWarning("[StudentProfile] History container or prefab not assigned!");
            return;
        }

        // Clear old rows
        foreach (Transform child in historyContainer)
        {
            Destroy(child.gameObject);
        }

        // Get current user ID
        if (AuthManager.Instance?.CurrentUser == null)
        {
            Debug.LogError("[StudentProfile] Not logged in!");
            return;
        }

        string userId = AuthManager.Instance.CurrentUser.UserId;

        // Fetch submission history from Firestore
        var submissions = await FirestoreHelper.GetStudentSubmissionHistory(userId);

        // Handle Empty
        if (submissions == null || submissions.Count == 0)
        {
            if (historyEmptyText)
            {
                historyEmptyText.gameObject.SetActive(true);
                historyEmptyText.text = "No submissions yet.\nComplete assignments to see your history!";
            }
            return;
        }

        // Hide empty state
        if (historyEmptyText) historyEmptyText.gameObject.SetActive(false);

        // Populate List
        foreach (var submission in submissions)
        {
            // Note: HistoryRowUI is not used here, we populate the fields directly.
            // You may need to ensure your historyRowPrefab has the correct children names:
            // "AssignmentTitle", "Score", "Date", "Grade"

            GameObject row = Instantiate(historyRowPrefab, historyContainer);

            TMP_Text titleText = row.transform.Find("AssignmentTitle")?.GetComponent<TMP_Text>();
            TMP_Text scoreText = row.transform.Find("Score")?.GetComponent<TMP_Text>();
            TMP_Text dateText = row.transform.Find("Date")?.GetComponent<TMP_Text>();
            TMP_Text gradeText = row.transform.Find("Grade")?.GetComponent<TMP_Text>();

            // Get assignment title (we need to fetch it - this adds latency per row)
            // A better practice is to pre-fetch all needed titles, but this works for now.
            string assignmentTitle = await GetAssignmentTitle(submission.assignmentId);

            if (titleText)
            {
                titleText.text = assignmentTitle;
            }

            if (scoreText)
            {
                scoreText.text = $"{submission.score}/{submission.totalQuestions}";
            }

            if (dateText)
            {
                // Ensure the 'submittedAt' value in Submission is compatible (long ticks or similar)
                // Assuming submission.submittedAt is the number of Ticks/milliseconds since Unix Epoch.
                DateTime submittedDate = new DateTime(submission.submittedAt); 
                dateText.text = submittedDate.ToString("MMM dd, yyyy");
            }

            if (gradeText)
            {
                // Calculate grade
                string grade = GetLetterGrade(submission.percentage);
                gradeText.text = grade;

                // Color coding
                if (submission.percentage >= 90) gradeText.color = Color.blue; // Example: A
                else if (submission.percentage >= 80) gradeText.color = Color.green; // Example: B
                else if (submission.percentage >= 70) gradeText.color = Color.yellow; // Example: C/D
                else gradeText.color = Color.red; // Example: F
            }
        }
    }

    /// <summary>
    /// Fetches assignment title from Firestore by ID.
    /// </summary>
    private async Task<string> GetAssignmentTitle(string assignmentId)
    {
        try
        {
            // Assumes FirebaseManager.Instance.Firestore is properly initialized
            var assignmentDoc = await FirebaseManager.Instance.Firestore
                .Collection("assignments")
                .Document(assignmentId)
                .GetSnapshotAsync();

            if (assignmentDoc.Exists)
            {
                var data = assignmentDoc.ToDictionary();
                if (data.ContainsKey("title"))
                {
                    return data["title"].ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StudentProfile] Could not fetch assignment title: {e.Message}");
        }

        return "Unknown Assignment";
    }

    /// <summary>
    /// Converts percentage to letter grade.
    /// </summary>
    private string GetLetterGrade(float percentage)
    {
        if (percentage >= 90) return "A";
        if (percentage >= 80) return "B";
        if (percentage >= 70) return "C";
        if (percentage >= 60) return "D";
        return "F";
    }
}