using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

public class StudentAssignmentsUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject assignmentCardPrefab;
    [SerializeField] private TMP_Text emptyStateText;

    [Header("✅ NEW: Confirmation Dialog")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button btnConfirmYes;
    [SerializeField] private Button btnConfirmNo;

    private AssignmentData _pendingAssignment;

    private void Start()
    {
        if (confirmPanel) confirmPanel.SetActive(false);
        if (btnConfirmYes) btnConfirmYes.onClick.AddListener(OnConfirmStart);
        if (btnConfirmNo) btnConfirmNo.onClick.AddListener(() => confirmPanel.SetActive(false));
    }

    private void OnEnable()
    {
        RefreshAssignments();
    }

    private async void RefreshAssignments()
    {
        foreach (Transform child in container) Destroy(child.gameObject);
        if (emptyStateText) emptyStateText.gameObject.SetActive(false);

        // Get student's class and user ID
        string myClassId = "Form3A"; // Fallback default class ID (if no class is joined)
        string myUserId = null;

        if (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null)
        {
            myUserId = AuthManager.Instance.CurrentUser.UserId;
            
            // ✅ FIX: USE THE NEW VARIABLE FROM AUTHMANAGER
            if (!string.IsNullOrEmpty(AuthManager.Instance.UserClassId))
            {
                myClassId = AuthManager.Instance.UserClassId;
            }
        }
        
        Debug.Log($"[StudentAssignmentsUI] Fetching assignments for Class: {myClassId}");

        // Fetch assignments using the dynamic Class ID
        var assignments = await FirestoreHelper.GetAssignments(myClassId);

        if (this == null || gameObject == null || container == null) return;

        if (assignments.Count == 0)
        {
            if (emptyStateText) emptyStateText.gameObject.SetActive(true);
            return;
        }

        // ✅ NEW: Fetch submission history to check which are done
        List<StudentSubmission> mySubmissions = new List<StudentSubmission>();
        if (!string.IsNullOrEmpty(myUserId))
        {
            mySubmissions = await FirestoreHelper.GetStudentSubmissionHistory(myUserId);
        }

        // Create cards
        foreach (var data in assignments)
        {
            if (data == null) continue;

            // ✅ Check if this assignment is already submitted
            bool isDone = mySubmissions.Any(s => s.assignmentId == data.id);

            GameObject cardObject = Instantiate(assignmentCardPrefab, container);
            AssignmentCardUI cardUI = cardObject.GetComponent<AssignmentCardUI>();
            
            if (cardUI != null)
            {
                cardUI.Setup(data, isDone, OnCardClicked); // ✅ NEW: Use confirmation callback
            }
            else
            {
                Debug.LogError("❌ AssignmentCardUI missing on prefab!");
            }
        }
    }

     // ✅ NEW: Show confirmation before starting

    private void OnCardClicked(AssignmentData assignment)

    {

        _pendingAssignment = assignment;

        

        string timeStr = assignment.timeLimitMinutes > 0 

            ? $"{assignment.timeLimitMinutes} Minutes" 

            : "No Time Limit";

        

        string modeStr = assignment.timeLimitMinutes > 0 

            ? "<color=orange>⚠️ EXAM MODE</color>\n• No hints\n• No AI help\n• Timer runs continuously" 

            : "<color=cyan>Practice Mode</color>\n• Hints available\n• AI explanations\n• Timer resets per question";

        

        confirmText.text = $"<b>Start: {assignment.title}?</b>\n\n" +

                           $"{modeStr}\n\n" +

                           $"<b>Time Limit:</b> {timeStr}\n" +

                           $"<b>Questions:</b> {assignment.questions.Count}\n\n" +

                           $"<i>Once started, you cannot pause or retake.</i>";

        

        confirmPanel.SetActive(true);

    }


    private void OnConfirmStart()
    {
        confirmPanel.SetActive(false);
        if (_pendingAssignment != null)
        {
            StartAssignment(_pendingAssignment);
        }
    }

    private void StartAssignment(AssignmentData assignment)
    {
        Debug.Log($"[StudentAssignmentsUI] Starting: {assignment.title}");

        if (AssessmentManager.Instance == null)
        {
            Debug.LogError("AssessmentManager missing!");
            return;
        }

        // 1. Load assignment into manager
        AssessmentManager.Instance.StartAssignmentQuiz(assignment);
        
        // ✅ 2. INJECT TIME LIMIT (converts minutes to seconds)
        AssessmentManager.Instance.CurrentQuiz.globalTimeLimit = assignment.timeLimitMinutes * 60;

        // 3. Show quiz panel
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPanel("QuizPanel");
        }

        // 4. Start the quiz
        QuizUI quizUI = FindObjectOfType<QuizUI>();
        if (quizUI != null)
        {
            quizUI.StartQuiz(AssessmentManager.Instance.CurrentQuiz);
        }
        else
        {
            Debug.LogError("QuizUI not found!");
        }
    }
}