using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CampaignManager : MonoBehaviour
{
    private static CampaignManager _instance;
    public static CampaignManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CampaignManager");
                _instance = go.AddComponent<CampaignManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public List<QuestionBank> allLevels = new List<QuestionBank>();
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadLevels();
    }

    private void LoadLevels()
    {
        Debug.Log("[CampaignManager] Starting to load levels...");

        // 1. Load ScriptableObject assets from Resources/Campaign
        QuestionBank[] assets = Resources.LoadAll<QuestionBank>("Campaign");
        
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("[CampaignManager] No QuestionBank assets found in Resources/Campaign!");
            return;
        }

        Debug.Log($"[CampaignManager] Found {assets.Length} QuestionBank assets");

        // DEBUG: Log each asset found
        foreach (var asset in assets)
        {
            if (asset != null)
            {
                Debug.Log($"  → Found: {asset.name} | Level Index: {asset.levelIndex} | Name: {asset.levelName}");
            }
            else
            {
                Debug.LogWarning("  → Found NULL asset!");
            }
        }

        allLevels = new List<QuestionBank>(assets);

        // 2. Load JSON questions into each level
        foreach (var level in allLevels)
        {
            if (level == null)
            {
                Debug.LogWarning("[CampaignManager] Null level found in assets!");
                continue;
            }

            Debug.Log($"[CampaignManager] Processing Level {level.levelIndex}: {level.levelName}");

            // Try multiple possible JSON file paths
            string[] possiblePaths = new string[]
            {
                $"Campaign/Level{level.levelIndex}_Questions",
                $"Campaign/Level{level.levelIndex}Questions",
                $"Questions/Level{level.levelIndex}_Questions",
            };

            bool foundQuestions = false;
            List<Question> jsonQs = new List<Question>();

            // Try each possible path
            foreach (string jsonPath in possiblePaths)
            {
                jsonQs = JsonLoader.LoadQuestions(jsonPath);
                
                if (jsonQs != null && jsonQs.Count > 0)
                {
                    foundQuestions = true;
                    Debug.Log($"  ✓ Loaded {jsonQs.Count} questions from '{jsonPath}'");
                    break;
                }
            }

            if (foundQuestions)
            {
                // Clear existing questions to avoid duplicates
                if (level.questions == null)
                {
                    level.questions = new List<Question>();
                }
                else
                {
                    level.questions.Clear();
                }

                // Add the loaded questions
                level.questions.AddRange(jsonQs);
            }
            else
            {
                Debug.LogWarning($"  ✗ Level {level.levelIndex} ({level.levelName}): No JSON found!");
                
                // If no JSON found, check if there are manual questions
                if (level.questions == null || level.questions.Count == 0)
                {
                    Debug.LogError($"  ✗ Level {level.levelIndex} has NO questions at all!");
                }
            }
        }

        // 3. Sort by level index
        allLevels.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));

        // 4. Final validation and summary
        Debug.Log($"[CampaignManager] === LOADED {allLevels.Count} LEVELS ===");
        
        foreach (var level in allLevels)
        {
            int qCount = level.questions != null ? level.questions.Count : 0;
            string status = qCount > 0 ? "✓" : "✗";
            Debug.Log($"  {status} Level {level.levelIndex}: {level.levelName} ({qCount} questions)");
        }
        
        Debug.Log("==========================================");
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex == 1) return true; // Level 1 is always unlocked
        
        // Check if the PREVIOUS level has a passing score
        int prevBest = PlayerPrefs.GetInt($"Level_{levelIndex - 1}_Best", 0);
        return prevBest >= 70; // 70% unlock threshold
    }

    public int GetBestScore(int levelIndex)
    {
        return PlayerPrefs.GetInt($"Level_{levelIndex}_Best", 0);
    }

    public void CompleteLevel(int levelIndex, int scorePercent)
    {
        // Save high score
        int currentBest = GetBestScore(levelIndex);
        if (scorePercent > currentBest)
        {
            PlayerPrefs.SetInt($"Level_{levelIndex}_Best", scorePercent);
            PlayerPrefs.Save();
            
            if (showDebugLogs)
            {
                Debug.Log($"[CampaignManager] New best score for Level {levelIndex}: {scorePercent}%");
            }
        }

        // Feedback
        if (UIManager.Instance != null)
        {
            if (scorePercent >= 70)
            {
                UIManager.Instance.ShowToast($"Passed Level {levelIndex}! Next level unlocked.");
            }
            else
            {
                UIManager.Instance.ShowToast($"Score: {scorePercent}%. You need 70% to advance.");
            }
        }
    }

    // Debug method - call from Inspector or console
    [ContextMenu("Reload All Levels")]
    public void ReloadLevels()
    {
        allLevels.Clear();
        LoadLevels();
    }

    // Debug method - reset all progress
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        foreach (var level in allLevels)
        {
            PlayerPrefs.DeleteKey($"Level_{level.levelIndex}_Best");
        }
        PlayerPrefs.Save();
        Debug.Log("[CampaignManager] All campaign progress reset!");
    }
}