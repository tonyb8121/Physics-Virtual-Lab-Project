using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class StudentPracticeUI : MonoBehaviour
{
    [Header("Input Controls")]
    [SerializeField] private TMP_InputField topicInput;
    [SerializeField] private TMP_Dropdown levelDropdown;
    [SerializeField] private Button startButton;
    
    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;
    
    [Header("Direct Links")]
    [SerializeField] private GameObject quizPanelObject; 
    
    // We'll get the QuizUI component automatically
    private QuizUI _quizScript;
    
    private void Start()
    {
        startButton.onClick.AddListener(OnStartPractice);
        
        // Get QuizUI component from the GameObject
        if (quizPanelObject != null)
        {
            _quizScript = quizPanelObject.GetComponent<QuizUI>();
            if (_quizScript == null)
            {
                Debug.LogError("QuizUI component not found on Quiz Panel Object!");
            }
        }
        else
        {
            Debug.LogError("Quiz Panel Object not assigned in Inspector!");
        }
        
        // Setup dropdown if empty
        if (levelDropdown.options.Count == 0)
        {
            levelDropdown.AddOptions(new List<string> { "Form 1", "Form 2", "Form 3", "Form 4" });
        }
        
        statusText.text = "Ready";
    }
    
    private void OnStartPractice()
    {
        string topic = topicInput.text.Trim();
        if (string.IsNullOrEmpty(topic))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Please enter a topic!");
            return;
        }
        
        // Offline Check
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Offline: Loading local practice.");
            LoadOfflinePractice();
            return;
        }
        
        string level = levelDropdown.options[levelDropdown.value].text;
        
        startButton.interactable = false;
        statusText.text = "Generating Questions...";
        statusText.color = Color.yellow;
        
        // Check if AIQuestionGenerator exists
        if (AIQuestionGenerator.Instance == null)
        {
            Debug.LogError("AIQuestionGenerator.Instance is null!");
            statusText.text = "Error: AI Generator not found";
            statusText.color = Color.red;
            startButton.interactable = true;
            return;
        }
        
        StartCoroutine(AIQuestionGenerator.Instance.GenerateQuestions(topic, level, 5, OnQuestionsGenerated));
    }
    
    private void OnQuestionsGenerated(List<Question> questions)
    {
        startButton.interactable = true;
        statusText.text = "Ready";
        statusText.color = Color.white;
        
        if (questions != null && questions.Count > 0)
        {
            LaunchQuiz(questions);
        }
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("AI Generation Failed. Try again.");
            statusText.text = "Generation Failed";
            statusText.color = Color.red;
        }
    }
    
    private void LoadOfflinePractice()
    {
        // Try to load from Resources
        List<Question> offlineQuestions = JsonLoader.LoadQuestions("Questions/ProjectileMotion_Easy");
        
        if (offlineQuestions != null && offlineQuestions.Count > 0)
        {
            LaunchQuiz(offlineQuestions);
        }
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("No offline questions found.");
            statusText.text = "No Offline Content";
            statusText.color = Color.red;
        }
    }
    
    private void LaunchQuiz(List<Question> questions)
    {
        // Check if AssessmentManager exists
        if (AssessmentManager.Instance == null)
        {
            Debug.LogError("AssessmentManager.Instance is null!");
            statusText.text = "Error: Manager not found";
            statusText.color = Color.red;
            return;
        }
        
        // Load the quiz data
        AssessmentManager.Instance.LoadPracticeQuizFromList(questions);
        
        // Launch the quiz UI
        if (quizPanelObject != null && _quizScript != null)
        {
            quizPanelObject.SetActive(true);
            _quizScript.StartQuiz(AssessmentManager.Instance.CurrentQuiz);
            
            // Optional: Hide the practice panel
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("QuizPanel or QuizUI script not properly linked!");
            statusText.text = "Error: Quiz UI Missing";
            statusText.color = Color.red;
        }
    }
    
    // Public method to reset the panel when returning from quiz
    public void ResetPanel()
    {
        topicInput.text = "";
        statusText.text = "Ready";
        statusText.color = Color.white;
        startButton.interactable = true;
    }
}