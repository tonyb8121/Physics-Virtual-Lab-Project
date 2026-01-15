using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class GamificationManager : MonoBehaviour
{
    private static GamificationManager _instance;
    public static GamificationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GamificationManager");
                _instance = go.AddComponent<GamificationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private AchievementData _achievementData;

    public void Initialize()
    {
        LoadAchievements();
    }

    private void LoadAchievements()
    {
        TextAsset json = Resources.Load<TextAsset>("Achievements/Achievements");
        if (json != null)
        {
            _achievementData = JsonConvert.DeserializeObject<AchievementData>(json.text);
        }
        else
        {
            _achievementData = new AchievementData { achievements = new List<Achievement>() };
        }
    }

    public void AwardPoints(int points, string reason)
    {
        Logger.Log($"Awarded {points} points for: {reason}", Color.yellow);
        
        // In a real app, we'd update the User object locally and push to Firestore
        if (AuthManager.Instance.IsLoggedIn)
        {
            // Placeholder: Assume UserManager exists (File 35) or update AuthManager user data directly
            UIManager.Instance.ShowToast($"+{points} Points!");
            CheckBadges(1000); // Pass current total points here
        }
    }

    private void CheckBadges(int currentTotalPoints)
    {
        if (_achievementData == null) return;

        foreach (var badge in _achievementData.achievements)
        {
            if (currentTotalPoints >= badge.requiredPoints)
            {
                // Check if already unlocked (need local persistence for this check)
                // For MVP, just logging
                // Logger.Log($"Badge Unlocked: {badge.name}");
            }
        }
    }
}

[System.Serializable]
public class AchievementData
{
    public List<Achievement> achievements;
}

[System.Serializable]
public class Achievement
{
    public string badgeId;
    public string name;
    public string description;
    public int requiredPoints;
    public string iconPath;
}