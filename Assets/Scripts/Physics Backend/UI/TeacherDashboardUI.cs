using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

public class TeacherDashboardUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject step1Panel;
    [SerializeField] private GameObject step2Panel;
    [SerializeField] private GameObject step3Panel;

    [Header("Step 1: Setup")]
    [SerializeField] private TMP_InputField classIdInput;
    [SerializeField] private TMP_InputField assignmentTitleInput;
    [SerializeField] private TMP_InputField topicInput;
    [SerializeField] private TMP_Dropdown levelDropdown;
    [SerializeField] private Slider questionCountSlider;
    [SerializeField] private TMP_Text questionCountLabel;
    [SerializeField] private Button step1NextButton;

    [Header("Step 2: Question Editor")]
    [SerializeField] private TMP_Text currentQuestionLabel; // "Question 1 of 5" or "New Question"
    [SerializeField] private TMP_InputField editQuestionText;
    [SerializeField] private TMP_InputField editOptionA;
    [SerializeField] private TMP_InputField editOptionB;
    [SerializeField] private TMP_InputField editOptionC;
    [SerializeField] private TMP_InputField editOptionD;
    [SerializeField] private TMP_Dropdown editCorrectDropdown;
    [SerializeField] private TMP_InputField editExplanation;
    [SerializeField] private Button saveChangesButton; // Saves current Q and opens next blank Q
    [SerializeField] private Button btnViewAllQuestions; // Opens popup to see all questions
    [SerializeField] private Button step2BackButton;
    [SerializeField] private Button step2NextButton;

    [Header("Question List Popup")]
    [SerializeField] private GameObject questionListPopup;
    [SerializeField] private Transform popupQuestionContainer;
    [SerializeField] private GameObject questionCardPrefab; // Card with: Title, Preview, Edit, Delete buttons
    [SerializeField] private Button btnClosePopup;

    [Header("Step 3: Publish - Deadline")]
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_InputField dueInDaysInput;
    [SerializeField] private TMP_InputField dueTimeHourInput;
    [SerializeField] private TMP_InputField dueTimeMinuteInput;
    [SerializeField] private TMP_Text deadlinePreviewText;
    
    [Header("Step 3: Publish - Time Limit")]
    [SerializeField] private TMP_InputField timeLimitInput;
    
    [SerializeField] private Button step3BackButton;
    [SerializeField] private Button publishButton;

    // DATA STATE
    private List<Question> _questionsCart = new List<Question>();
    private int _currentEditingIndex = -1; // -1 = creating new, >=0 = editing existing
    private List<TMP_InputField> _optionInputs;

    private void Awake()
    {
        _optionInputs = new List<TMP_InputField> { editOptionA, editOptionB, editOptionC, editOptionD };
    }

    private void Start()
    {
        step1NextButton.onClick.AddListener(OnStep1Next);
        step2BackButton.onClick.AddListener(() => GoToStep(1));
        step2NextButton.onClick.AddListener(OnStep2Next);
        saveChangesButton.onClick.AddListener(OnSaveChanges); // ✅ NEW: Smart save logic
        btnViewAllQuestions.onClick.AddListener(OpenQuestionListPopup);
        btnClosePopup.onClick.AddListener(CloseQuestionListPopup);
        step3BackButton.onClick.AddListener(() => GoToStep(2));
        publishButton.onClick.AddListener(OnPublish);

        questionCountSlider.onValueChanged.AddListener((v) => questionCountLabel.text = $"Generate: {v} Questions");

        if (dueInDaysInput != null)
            dueInDaysInput.onValueChanged.AddListener((v) => UpdateDeadlinePreview());
        if (dueTimeHourInput != null)
            dueTimeHourInput.onValueChanged.AddListener((v) => UpdateDeadlinePreview());
        if (dueTimeMinuteInput != null)
            dueTimeMinuteInput.onValueChanged.AddListener((v) => UpdateDeadlinePreview());
        if (timeLimitInput != null)
            timeLimitInput.onValueChanged.AddListener((v) => UpdateSummary());

        if (dueInDaysInput != null) dueInDaysInput.text = "7";
        if (dueTimeHourInput != null) dueTimeHourInput.text = "23";
        if (dueTimeMinuteInput != null) dueTimeMinuteInput.text = "59";
        if (timeLimitInput != null) timeLimitInput.text = "0";

        if (questionListPopup) questionListPopup.SetActive(false);

        GoToStep(1);
    }

    // --- STEP 1 LOGIC ---
    private void OnStep1Next()
    {
        if (string.IsNullOrEmpty(topicInput.text) || string.IsNullOrEmpty(assignmentTitleInput.text))
        {
            UIManager.Instance.ShowToast("Please enter Topic and Title");
            return;
        }

        int count = (int)questionCountSlider.value;
        if (count > 0)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                UIManager.Instance.ShowToast("Offline: Connect to Internet to use AI generation.");
                return;
            }

            UIManager.Instance.ShowToast("AI Generating... Please Wait.");
            string level = levelDropdown.options[levelDropdown.value].text;

            StartCoroutine(AIQuestionGenerator.Instance.GenerateQuestions(
                topicInput.text,
                level,
                count,
                OnAIGenerated));
        }
        else
        {
            // Manual creation mode
            _questionsCart.Clear();
            GoToStep(2);
        }
    }

    private void OnAIGenerated(List<Question> aiQuestions)
    {
        if (aiQuestions != null)
        {
            _questionsCart = aiQuestions;
            
            string tId = (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null) 
                         ? AuthManager.Instance.CurrentUser.UserId : "Unknown_Teacher";

            foreach (var q in _questionsCart)
            {
                List<string> optionsList = q.options != null ? q.options.ToList() : new List<string>();
                
                while (optionsList.Count < 4) optionsList.Add(string.Empty);
                while (optionsList.Count > 4) optionsList.RemoveAt(optionsList.Count - 1);
                
                q.options = optionsList.ToArray();
                q.moduleId = topicInput.text;
                q.difficulty = levelDropdown.options[levelDropdown.value].text;
                q.teacherId = tId;
            }
            GoToStep(2);
        }
        else
        {
            UIManager.Instance.ShowToast("AI Failed. Try again or check internet.");
        }
    }

    // --- STEP 2 LOGIC (NEW FLOW) ---
    private void GoToStep(int step)
    {
        step1Panel.SetActive(step == 1);
        step2Panel.SetActive(step == 2);
        step3Panel.SetActive(step == 3);

        if (step == 2)
        {
            if (_questionsCart.Count > 0)
            {
                // AI generated questions - load first one
                LoadQuestionForEdit(0);
            }
            else
            {
                // Manual creation - start with blank Q1
                StartNewQuestion();
            }
            UpdateViewButton();
        }

        if (step == 3) UpdateSummary();
    }

    // ✅ NEW: Smart Save Logic
    private void OnSaveChanges()
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(editQuestionText.text))
        {
            UIManager.Instance.ShowToast("❌ Question text cannot be empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(editOptionA.text) || 
            string.IsNullOrWhiteSpace(editOptionB.text) ||
            string.IsNullOrWhiteSpace(editOptionC.text) || 
            string.IsNullOrWhiteSpace(editOptionD.text))
        {
            UIManager.Instance.ShowToast("❌ All 4 options must be filled!");
            return;
        }

        // Create/Update question
        Question q;
        if (_currentEditingIndex == -1)
        {
            // Creating new question
            q = new Question
            {
                id = System.Guid.NewGuid().ToString(),
                moduleId = topicInput.text,
                difficulty = levelDropdown.options[levelDropdown.value].text,
                teacherId = (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null) 
                           ? AuthManager.Instance.CurrentUser.UserId : "Unknown_Teacher"
            };
            _questionsCart.Add(q);
        }
        else
        {
            // Editing existing question
            q = _questionsCart[_currentEditingIndex];
        }

        // Save data
        q.questionText = editQuestionText.text;
        q.options = new string[4]
        {
            editOptionA.text,
            editOptionB.text,
            editOptionC.text,
            editOptionD.text
        };
        q.correctAnswerIndex = editCorrectDropdown.value;
        q.explanation = editExplanation.text;

        UIManager.Instance.ShowToast("✅ Question Saved!");

        // ✅ Auto-open next blank question
        StartNewQuestion();
        UpdateViewButton();
    }

    private void StartNewQuestion()
    {
        _currentEditingIndex = -1; // Signals "new question"
        
        ClearEditor();
        
        if (currentQuestionLabel)
        {
            currentQuestionLabel.text = $"New Question (Total: {_questionsCart.Count})";
        }
    }

    private void ClearEditor()
    {
        editQuestionText.text = "";
        editOptionA.text = "";
        editOptionB.text = "";
        editOptionC.text = "";
        editOptionD.text = "";
        editCorrectDropdown.value = 0;
        editExplanation.text = "";
    }

    private void LoadQuestionForEdit(int index)
    {
        if (index < 0 || index >= _questionsCart.Count) return;

        _currentEditingIndex = index;
        Question q = _questionsCart[index];

        editQuestionText.text = q.questionText;

        for (int i = 0; i < _optionInputs.Count; i++)
        {
            _optionInputs[i].text = (q.options != null && q.options.Length > i) ? q.options[i] : string.Empty;
        }

        if (q.correctAnswerIndex >= 0 && q.correctAnswerIndex < editCorrectDropdown.options.Count)
        {
            editCorrectDropdown.value = q.correctAnswerIndex;
        }
        else
        {
             editCorrectDropdown.value = 0;
        }
        
        editExplanation.text = q.explanation;

        if (currentQuestionLabel)
        {
            currentQuestionLabel.text = $"Editing Q{index + 1} of {_questionsCart.Count}";
        }
    }

    private void UpdateViewButton()
    {
        if (btnViewAllQuestions)
        {
            TMP_Text btnText = btnViewAllQuestions.GetComponentInChildren<TMP_Text>();
            if (btnText) btnText.text = $"📋 View All Questions ({_questionsCart.Count})";
        }
    }

    private void OnStep2Next()
    {
        if (_questionsCart.Count == 0)
        {
            UIManager.Instance.ShowToast("❌ Add at least one question before publishing!");
            return;
        }
        GoToStep(3);
    }

    // --- POPUP LOGIC ---
    private void OpenQuestionListPopup()
    {
        if (questionListPopup) questionListPopup.SetActive(true);
        RefreshQuestionListPopup();
    }

    private void CloseQuestionListPopup()
    {
        if (questionListPopup) questionListPopup.SetActive(false);
    }

    private void RefreshQuestionListPopup()
    {
        if (popupQuestionContainer == null) return;

        foreach (Transform child in popupQuestionContainer) Destroy(child.gameObject);

        if (_questionsCart.Count == 0)
        {
            GameObject emptyMsg = new GameObject("EmptyMessage");
            emptyMsg.transform.SetParent(popupQuestionContainer, false);
            
            TMP_Text txt = emptyMsg.AddComponent<TextMeshProUGUI>();
            txt.text = "No questions created yet.\nClick 'Save Changes' to add your first question!";
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 18;
            txt.color = Color.gray;

            RectTransform rt = emptyMsg.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 100);
            return;
        }

        for (int i = 0; i < _questionsCart.Count; i++)
        {
            int index = i;
            Question q = _questionsCart[i];

            if (questionCardPrefab == null)
            {
                Debug.LogError("[TeacherDashboard] questionCardPrefab not assigned!");
                break;
            }

            GameObject card = Instantiate(questionCardPrefab, popupQuestionContainer);
            
            TMP_Text titleText = card.transform.Find("Title")?.GetComponent<TMP_Text>();
            TMP_Text previewText = card.transform.Find("Preview")?.GetComponent<TMP_Text>();
            Button editBtn = card.transform.Find("BtnEdit")?.GetComponent<Button>();
            Button deleteBtn = card.transform.Find("BtnDelete")?.GetComponent<Button>();

            if (titleText) titleText.text = $"Q{index + 1}";
            if (previewText)
            {
                string preview = q.questionText.Length > 60 
                    ? q.questionText.Substring(0, 60) + "..." 
                    : q.questionText;
                previewText.text = preview;
            }

            if (editBtn) editBtn.onClick.AddListener(() => {
                CloseQuestionListPopup();
                LoadQuestionForEdit(index);
            });
            
            if (deleteBtn) deleteBtn.onClick.AddListener(() => DeleteQuestion(index));
        }
    }

    private void DeleteQuestion(int index)
    {
        if (index < 0 || index >= _questionsCart.Count) return;

        _questionsCart.RemoveAt(index);
        UIManager.Instance.ShowToast("🗑️ Question Deleted!");
        
        RefreshQuestionListPopup();
        UpdateViewButton();

        // If we deleted the question we were editing, clear the editor
        if (_currentEditingIndex == index)
        {
            StartNewQuestion();
        }
        else if (_currentEditingIndex > index)
        {
            _currentEditingIndex--; // Adjust index after deletion
        }
    }

    // --- STEP 3 LOGIC ---
    private void UpdateSummary()
    {
        string timeTxt = "No Timer (Practice Mode)";
        
        if (timeLimitInput != null && !string.IsNullOrEmpty(timeLimitInput.text))
        {
            if (int.TryParse(timeLimitInput.text, out int mins) && mins > 0)
            {
                timeTxt = $"⏱️ {mins} Minutes (Exam Mode)";
            }
        }

        summaryText.text = $"<b>Assignment:</b> {assignmentTitleInput.text}\n" +
                           $"<b>Class:</b> {classIdInput.text}\n" +
                           $"<b>Topic:</b> {topicInput.text}\n" +
                           $"<b>Time Limit:</b> {timeTxt}\n" +
                           $"<b>Total Questions:</b> {_questionsCart.Count}";
        
        UpdateDeadlinePreview();
    }

    private void UpdateDeadlinePreview()
    {
        if (deadlinePreviewText == null) return;

        try
        {
            int days = int.Parse(dueInDaysInput.text);
            int hours = int.Parse(dueTimeHourInput.text);
            int minutes = int.Parse(dueTimeMinuteInput.text);

            if (days < 0) days = 0;
            if (hours < 0) hours = 0; if (hours > 23) hours = 23;
            if (minutes < 0) minutes = 0; if (minutes > 59) minutes = 59;

            DateTime deadline = DateTime.Now.AddDays(days).Date
                .AddHours(hours)
                .AddMinutes(minutes);

            deadlinePreviewText.text = $"<b>Deadline:</b> {deadline:MMM dd, yyyy 'at' HH:mm}";
            deadlinePreviewText.color = Color.white;
        }
        catch
        {
            deadlinePreviewText.text = "<b>Deadline:</b> Invalid input";
            deadlinePreviewText.color = Color.red;
        }
    }

    private async void OnPublish()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            UIManager.Instance.ShowToast("Offline: Connect to Internet to publish assignments.");
            return;
        }

        if (AuthManager.Instance == null || AuthManager.Instance.CurrentUser == null)
        {
            UIManager.Instance.ShowToast("Error: You are not logged in. Cannot Publish.");
            return;
        }
        string currentTeacherId = AuthManager.Instance.CurrentUser.UserId;

        if (_questionsCart.Count == 0)
        {
            UIManager.Instance.ShowToast("❌ Cannot publish: No questions added!");
            return;
        }

        if (!int.TryParse(dueInDaysInput.text, out int days) || days < 0)
        {
            UIManager.Instance.ShowToast("Invalid days input.");
            return;
        }

        if (!int.TryParse(dueTimeHourInput.text, out int hours) || hours < 0 || hours > 23) {
            UIManager.Instance.ShowToast("Invalid hour (0-23)."); return; 
        }

        if (!int.TryParse(dueTimeMinuteInput.text, out int minutes) || minutes < 0 || minutes > 59) {
            UIManager.Instance.ShowToast("Invalid minute (0-59)."); return; 
        }

        int.TryParse(timeLimitInput.text, out int timeLimit);
        if (timeLimit < 0) timeLimit = 0;

        publishButton.interactable = false;
        UIManager.Instance.ShowToast("Publishing to Student App...");

        DateTime deadline = DateTime.Now.AddDays(days).Date
            .AddHours(hours)
            .AddMinutes(minutes);

        AssignmentData assignment = new AssignmentData
        {
            title = assignmentTitleInput.text,
            classId = classIdInput.text,
            teacherId = currentTeacherId,
            dueDate = deadline.Ticks,
            timeLimitMinutes = timeLimit,
            questions = new List<Question>(_questionsCart)
        };

        bool success = await FirestoreHelper.PublishAssignment(assignment);

        if (success)
        {
            UIManager.Instance.ShowToast("Published Successfully!");
            _questionsCart.Clear();
            GoToStep(1);
        }
        else
        {
            UIManager.Instance.ShowToast("Publishing Failed. Check Console for details.");
        }
        
        publishButton.interactable = true;
    }
}