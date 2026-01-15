using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AssignmentCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dueText;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text statusText;

    public void Setup(AssignmentData data, bool isSubmitted, Action<AssignmentData> onStart)
    {
        titleText.text = data.title;
        
        // --- 1. PRECISE DATE FORMAT ---
        DateTime dueDate = new DateTime(data.dueDate);
        
        // ✅ NEW: Show time limit badge
        string timeLimitStr = data.timeLimitMinutes > 0 
            ? $"⏱️ {data.timeLimitMinutes}m" 
            : "Practice";
        
        dueText.text = $"Due: {dueDate:MMM dd, HH:mm} | {timeLimitStr}";
        
        // --- 2. STATUS & LOCKING LOGIC ---
        TMP_Text btnLabel = startButton.GetComponentInChildren<TMP_Text>();
        bool isOverdue = DateTime.Now > dueDate;
        
        if (isSubmitted)
        {
            statusText.text = "COMPLETED";
            statusText.color = Color.green;
            btnLabel.text = "DONE";
            startButton.interactable = false;
        }
        else if (isOverdue)
        {
            statusText.text = "MISSED";
            statusText.color = Color.red;
            dueText.color = Color.red;
            btnLabel.text = "CLOSED";
            startButton.interactable = false;
        }
        else
        {
            statusText.text = "PENDING";
            statusText.color = Color.yellow;
            dueText.color = Color.white;
            
            // ✅ Show warning for timed exams
            if (data.timeLimitMinutes > 0)
            {
                btnLabel.text = "START EXAM";
                statusText.text = "⚠️ TIMED";
                statusText.color = new Color(1f, 0.5f, 0f); // Orange
            }
            else
            {
                btnLabel.text = "START";
            }
            
            startButton.interactable = true;
        }
        
        // --- 3. CLICK LISTENER ---
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => {
            if (!isSubmitted && !isOverdue)
            {
                onStart(data);
            }
        });
    }
}