using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class StudentHistoryUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject historyRowPrefab; // The prefab using HistoryRowUI
    [SerializeField] private GameObject emptyStateObject; // Text saying "No history yet"

    private async void OnEnable()
    {
        // 1. Clear old list
        foreach(Transform child in container) Destroy(child.gameObject);
        
        if (emptyStateObject) emptyStateObject.SetActive(false);

        // 2. Check Login
        if (AuthManager.Instance.CurrentUser == null) return;

        // 3. Fetch Data
        var history = await FirestoreHelper.GetStudentSubmissionHistory(AuthManager.Instance.CurrentUser.UserId);

        // 4. Handle Empty
        if (history == null || history.Count == 0)
        {
            if (emptyStateObject) emptyStateObject.SetActive(true);
            return;
        }

        // 5. Populate List
        foreach(var sub in history)
        {
            GameObject row = Instantiate(historyRowPrefab, container);
            var rowScript = row.GetComponent<HistoryRowUI>();
            if (rowScript != null)
            {
                rowScript.Setup(sub);
            }
        }
    }
}