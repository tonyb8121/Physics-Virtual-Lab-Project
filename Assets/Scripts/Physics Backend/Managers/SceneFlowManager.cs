using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Manages scene transitions with a fade-out/fade-in effect.
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    private static SceneFlowManager _instance;
    public static SceneFlowManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneFlowManager");
                _instance = go.AddComponent<SceneFlowManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private GameObject _loadingOverlay;
    private CanvasGroup _fadeCanvasGroup;
    private Slider _progressBar;
    private bool _isLoading = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreateLoadingOverlay();
    }

    private void CreateLoadingOverlay()
    {
        // Programmatically create a simple loading canvas so we don't rely on prefabs yet
        GameObject canvasGO = new GameObject("LoadingCanvas");
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Topmost
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image bg = panelGO.AddComponent<Image>();
        bg.color = Color.black;
        RectTransform rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _fadeCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        _fadeCanvasGroup.alpha = 0; // Invisible by default
        _fadeCanvasGroup.blocksRaycasts = false;

        _loadingOverlay = canvasGO;
    }

    public void LoadScene(string sceneName, float minDuration = 0.5f)
    {
        if (_isLoading) return;
        StartCoroutine(ProcessSceneLoad(sceneName, minDuration));
    }

    private IEnumerator ProcessSceneLoad(string sceneName, float minDuration)
    {
        _isLoading = true;
        
        // Fade In (Black screen appears)
        _fadeCanvasGroup.blocksRaycasts = true;
        float timer = 0;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / 0.3f);
            yield return null;
        }
        _fadeCanvasGroup.alpha = 1;

        // Async Load
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float loadTimer = 0;
        while (!op.isDone)
        {
            loadTimer += Time.deltaTime;
            
            // Unity loads to 0.9, then waits for activation
            if (op.progress >= 0.9f && loadTimer >= minDuration)
            {
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        // Wait a frame
        yield return null;

        // Fade Out (Reveal new scene)
        timer = 0;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / 0.3f);
            yield return null;
        }
        
        _fadeCanvasGroup.alpha = 0;
        _fadeCanvasGroup.blocksRaycasts = false;
        _isLoading = false;
    }
}