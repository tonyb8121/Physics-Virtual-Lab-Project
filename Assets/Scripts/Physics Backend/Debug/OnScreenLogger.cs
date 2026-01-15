using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class OnScreenLogger : MonoBehaviour
{
    private static OnScreenLogger _instance;
    private StringBuilder logBuilder = new StringBuilder();
    private TextMeshProUGUI logText;
    private GameObject logPanel;
    
    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreateLogUI();
        Application.logMessageReceived += HandleLog;
    }
    
    private void CreateLogUI()
    {
        GameObject canvasGO = new GameObject("DebugCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);
        
        logPanel = new GameObject("LogPanel");
        logPanel.transform.SetParent(canvasGO.transform, false);
        
        Image panelImage = logPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f);
        
        RectTransform panelRect = logPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0.5f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        GameObject textGO = new GameObject("LogText");
        textGO.transform.SetParent(logPanel.transform, false);
        
        logText = textGO.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 20;
        logText.color = Color.white;
        logText.alignment = TextAlignmentOptions.TopLeft;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);
    }
    
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string prefix = type == LogType.Error || type == LogType.Exception ? "❌ " : 
                        type == LogType.Warning ? "⚠️ " : "✅ ";
        Log(prefix + logString);
    }
    
    public static void Log(string message)
    {
        if (_instance == null) return;
        
        _instance.logBuilder.AppendLine($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        
        // Keep last 30 lines
        string[] lines = _instance.logBuilder.ToString().Split('\n');
        if (lines.Length > 30)
        {
            _instance.logBuilder.Clear();
            for (int i = lines.Length - 30; i < lines.Length; i++)
            {
                _instance.logBuilder.AppendLine(lines[i]);
            }
        }
        
        if (_instance.logText != null)
        {
            _instance.logText.text = _instance.logBuilder.ToString();
        }
    }
}