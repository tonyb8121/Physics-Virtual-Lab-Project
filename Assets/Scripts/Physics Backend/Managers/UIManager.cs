using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PanelEntry
{
    public string panelID;
    public GameObject panelObject;
}

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // If found in scene, use it
                _instance = FindObjectOfType<UIManager>();
                
                // If not, create temporary (but this won't have inspector links!)
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Inspector Setup")]
    [Tooltip("Drag your panels here to register them manually")]
    [SerializeField] private List<PanelEntry> inspectorPanels = new List<PanelEntry>();

    [Header("Runtime References")]
    private Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
    
    [Header("Toast Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    private GameObject _toastPanel;
    private TMP_Text _toastText;

    private void Awake()
    {
        // Ensure Singleton
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);

        // Register panels from the Inspector List
        foreach (var entry in inspectorPanels)
        {
            if (entry.panelObject != null)
            {
                RegisterPanel(entry.panelID, entry.panelObject);
            }
        }
    }

    public void Initialize()
    {
        CreateToastOverlay();
    }

    // --- Panel Management ---

    public void RegisterPanel(string name, GameObject panel)
    {
        if (!_panels.ContainsKey(name))
        {
            _panels.Add(name, panel);
            panel.SetActive(false); // Default to hidden to keep scene clean
        }
        else
        {
            // Update reference if it already exists (useful for scene reloads)
            _panels[name] = panel;
        }
    }

    public void ShowPanel(string name)
    {
        if (_panels.ContainsKey(name))
        {
            // Optional: Hide all others first? 
            // HideAllPanels(); 
            
            _panels[name].SetActive(true);
            Debug.Log($"[UIManager] Opened Panel: {name}");
        }
        else
        {
            Debug.LogError($"[UIManager] ERROR: Panel '{name}' not found! Did you add it to the UIManager list?");
        }
    }

    public void HidePanel(string name)
    {
        if (_panels.ContainsKey(name))
        {
            _panels[name].SetActive(false);
        }
    }

    public void HideAllPanels()
    {
        foreach (var p in _panels.Values)
        {
            if (p != null) p.SetActive(false);
        }
    }

    // --- Toast & Audio ---

    public void PlayClickSound()
    {
        if (clickSound && audioSource) audioSource.PlayOneShot(clickSound);
    }

    public void ShowToast(string message, float duration = 2.0f)
    {
        if (_toastPanel == null) CreateToastOverlay();
        StartCoroutine(ToastRoutine(message, duration));
    }

    private void CreateToastOverlay()
    {
        if (_toastPanel != null) return;
        // (Simplified toast creation logic preserved from your script)
        GameObject canvasGO = new GameObject("ToastCanvas");
        DontDestroyOnLoad(canvasGO);
        Canvas cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 1000;
        GameObject pnl = new GameObject("ToastPanel");
        pnl.transform.SetParent(canvasGO.transform, false);
        Image img = pnl.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);
        RectTransform rt = pnl.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.1f);
        rt.anchorMax = new Vector2(0.5f, 0.1f);
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(600, 100);
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(pnl.transform, false);
        _toastText = txtGO.AddComponent<TextMeshProUGUI>();
        _toastText.alignment = TextAlignmentOptions.Center;
        _toastText.fontSize = 28;
        _toastText.color = Color.white;
        _toastText.GetComponent<RectTransform>().sizeDelta = new Vector2(580, 90);
        _toastPanel = pnl;
        _toastPanel.SetActive(false);
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        _toastText.text = message;
        _toastPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        _toastPanel.SetActive(false);
    }
}