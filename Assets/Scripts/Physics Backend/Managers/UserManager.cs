using UnityEngine;
using System.Collections.Generic;

public class UserManager : MonoBehaviour
{
    private static UserManager _instance;
    public static UserManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UserManager");
                _instance = go.AddComponent<UserManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public string UserId { get; private set; }
    public int TotalPoints { get; private set; }
    public string Role { get; private set; }

    public void SetUser(string userId, string role, int points)
    {
        UserId = userId;
        Role = role;
        TotalPoints = points;
    }

    public void AddPoints(int amount)
    {
        TotalPoints += amount;
        // In real app, sync this back to FirestoreHelper immediately or via ProgressManager
    }
}