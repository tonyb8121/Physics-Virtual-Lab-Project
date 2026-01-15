using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Scroll View Setup")]
    [SerializeField] private Transform contentContainer; // The "Content" object inside Viewport
    [SerializeField] private GameObject rankRowPrefab;   // Prefab with 3 TMP_Text children
    
    [Header("Optional")]
    [SerializeField] private TMP_Text loadingText;       // "Loading..." message
    [SerializeField] private GameObject emptyStatePanel; // "No data yet" message
    
    private bool isLoading = false;

    private void OnEnable()
    {
        RefreshLeaderboard();
    }

    public async void RefreshLeaderboard()
    {
        if (isLoading) return; // Prevent multiple simultaneous loads
        isLoading = true;

        // Show loading state
        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (emptyStatePanel != null) emptyStatePanel.SetActive(false);

        // Clear existing rows
        ClearLeaderboard();

        try
        {
            // Fetch from Firestore
            var topStudents = await FirestoreHelper.GetLeaderboard(10); // Top 10

            // Hide loading
            if (loadingText != null) loadingText.gameObject.SetActive(false);

            // Check if we have data
            if (topStudents == null || topStudents.Count == 0)
            {
                if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
                Debug.Log("No leaderboard data found.");
                return;
            }

            // Populate rows
            int rank = 1;
            foreach (var studentData in topStudents)
            {
                CreateRankRow(rank, studentData);
                rank++;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading leaderboard: {e.Message}");
            
            if (loadingText != null)
            {
                loadingText.text = "Failed to load leaderboard";
                loadingText.color = Color.red;
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ClearLeaderboard()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateRankRow(int rank, Dictionary<string, object> studentData)
    {
        // Instantiate row
        GameObject row = Instantiate(rankRowPrefab, contentContainer);
        
        // Get all TMP_Text components
        var texts = row.GetComponentsInChildren<TMP_Text>();

        if (texts.Length < 3)
        {
            Debug.LogError("RankRow prefab must have at least 3 TMP_Text children!");
            Destroy(row);
            return;
        }

        // Extract data safely
        string displayName = studentData.ContainsKey("displayName") 
            ? studentData["displayName"].ToString() 
            : "Unknown User";
        
        string points = "0";
        if (studentData.ContainsKey("totalPoints"))
        {
            points = studentData["totalPoints"].ToString();
        }
        else if (studentData.ContainsKey("points")) // Fallback field name
        {
            points = studentData["points"].ToString();
        }

        // Assign text values
        texts[0].text = $"#{rank}";
        texts[1].text = displayName;
        texts[2].text = $"{points} pts";

        // Optional: Color coding for top ranks
        if (rank == 1)
        {
            texts[0].color = new Color(1f, 0.84f, 0f); // Gold
        }
        else if (rank == 2)
        {
            texts[0].color = new Color(0.75f, 0.75f, 0.75f); // Silver
        }
        else if (rank == 3)
        {
            texts[0].color = new Color(0.8f, 0.5f, 0.2f); // Bronze
        }
    }

    // Public method to be called from button or other scripts
    public void OnRefreshButtonClicked()
    {
        RefreshLeaderboard();
    }
}