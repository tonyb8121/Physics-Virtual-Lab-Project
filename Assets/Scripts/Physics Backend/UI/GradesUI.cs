using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GradesUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField assignmentIdInput; // Teacher pastes ID or selects from dropdown
    [SerializeField] private Button fetchGradesButton;
    [SerializeField] private Transform resultsContainer; // Content of a ScrollView
    [SerializeField] private GameObject gradeRowPrefab;  // Prefab with 2 texts: Name, Score

    private void Start()
    {
        fetchGradesButton.onClick.AddListener(FetchGrades);
    }

    private async void FetchGrades()
    {
        // For MVP, teacher types Assignment ID or Title. 
        // In full version, we'd list their assignments in a dropdown first.
        string id = assignmentIdInput.text;

        var grades = await FirestoreHelper.GetAssignmentGrades(id);

        // Clear old list
        foreach (Transform child in resultsContainer) Destroy(child.gameObject);

        if (grades.Count == 0)
        {
            UIManager.Instance.ShowToast("No submissions yet.");
            return;
        }

        // Populate List
        foreach (var sub in grades)
        {
            GameObject row = Instantiate(gradeRowPrefab, resultsContainer);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();

            // Assuming Prefab has Text1 (Name) and Text2 (Score)
            if (texts.Length >= 2)
            {
                texts[0].text = sub.studentName;
                texts[1].text = $"{sub.score}/{sub.totalQuestions}";
            }
        }
    }
}