using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class AssessmentManager : MonoBehaviour
{
    private static AssessmentManager _instance;
    public static AssessmentManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AssessmentManager");
                _instance = go.AddComponent<AssessmentManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public QuizData CurrentQuiz { get; private set; }

    // LOGIC TRACKING
    private string _activeAssignmentId = null;
    private bool _isGradedAssignment = false;
    private int _currentLevelIndex = 0; // Tracks which Campaign Level we are playing

    // --- 1. PRACTICE MODE (Local JSON) ---
    public void LoadPracticeQuiz(string moduleId, string difficulty)
    {
        _isGradedAssignment = false;
        _activeAssignmentId = null;
        _currentLevelIndex = 0;

        string path = $"Questions/{moduleId}_{difficulty}";
        TextAsset jsonFile = Resources.Load<TextAsset>(path);

        List<Question> allQuestions = new List<Question>();

        if (jsonFile != null)
        {
            QuizData localData = JsonConvert.DeserializeObject<QuizData>(jsonFile.text);
            allQuestions.AddRange(localData.questions);
        }

        CurrentQuiz = new QuizData { questions = allQuestions, globalTimeLimit = 0 };
        Logger.Log($"Starting Practice Quiz for {moduleId}. Qs: {allQuestions.Count}");
    }

    // --- 2. ASSIGNMENT MODE (Teacher Data) ---
    public void StartAssignmentQuiz(AssignmentData assignment)
    {
        _isGradedAssignment = true;
        _activeAssignmentId = assignment.id;
        _currentLevelIndex = 0;

        int limitInSeconds = assignment.timeLimitMinutes * 60;

        CurrentQuiz = new QuizData { questions = assignment.questions, globalTimeLimit = limitInSeconds };
        Logger.Log($"Starting Assignment: {assignment.title}. ID: {_activeAssignmentId}");
    }

    // --- 3. CAMPAIGN / TIER MODE ---
    public void StartCampaignLevel(int levelIndex, List<Question> questions)
    {
        _isGradedAssignment = false;
        _activeAssignmentId = null;
        _currentLevelIndex = levelIndex;

        if (questions == null || questions.Count == 0)
        {
            Logger.LogError($"Cannot start Campaign Level {levelIndex}: questions list is empty!");
            return;
        }

        CurrentQuiz = new QuizData { questions = questions, globalTimeLimit = 0 };
        Logger.Log($"Started Tier Level {levelIndex}. Qs: {questions.Count}", Color.magenta);
    }

    // --- 4. AI PRACTICE MODE ---
    public void LoadPracticeQuizFromList(List<Question> questions)
    {
        _isGradedAssignment = false;
        _activeAssignmentId = null;
        _currentLevelIndex = 0;

        if (questions == null || questions.Count == 0)
        {
            Logger.LogError("Cannot load empty practice quiz!");
            return;
        }

        CurrentQuiz = new QuizData { questions = questions, globalTimeLimit = 0 };
        Logger.Log($"Practice Quiz Loaded: {questions.Count} AI questions", Color.cyan);
    }

    public async void SubmitQuizResult(int correctAnswers, int totalQuestions, int totalPoints = 0)
    {
        int scorePercentage = (totalQuestions > 0) ? Mathf.RoundToInt((float)correctAnswers / totalQuestions * 100) : 0;
        bool passed = scorePercentage >= 50;

        Logger.Log($"Quiz Finished. Score: {scorePercentage}%");

        // --- 1. LEADERBOARD UPDATE (TIERS ONLY) ---
        if (_currentLevelIndex > 0 && AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
        {
            int finalPoints = totalPoints > 0 ? totalPoints : (correctAnswers * 10);
            await FirestoreHelper.AddUserPoints(AuthManager.Instance.CurrentUser.UserId, finalPoints);
            Logger.Log($"[Leaderboard] Added {finalPoints} points for Tier Level {_currentLevelIndex}", Color.green);
        }
        else
        {
            Logger.Log("[Leaderboard] Points NOT added (Not a Tier Level)");
        }

        // --- 2. GAMIFICATION ---
        if (GamificationManager.Instance != null)
        {
            int pointsToAward = totalPoints > 0 ? totalPoints : scorePercentage;
            string activityName = _isGradedAssignment ? "Homework Finished" : "Practice Quiz";
            GamificationManager.Instance.AwardPoints(pointsToAward, activityName);
        }

        // --- 3. LOCAL PROGRESS ---
        if (!_isGradedAssignment && CampaignManager.Instance != null && _currentLevelIndex > 0)
        {
            CampaignManager.Instance.CompleteLevel(_currentLevelIndex, scorePercentage);
        }

        // --- 4. CLOUD SUBMISSION ---
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn && AuthManager.Instance.CurrentUser != null)
        {
            string userId = AuthManager.Instance.CurrentUser.UserId;

            if (_isGradedAssignment && !string.IsNullOrEmpty(_activeAssignmentId))
            {
                var userData = await FirestoreHelper.GetUserData(userId);
                string admNo = (userData != null && userData.ContainsKey("admissionNumber")) ? userData["admissionNumber"].ToString() : "Unknown";

                StudentSubmission submission = new StudentSubmission
                {
                    assignmentId = _activeAssignmentId,
                    studentId = userId,
                    studentName = AuthManager.Instance.CurrentUser.DisplayName ?? "Student",
                    admissionNumber = admNo,
                    score = correctAnswers,
                    totalQuestions = totalQuestions,
                    percentage = scorePercentage
                };

                string result = await FirestoreHelper.SubmitGrade(submission);
                if (result == "Success") UIManager.Instance.ShowToast($"Submitted: {scorePercentage}%");
            }
            else
            {
                string modId = (CurrentQuiz.questions.Count > 0) ? CurrentQuiz.questions[0].moduleId : "General";
                string saveId = (_currentLevelIndex > 0) ? $"Level_{_currentLevelIndex}" : modId;

                await FirestoreHelper.SaveProgress(userId, saveId, scorePercentage, passed);
            }
        }
    }
}

// --- DATA STRUCTURES ---
[System.Serializable]
public class AssignmentData
{
    public string id;
    public string title;
    public string teacherId;
    public string classId;
    public long dueDate;
    public int timeLimitMinutes;
    public List<Question> questions;
    public long createdAt;
    public bool isOverdue;
}

[System.Serializable]
public class StudentSubmission
{
    public string id;
    public string assignmentId;
    public string studentId;
    public string studentName;
    public int score;
    public int totalQuestions;
    public string admissionNumber;
    public long submittedAt;
    public float percentage;
}

[System.Serializable]
public class QuizData
{
    public List<Question> questions;
    public int globalTimeLimit;
}

[System.Serializable]
public class Question
{
    public string id;
    public string questionText;
    public string[] options;
    public int correctAnswerIndex;
    public string explanation;
    public string moduleId;
    public string difficulty;
    public string teacherId;
    public int points = 10;
    public int timeLimit = 60;
    public string hint1 = "";
    public string hint2 = "";
}