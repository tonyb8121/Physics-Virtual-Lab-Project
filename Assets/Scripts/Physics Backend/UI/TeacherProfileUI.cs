using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class TeacherProfileUI : MonoBehaviour
{
    [Header("Profile Header")]
    [SerializeField] private TMP_Text teacherNameText;
    [SerializeField] private TMP_Text schoolNameText;
    [SerializeField] private TMP_Text schoolCodeText;
    [SerializeField] private Button editProfileButton;
    [SerializeField] private Button logoutButton;

    [Header("Manage Assignments")]
    [SerializeField] private Transform assignmentListContainer;
    [SerializeField] private GameObject manageRowPrefab;
    [SerializeField] private Button refreshButton;

    [Header("Edit Date Popup")]
    [SerializeField] private GameObject editDatePopup;
    [SerializeField] private TMP_InputField newDateInput;
    [SerializeField] private TMP_InputField newTimeInput;
    [SerializeField] private Button saveDateButton;
    [SerializeField] private Button closePopupButton;

    [Header("Edit Profile Popup")]
    [SerializeField] private GameObject editProfilePopup;
    [SerializeField] private TMP_InputField editNameInput;
    [SerializeField] private TMP_InputField editNewPasswordInput;
    [SerializeField] private TMP_InputField editConfirmPasswordInput;
    [SerializeField] private Button saveProfileButton;
    [SerializeField] private Button cancelProfileButton;

    private string _selectedAssignmentId;

    private void Start()
    {
        // ✅ FIX: Add null checks for all buttons
        if (logoutButton) logoutButton.onClick.AddListener(() => AuthManager.Instance.Logout());
        if (refreshButton) refreshButton.onClick.AddListener(LoadAssignments);
        if (saveDateButton) saveDateButton.onClick.AddListener(OnSaveDate);
        if (closePopupButton) closePopupButton.onClick.AddListener(() => editDatePopup.SetActive(false));
        if (editProfileButton) editProfileButton.onClick.AddListener(OpenEditProfilePopup);
        if (saveProfileButton) saveProfileButton.onClick.AddListener(OnSaveProfile);
        if (cancelProfileButton) cancelProfileButton.onClick.AddListener(() => editProfilePopup.SetActive(false));

        LoadProfileInfo();
        LoadAssignments();
    }

    private async void LoadProfileInfo()
    {
        // ✅ FIX: Add comprehensive null checks
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[TeacherProfileUI] AuthManager.Instance is NULL!");
            return;
        }

        if (AuthManager.Instance.CurrentUser == null)
        {
            Debug.LogError("[TeacherProfileUI] CurrentUser is NULL!");
            return;
        }

        var user = AuthManager.Instance.CurrentUser;

        // ✅ FIX: Check if UI element exists before setting text
        if (teacherNameText != null)
        {
            teacherNameText.text = user.DisplayName ?? "Teacher";
        }
        else
        {
            Debug.LogWarning("[TeacherProfileUI] teacherNameText is not assigned in Inspector!");
        }

        var userData = await FirestoreHelper.GetUserData(user.UserId);

        if (schoolNameText != null)
        {
            if (userData != null && userData.ContainsKey("schoolName"))
            {
                schoolNameText.text = userData["schoolName"].ToString();
            }
            else
            {
                schoolNameText.text = "My Classroom";
            }
        }
    }

    private async void LoadAssignments()
    {
        if (assignmentListContainer == null)
        {
            Debug.LogWarning("[TeacherProfileUI] assignmentListContainer not assigned!");
            return;
        }

        foreach (Transform child in assignmentListContainer) Destroy(child.gameObject);

        if (AuthManager.Instance == null || AuthManager.Instance.CurrentUser == null)
        {
            Debug.LogError("[TeacherProfileUI] Cannot load assignments - not logged in!");
            return;
        }

        var assignments = await FirestoreHelper.GetAssignmentsByTeacher(AuthManager.Instance.CurrentUser.UserId);

        if (assignments.Count == 0)
        {
            return;
        }

        foreach (var asn in assignments)
        {
            if (manageRowPrefab == null)
            {
                Debug.LogError("[TeacherProfileUI] manageRowPrefab not assigned!");
                break;
            }

            GameObject row = Instantiate(manageRowPrefab, assignmentListContainer);

            TMP_Text infoText = row.transform.Find("InfoText")?.GetComponent<TMP_Text>();
            if (infoText != null)
            {
                DateTime dueDate = new DateTime(asn.dueDate);
                infoText.text = $"{asn.title} ({asn.classId})\nDue: {dueDate:MMM dd, HH:mm}";
            }

            Button editBtn = row.transform.Find("EditButton")?.GetComponent<Button>();
            Button exportBtn = row.transform.Find("ExportButton")?.GetComponent<Button>();

            if (editBtn) editBtn.onClick.AddListener(() => OpenEditPopup(asn.id));
            if (exportBtn) exportBtn.onClick.AddListener(() => ExportGrades(asn));
        }
    }

    // ========== EDIT PROFILE METHODS ==========

    private void OpenEditProfilePopup()
    {
        if (AuthManager.Instance == null || AuthManager.Instance.CurrentUser == null)
        {
            UIManager.Instance.ShowToast("❌ Error: Not logged in!");
            return;
        }

        var user = AuthManager.Instance.CurrentUser;

        if (editNameInput) editNameInput.text = user.DisplayName;
        if (editNewPasswordInput) editNewPasswordInput.text = "";
        if (editConfirmPasswordInput) editConfirmPasswordInput.text = "";

        if (editProfilePopup) editProfilePopup.SetActive(true);
    }

    private async void OnSaveProfile()
    {
        string newName = editNameInput != null ? editNameInput.text.Trim() : "";
        string newPassword = editNewPasswordInput != null ? editNewPasswordInput.text.Trim() : "";
        string confirmPassword = editConfirmPasswordInput != null ? editConfirmPasswordInput.text.Trim() : "";

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
        if (saveProfileButton) saveProfileButton.interactable = false;

        string result = await AuthManager.Instance.UpdateProfileAsync(newName, newPassword);

        if (result == "Success")
        {
            UIManager.Instance.ShowToast("✅ Profile Updated!");
            if (teacherNameText) teacherNameText.text = newName;
            if (editProfilePopup) editProfilePopup.SetActive(false);
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

        if (saveProfileButton) saveProfileButton.interactable = true;
    }

    // ========== EDIT DATE/TIME METHODS ==========

    private void OpenEditPopup(string assignmentId)
    {
        _selectedAssignmentId = assignmentId;

        if (newDateInput) newDateInput.text = "1";
        if (newTimeInput) newTimeInput.text = "23:59";
        if (editDatePopup) editDatePopup.SetActive(true);
    }

    private async void OnSaveDate()
    {
        if (newDateInput == null)
        {
            UIManager.Instance.ShowToast("❌ Error: Date input not assigned!");
            return;
        }

        if (!int.TryParse(newDateInput.text, out int days) || days < 0)
        {
            UIManager.Instance.ShowToast("❌ Enter valid number of days (e.g., 1, 2, 7)");
            return;
        }

        DateTime futureDate = DateTime.Now.AddDays(days);

        string timeStr = newTimeInput != null ? newTimeInput.text.Trim() : "23:59";

        if (!TryParseTime(timeStr, out int hour, out int minute))
        {
            UIManager.Instance.ShowToast("❌ Invalid time format. Use HH:mm (e.g., 17:00, 23:59)");
            return;
        }

        DateTime finalDeadline = new DateTime(
            futureDate.Year,
            futureDate.Month,
            futureDate.Day,
            hour,
            minute,
            0
        );

        if (finalDeadline <= DateTime.Now)
        {
            UIManager.Instance.ShowToast("❌ Deadline must be in the future!");
            return;
        }

        UIManager.Instance.ShowToast("Updating deadline...");
        if (saveDateButton) saveDateButton.interactable = false;

        bool success = await FirestoreHelper.UpdateAssignmentDueDate(_selectedAssignmentId, finalDeadline.Ticks);

        if (success)
        {
            UIManager.Instance.ShowToast($"✅ Updated: Due {finalDeadline:MMM dd, yyyy HH:mm}");
            if (editDatePopup) editDatePopup.SetActive(false);
            LoadAssignments();
        }
        else
        {
            UIManager.Instance.ShowToast("❌ Update failed. Please try again.");
        }

        if (saveDateButton) saveDateButton.interactable = true;
    }

    private bool TryParseTime(string input, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            hour = 23;
            minute = 59;
            return true;
        }

        input = input.Trim().ToUpper();

        if (input.Contains(":"))
        {
            string[] parts = input.Split(':');
            if (parts.Length != 2) return false;

            string hourPart = parts[0].Trim();
            string minutePart = parts[1].Trim();

            bool isPM = minutePart.Contains("PM");
            bool isAM = minutePart.Contains("AM");
            minutePart = minutePart.Replace("PM", "").Replace("AM", "").Trim();

            if (!int.TryParse(hourPart, out hour)) return false;
            if (!int.TryParse(minutePart, out minute)) return false;

            if (isPM && hour < 12) hour += 12;
            if (isAM && hour == 12) hour = 0;

            return IsValidTime(hour, minute);
        }

        if (Regex.IsMatch(input, @"^\d{3,4}$"))
        {
            if (input.Length == 3)
            {
                hour = int.Parse(input.Substring(0, 1));
                minute = int.Parse(input.Substring(1, 2));
            }
            else if (input.Length == 4)
            {
                hour = int.Parse(input.Substring(0, 2));
                minute = int.Parse(input.Substring(2, 2));
            }

            return IsValidTime(hour, minute);
        }

        if (int.TryParse(input, out hour))
        {
            minute = 0;
            return IsValidTime(hour, minute);
        }

        var match = Regex.Match(input, @"^(\d{1,2})\s*(AM|PM)$");
        if (match.Success)
        {
            hour = int.Parse(match.Groups[1].Value);
            minute = 0;
            bool isPM = match.Groups[2].Value == "PM";

            if (isPM && hour < 12) hour += 12;
            if (!isPM && hour == 12) hour = 0;

            return IsValidTime(hour, minute);
        }

        return false;
    }

    private bool IsValidTime(int hour, int minute)
    {
        return hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59;
    }

    // ========== EXPORT METHODS ==========

    private async void ExportGrades(AssignmentData asn)
    {
        UIManager.Instance.ShowToast("Generating CSV...");
        var submissions = await FirestoreHelper.GetSubmissionsByAssignment(asn.id);
        string csvContent = GradeExporter.GenerateCSV(submissions, asn.title);
        string fileName = $"Grades_{asn.title}_{DateTime.Now:MMdd}.csv";
        GradeExporter.SaveToFile(csvContent, fileName);
    }
}