using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TeacherGradingUI : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private TMP_Dropdown assignmentDropdown;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button deleteAssignmentButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Results List")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject gradeRowPrefab; // Prefab with: NameText, ScoreText, DateText
    [SerializeField] private GameObject emptyStateObject;
    
    // START: Add Analytics Fields
    [Header("Analytics")]
    [SerializeField] private GameObject analyticsPanel;
    [SerializeField] private TMP_Text analyticsText; // "Avg: 80%, Submitted: 10/20"
    [SerializeField] private Button btnShowAnalytics;
    // CRITICAL UPDATE: Add reference for Close button
    [SerializeField] private Button btnCloseAnalytics;
    // END: Add Analytics Fields

    private List<AssignmentData> _myAssignments;
    private AssignmentData _selectedAssignment;

    private void Start()
    {
        // Null checks for buttons are good practice but not the source of this error
        if (refreshButton != null) refreshButton.onClick.AddListener(LoadAssignments);
        if (deleteAssignmentButton != null) deleteAssignmentButton.onClick.AddListener(OnDeleteClicked);
        if (assignmentDropdown != null) assignmentDropdown.onValueChanged.AddListener(OnAssignmentSelected);
        
        // START: Add Analytics Button Listeners
        if (btnShowAnalytics != null) btnShowAnalytics.onClick.AddListener(ShowAnalytics);
        
        // CRITICAL UPDATE: Add listener for the Close button
        if (btnCloseAnalytics != null)
        {
            btnCloseAnalytics.onClick.AddListener(CloseAnalytics);
        }
        // END: Add Analytics Button Listeners
        
        // We call LoadAssignments(), which now has the necessary null checks.
        LoadAssignments();
        
        // Hide analytics panel on start
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
        }
    }

    private async void LoadAssignments()
    {
        // ----------------------------------------------------
        // ✅ FIX APPLIED HERE (Around original line 63)
        // Check if AuthManager and CurrentUser are initialized
        // ----------------------------------------------------
        if (AuthManager.Instance == null || AuthManager.Instance.CurrentUser == null)
        {
            statusText.text = "Error: User not authenticated or AuthManager not ready.";
            // Optionally, you could try to re-wait for the authentication state here.
            Debug.LogError("TeacherGradingUI: Cannot load assignments. AuthManager.Instance or CurrentUser is null.");
            return; 
        }

        statusText.text = "Loading Assignments...";
        assignmentDropdown.ClearOptions();

        // This line is now safe because of the check above
        string teacherId = AuthManager.Instance.CurrentUser.UserId; 
        
        _myAssignments = await FirestoreHelper.GetAssignmentsByTeacher(teacherId);

        if (_myAssignments == null || _myAssignments.Count == 0)
        {
            statusText.text = "No Assignments Created.";
            // Ensure captionText doesn't cause a NRE if dropdown is missing/misconfigured
            if (assignmentDropdown.captionText != null) assignmentDropdown.captionText.text = "None"; 
            return;
        }

        List<string> options = _myAssignments.Select(a => $"{a.title} ({a.classId})").ToList();
        assignmentDropdown.AddOptions(options);

        // Auto-select first
        OnAssignmentSelected(0);
        statusText.text = "Ready.";
    }

    private async void OnAssignmentSelected(int index)
    {
        if (_myAssignments == null || _myAssignments.Count <= index) return;

        _selectedAssignment = _myAssignments[index];
        deleteAssignmentButton.interactable = true;
        
        // Hide analytics panel on new assignment selection
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
        }

        // Fetch submissions for this assignment
        // Assuming FirestoreHelper.GetAssignmentGrades returns a List<SubmissionData> (or similar)
        var submissions = await FirestoreHelper.GetAssignmentGrades(_selectedAssignment.id);

        // Clear UI
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        // Note: Checking for submissions being null is good practice too, though unlikely if FirestoreHelper is robust
        if (submissions == null || submissions.Count == 0)
        {
            if (emptyStateObject != null) emptyStateObject.SetActive(true);
            return;
        }

        if (emptyStateObject != null) emptyStateObject.SetActive(false);

        // Populate UI
        foreach (var sub in submissions)
        {
            GameObject row = Instantiate(gradeRowPrefab, contentContainer);

            var texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = sub.studentName;
                
                float percentage = (sub.totalQuestions > 0) 
                    ? ((float)sub.score / sub.totalQuestions) * 100f 
                    : 0f;
                
                texts[1].text = $"{sub.score}/{sub.totalQuestions} ({percentage:F0}%)";
                
                // Assuming 'submittedAt' is a long (timestamp)
                System.DateTime date = new System.DateTime(sub.submittedAt);
                texts[2].text = date.ToString("MMM dd");
            }
        }
    }

    // START: Add ShowAnalytics method
    private async void ShowAnalytics()
    {
        if (_selectedAssignment == null) return;

        // CRITICAL FIX: Ensure UIManager.Instance is not null before calling it
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast("Calculating stats...");
        }

        var stats = await FirestoreHelper.GetAssignmentAnalytics(_selectedAssignment.id, _selectedAssignment.classId);

        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(true);
        }
        
        if (analyticsText != null)
        {
            analyticsText.text = $"<b>Class Performance</b>\n\n" +
                                 $"Average Score: {stats.averageScore:F1}%\n" +
                                 $"Completion: {stats.completionRate:F1}%\n" +
                                 $"Submitted: {stats.totalSubmissions}/{stats.totalStudents}";
        }
    }
    // END: Add ShowAnalytics method

    // CRITICAL UPDATE: Add CloseAnalytics method
    private void CloseAnalytics()
    {
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
        }
    }

    private async void OnDeleteClicked()
    {
        if (_selectedAssignment == null) return;

        statusText.text = "Deleting...";
        deleteAssignmentButton.interactable = false;

        bool success = await FirestoreHelper.DeleteAssignment(_selectedAssignment.id);

        if (success)
        {
            // CRITICAL FIX: Ensure UIManager.Instance is not null before calling it
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("Assignment Deleted.");
            }
            LoadAssignments();
        }
        else
        {
            statusText.text = "Delete Failed.";
            deleteAssignmentButton.interactable = true;
        }
    }
}