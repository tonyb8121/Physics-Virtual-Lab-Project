using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuizUI : MonoBehaviour
{
    // --- UI REFERENCES ---
    [Header("Header Stats")]
    [SerializeField] private TMP_Text questionCounterText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button btnExit;

    [Header("Question Area")]
    [SerializeField] private GameObject questionAreaParent; // Parent to hide all question content
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TMP_Text[] optionTexts;

    [Header("Tools (Hidden in Exam Mode)")]
    [SerializeField] private GameObject toolsContainer; 
    [SerializeField] private Button hint1Button;
    [SerializeField] private Button hint2Button;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintContentText;
    [SerializeField] private Button btnCloseHint; 
    [SerializeField] private Button btnSkip;
    [SerializeField] private Button btnLearnMore;

    [Header("Feedback (Practice Only)")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackTitle;
    [SerializeField] private TMP_Text explanationText;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject aiThinkingOverlay;

    [Header("Results")]
    [SerializeField] private GameObject finalResultPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button closeButton;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip finishSound;

    // --- STATE ---
    private QuizData _currentQuiz;
    private int _currentIndex;
    private int _totalScore;
    private int _correctCount;
    
    private float _timerValue;
    private bool _isGlobalTimer;
    private bool _isRunning;
    private int _currentQuestionPoints;

    private const float EXAM_NEXT_DELAY = 1.5f;
    // --- NEW CONSTANT: Use a unique index to signify a SKIPPED question ---
    private const int SKIPPED_INDEX = -2; 
    // The previous index of -1 is still used for Timeout

    private void Awake()
    {
        // Setup listeners (Ensuring we don't duplicate them)
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.RemoveAllListeners(); 
            optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }

        if (hint1Button) hint1Button.onClick.RemoveAllListeners();
        if (hint2Button) hint2Button.onClick.RemoveAllListeners();
        if (btnSkip) btnSkip.onClick.RemoveAllListeners();
        if (btnLearnMore) btnLearnMore.onClick.RemoveAllListeners();
        if (btnCloseHint) btnCloseHint.onClick.RemoveAllListeners();

        if (hint1Button) hint1Button.onClick.AddListener(() => UseHint(1));
        if (hint2Button) hint2Button.onClick.AddListener(() => UseHint(2));
        if (btnCloseHint) btnCloseHint.onClick.AddListener(() => { if(hintPanel) hintPanel.SetActive(false); });
        if (btnSkip) btnSkip.onClick.AddListener(OnSkipClicked);
        if (btnLearnMore) btnLearnMore.onClick.AddListener(OnLearnMoreClicked);
        
        nextButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        if (btnExit) btnExit.onClick.RemoveAllListeners();
        
        nextButton.onClick.AddListener(NextQuestion);
        closeButton.onClick.AddListener(CloseQuiz);
        if (btnExit) btnExit.onClick.AddListener(OnExitButtonClicked);
    }
    
    private void Start()
    {
        if (hintPanel) hintPanel.SetActive(false); 

        if (AssessmentManager.Instance != null && AssessmentManager.Instance.CurrentQuiz != null)
        {
            StartQuiz(AssessmentManager.Instance.CurrentQuiz);
        }
    }

    public void StartQuiz(QuizData quiz)
    {
        _currentQuiz = quiz;
        _currentIndex = 0;
        _totalScore = 0;
        _correctCount = 0;

        _isGlobalTimer = quiz.globalTimeLimit > 0;
        
        // --- UI Setup ---
        finalResultPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        if (aiThinkingOverlay) aiThinkingOverlay.SetActive(false);
        if (hintPanel) hintPanel.SetActive(false);

        if (questionAreaParent) questionAreaParent.SetActive(true); 
        if (scoreText) scoreText.text = "Score: 0";
        if (timerText) timerText.color = Color.white;

        if (_isGlobalTimer)
        {
            // --- EXAM MODE (Teacher Assignment) ---
            if (toolsContainer) toolsContainer.SetActive(false);
            if (btnLearnMore) btnLearnMore.gameObject.SetActive(false);
            
            _timerValue = quiz.globalTimeLimit; 
            if (timerText) timerText.gameObject.SetActive(true);
        }
        else
        {
            // --- PRACTICE MODE ---
            if (toolsContainer) toolsContainer.SetActive(true);
            if (btnLearnMore) btnLearnMore.gameObject.SetActive(true);
            
            _timerValue = 0; 
            // 🚩 Setting visibility will now be handled in ShowQuestion/Update based on setting.
            // StartQuiz ensures the timer is initially visible in case it's instantly finished.
            if (timerText) timerText.gameObject.SetActive(true); 
        }
        
        _isRunning = true;
        ShowQuestion(0);
    }

    private void Update()
    {
        // 🚀 UPDATED LOGIC FOR PRACTICE TIMER HIDING IN UPDATE 🚀
        // Check if the timer is NOT visible (the new setting property)
        bool isTimerVisible = (GlobalSettingsManager.Instance != null && GlobalSettingsManager.Instance.IsPracticeTimerVisible);
        
        // If we are in Practice Mode AND the setting says the timer is NOT visible
        if (!_isGlobalTimer && !isTimerVisible)
        {
            // This is mainly for catching the setting change mid-quiz.
            _isRunning = false;
            if (timerText)
            {
                timerText.text = "∞"; // Show infinity symbol
                // Ensure the game object is actually active, even if not counting down
                timerText.gameObject.SetActive(true); 
            }
            return; // STOP HERE -> Don't count down or check for timeout
        }

        if (!_isRunning) return; // Standard check

        // Countdown
        _timerValue -= Time.deltaTime;
        
        // Update Display
        int min = Mathf.FloorToInt(_timerValue / 60);
        int sec = Mathf.FloorToInt(_timerValue % 60);
        timerText.text = $"{min:00}:{sec:00}";
        
        // Warning color
        if (_timerValue < 30) timerText.color = Color.red;
        else if (_timerValue < 60) timerText.color = Color.yellow;
        else timerText.color = Color.white;

        // Time's Up Logic
        if (_timerValue <= 0)
        {
            if (_isGlobalTimer)
            {
                // EXAM OVER - Global Timer expired
                Debug.LogWarning("⏰ TIME'S UP! Submitting exam...");
                FinishQuiz();
            }
            else
            {
                // PRACTICE: Per-question timer expired
                Debug.LogWarning("⏰ Time limit reached for question. Moving to next.");
                // Use index -1 for a timeout
                OnAnswerSelected(-1); 
            }
        }
    }

    private void ShowQuestion(int index)
    {
        if (index >= _currentQuiz.questions.Count)
        {
            FinishQuiz();
            return;
        }

        Question q = _currentQuiz.questions[index];
        
        // Update UI
        questionText.text = q.questionText;
        questionCounterText.text = $"Q: {index + 1}/{_currentQuiz.questions.Count}";
        feedbackPanel.SetActive(false);
        if (hintPanel) hintPanel.SetActive(false);

        // Reset points and buttons
        _currentQuestionPoints = q.points > 0 ? q.points : 10;
        
        if (hint1Button) hint1Button.interactable = true;
        if (hint2Button) hint2Button.interactable = true;
        if (btnLearnMore) btnLearnMore.interactable = true;
        if (btnSkip) btnSkip.interactable = true;

        // Setup options
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = true;
            optionButtons[i].image.color = Color.white;
            
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = q.options[i];
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        // 🚀 UPDATED LOGIC FOR SHOWQUESTION 🚀
        bool isTimerVisible = (GlobalSettingsManager.Instance != null && GlobalSettingsManager.Instance.IsPracticeTimerVisible);

        if (_isGlobalTimer)
        {
            // Exam Mode: Always run timer
            _timerValue = _currentQuiz.globalTimeLimit; 
            if (timerText) { timerText.gameObject.SetActive(true); timerText.color = Color.white; }
            _isRunning = true;
        }
        else
        {
            // Practice Mode
            if (isTimerVisible)
            {
                // NORMAL TIMER: Visible and counting down
                _timerValue = q.timeLimit > 0 ? q.timeLimit : 60;
                if (timerText) 
                {
                    timerText.gameObject.SetActive(true);
                    timerText.color = Color.white; // Timer will be formatted in Update
                }
                _isRunning = true; // Start countdown
            }
            else
            {
                // NO TIMER: Not counting down, show "∞"
                if (timerText) 
                {
                    timerText.gameObject.SetActive(true);
                    timerText.text = "∞"; 
                }
                _isRunning = false; // Force stop countdown
            }
        }
        // 🚀 END OF UPDATED LOGIC 🚀
    }

    private void OnAnswerSelected(int index)
    {
        _isRunning = false; 
        CancelInvoke(nameof(NextQuestion));

        Question q = _currentQuiz.questions[_currentIndex];
        // Only consider the answer correct if the index is a valid option index AND matches the correct answer
        bool isCorrect = (index >= 0 && index < optionButtons.Length) && (index == q.correctAnswerIndex);
        bool isTimeout = index == -1;
        bool isSkipped = index == SKIPPED_INDEX;
        
        // Lock answer and skip buttons only, keep Learn More accessible for feedback!
        foreach (var btn in optionButtons) btn.interactable = false;
        if (btnSkip) btnSkip.interactable = false;

        // Scoring
        if (isCorrect) 
        {
            _correctCount++;
            if (!_isGlobalTimer) 
            {
                 // Add the reduced points to the total score in practice mode
                 _totalScore += _currentQuestionPoints;
            }
            scoreText.text = $"Score: {_totalScore}";
        }

        if (_isGlobalTimer)
        {
            // EXAM MODE: Instant Next
            Invoke(nameof(NextQuestion), EXAM_NEXT_DELAY); 
        }
        else
        {
            // PRACTICE MODE: Show Feedback
            
            // Visual feedback
            if (index >= 0 && index < optionButtons.Length)
            {
                optionButtons[index].image.color = isCorrect ? Color.green : Color.red;
            }
            if (q.correctAnswerIndex >= 0 && q.correctAnswerIndex < optionButtons.Length)
            {
                // Highlight the correct answer
                optionButtons[q.correctAnswerIndex].image.color = Color.green;
            }
            
            // Text Feedback and Sound
            if (isCorrect)
            {
                feedbackTitle.text = "✅ CORRECT!";
                feedbackTitle.color = Color.green;
                PlaySound(correctSound);
            }
            else
            {
                // --- FIX: Differentiate between Skip and Timeout ---
                if (isTimeout)
                {
                    feedbackTitle.text = "⏰ TIME'S UP! (0 Points)";
                }
                else if (isSkipped)
                {
                    feedbackTitle.text = "⏭️ SKIPPED! (0 Points)";
                }
                else // Wrong Answer
                {
                    feedbackTitle.text = "❌ WRONG! (0 Points)";
                }
                
                feedbackTitle.color = Color.red;
                PlaySound(wrongSound);
                
                // Re-enable Learn More for feedback context
                if (btnLearnMore) btnLearnMore.interactable = true;
            }

            // Show explanation
            explanationText.text = string.IsNullOrEmpty(q.explanation) 
                ? "No built-in explanation. Tap 'Learn More' for AI help." 
                : q.explanation;
            
            feedbackPanel.SetActive(true);
        }
    }

    private void UseHint(int hintIndex)
    {
        if (_isGlobalTimer) return;

        Question q = _currentQuiz.questions[_currentIndex];
        string text = (hintIndex == 1) ? q.hint1 : q.hint2;
        int penalty = (hintIndex == 1) ? 5 : 10;

        if (string.IsNullOrEmpty(text)) text = "No hint available.";

        if (hintPanel)
        {
            hintPanel.SetActive(true);
            if (hintContentText) hintContentText.text = $"<b>HINT {hintIndex}:</b>\n{text}";
        }
        
        // Reduce the maximum points for this question
        _currentQuestionPoints -= penalty;
        if (_currentQuestionPoints < 0) _currentQuestionPoints = 0;

        // --- FIX: Apply a token penalty to the total score immediately in Practice Mode ---
        // This is a common pattern to show the user the cost of hints instantly.
        _totalScore -= 1; // Subtract a minimal 1 point from the total score for using the hint
        if (_totalScore < 0) _totalScore = 0;
        scoreText.text = $"Score: {_totalScore}";
        
        // Disable the used hint button
        if (hintIndex == 1 && hint1Button) hint1Button.interactable = false;
        else if (hint2Button) hint2Button.interactable = false;

        // UIManager.Instance.ShowToast($"Hint Used! -{penalty} Points"); // Use this if you have a toast system
    }

    private void OnSkipClicked()
    {
        if (_isGlobalTimer) return; 
        
        // --- FIX: Use the new SKIPPED_INDEX constant to differentiate from a timeout ---
        OnAnswerSelected(SKIPPED_INDEX); 
    }

    private void OnLearnMoreClicked()
    {
        // ... (No changes needed here)
        if (_isGlobalTimer) return; 

        Question q = _currentQuiz.questions[_currentIndex];
        string correctOpt = q.options[q.correctAnswerIndex];

        if (aiThinkingOverlay) aiThinkingOverlay.SetActive(true);
        if (btnLearnMore) btnLearnMore.interactable = false;

        if (AIQuestionGenerator.Instance != null)
        {
              StartCoroutine(AIQuestionGenerator.Instance.GetExplanation(
                  q.questionText, 
                  correctOpt, 
                  (explanation) => {
                      if (aiThinkingOverlay) aiThinkingOverlay.SetActive(false);
                      if (explanationText) explanationText.text = $"<color=cyan>AI Explanation:</color>\n{explanation}";
                      if (explanationText) explanationText.color = Color.white;
                  }
            ));
        }
    }

    private void NextQuestion()
    {
        CancelInvoke(nameof(NextQuestion));
        
        _currentIndex++;
        ShowQuestion(_currentIndex);
    }

    private void FinishQuiz()
    {
        _isRunning = false;
        StopAllCoroutines();
        CancelInvoke();
        
        // CLEANUP UI
        if (toolsContainer) toolsContainer.SetActive(false);
        if (questionAreaParent) questionAreaParent.SetActive(false); 
        if (hintPanel) hintPanel.SetActive(false);
        if (feedbackPanel) feedbackPanel.SetActive(false); 

        finalResultPanel.SetActive(true);
        
        int totalQuestions = _currentQuiz.questions.Count;
        float percent = totalQuestions > 0 ? ((float)_correctCount / totalQuestions) * 100 : 0;
        
        // Professional result display
        if (_isGlobalTimer)
        {
            finalScoreText.text = $"<b>EXAM COMPLETE</b>\n\n" +
                                  $"Correct: {_correctCount}/{totalQuestions}\n" +
                                  $"Grade: {percent:0}%";
        }
        else
        {
            finalScoreText.text = $"<b>PRACTICE COMPLETE</b>\n\n" +
                                  $"Correct: {_correctCount}/{totalQuestions}\n" +
                                  $"Total Points: <color=yellow>{_totalScore}</color>";
        }
        
        PlaySound(finishSound);

        // Save to database
        if (AssessmentManager.Instance != null)
        {
            AssessmentManager.Instance.SubmitQuizResult(_correctCount, totalQuestions, _totalScore);
        }
    }

    public void OnExitButtonClicked()
    {
        _isRunning = false;
        StopAllCoroutines();
        CancelInvoke();
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePanel(gameObject.name); 
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void CloseQuiz()
    {
        OnExitButtonClicked();
    }

    private void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}