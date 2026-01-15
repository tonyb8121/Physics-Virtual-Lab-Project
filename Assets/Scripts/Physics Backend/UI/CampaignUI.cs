using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Assuming AuthManager and QuestionBank are in scope (e.g., in a default namespace)

public class CampaignUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject levelCardPrefab;

    private void OnEnable()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        Debug.Log("[CampaignUI] RefreshUI called");

        // 1. Clear existing cards
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. Get Data
        if (CampaignManager.Instance == null)
        {
            Debug.LogError("[CampaignUI] CampaignManager.Instance is NULL!");
            return;
        }

        var levels = CampaignManager.Instance.allLevels;

        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[CampaignUI] No levels found in CampaignManager!");
            return;
        }

        // 3. Loop through every level
        int cardCount = 0;
        foreach (var level in levels)
        {
            if (level == null) continue;

            // Spawn card
            GameObject card = Instantiate(levelCardPrefab, container);
            
            // Get Status
            bool isUnlocked = CampaignManager.Instance.IsLevelUnlocked(level.levelIndex);
            int bestScore = CampaignManager.Instance.GetBestScore(level.levelIndex);

            // Set Title
            TMP_Text title = card.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            if (title != null) title.text = $"Level {level.levelIndex}: {level.levelName}";

            // Set Score
            TMP_Text score = card.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            if (score != null)
            {
                score.text = $"Best Score: {bestScore}%";
                score.color = (bestScore >= 70) ? Color.green : Color.yellow;
            }

            // Handle Lock & Button
            GameObject lockIcon = card.transform.Find("LockIcon")?.gameObject;
            Button playBtn = card.transform.Find("Btn_Play")?.GetComponent<Button>();

            if (isUnlocked)
            {
                if (lockIcon != null) lockIcon.SetActive(false);
                
                if (playBtn != null)
                {
                    playBtn.gameObject.SetActive(true);
                    
                    // IMPORTANT: Clear previous listeners to avoid duplicates
                    playBtn.onClick.RemoveAllListeners();

                    // CRITICAL FIX: Capture the loop variable into a local variable
                    QuestionBank levelToStart = level; 

                    playBtn.onClick.AddListener(() => 
                    {
                        Debug.Log($"[CampaignUI] 🖱️ CLICKED Start Button for Level {levelToStart.levelIndex}");
                        StartLevel(levelToStart);
                    });
                }
            }
            else
            {
                if (lockIcon != null) lockIcon.SetActive(true);
                if (playBtn != null) playBtn.gameObject.SetActive(false);
            }

            cardCount++;
        }

        Debug.Log($"[CampaignUI] Created {cardCount} level cards");
    }

    private void StartLevel(QuestionBank level)
    {
        Debug.Log($"[CampaignUI] 🚀 Attempting to start Level {level.levelIndex}: {level.levelName}");

        // --- 🔒 NEW: LOGIN GATE LOGIC ---
        // If trying to access Level 2 or higher...
        if (level.levelIndex > 1) 
        {
            // Check if user is Offline (Not Logged In)
            // Note: We check AuthManager.Instance != null because it might not be initialized/exist
            if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[CampaignUI] 🛑 Blocked: Offline user trying to access Level 2+");
                
                // Show a dialog/toast explaining they need to register
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("🔒 Sign Up to unlock advanced levels!");
                    // Optional: Open the Auth Scene/Panel automatically
                    // SceneManager.LoadScene("AuthScene"); 
                }
                return; // STOP execution here
            }
        }
        // --------------------------------

        // 1. Check Level Data
        var questions = level.GetRandomBatch(10);
        if (questions.Count == 0)
        {
            Debug.LogError($"[CampaignUI] Level {level.levelIndex} has 0 questions! Check your QuestionBank asset.");
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("No questions in this level!");
            return;
        }

        // 2. Check AssessmentManager
        if (AssessmentManager.Instance == null)
        {
            Debug.LogError("[CampaignUI] CRITICAL: AssessmentManager.Instance is NULL.");
            return;
        }

        // 3. Setup Quiz Data
        AssessmentManager.Instance.StartCampaignLevel(level.levelIndex, questions);
        Debug.Log("[CampaignUI] AssessmentManager initialized with questions.");

        // 4. Show Panel
        if (UIManager.Instance == null)
        {
            Debug.LogError("[CampaignUI] CRITICAL: UIManager.Instance is NULL.");
            return;
        }
        UIManager.Instance.ShowPanel("QuizPanel");
        Debug.Log("[CampaignUI] Requesting QuizPanel...");

        // 5. Find QuizUI and Start
        // We look for it NOW, because ShowPanel should have just enabled it
        QuizUI quizUI = FindObjectOfType<QuizUI>();

        if (quizUI != null)
        {
            Debug.Log("[CampaignUI] Found QuizUI. Starting Quiz...");
            quizUI.StartQuiz(AssessmentManager.Instance.CurrentQuiz);
        }
        else
        {
            Debug.LogError("[CampaignUI] CRITICAL: Could not find 'QuizUI' script in the scene! Is the QuizPanel active?");
        }
    }
}