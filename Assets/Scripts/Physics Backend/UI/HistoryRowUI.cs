using UnityEngine;
using TMPro;
using System;

public class HistoryRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text assignmentTitleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text statusText; // "Passed" or "Failed"

    public void Setup(StudentSubmission submission)
    {
        // 1. Set Title (We assume assignmentId helps identify it, or we store title in submission)
        // For MVP, we display Assignment ID or fetch title if cached. 
        // Ideally, StudentSubmission should store the 'AssignmentTitle' snapshot.
        assignmentTitleText.text = $"Assignment: {submission.assignmentId}"; 

        // 2. Set Score
        scoreText.text = $"{submission.score}/{submission.totalQuestions} ({submission.percentage:F0}%)";
        
        // 3. Set Date
        DateTime date = new DateTime(submission.submittedAt);
        dateText.text = date.ToString("MMM dd, HH:mm");

        // 4. Set Status Color
        if (submission.percentage >= 50)
        {
            statusText.text = "PASSED";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "FAILED";
            statusText.color = Color.red;
        }
    }
}