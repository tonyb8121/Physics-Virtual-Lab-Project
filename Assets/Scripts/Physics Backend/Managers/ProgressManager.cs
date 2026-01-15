using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProgressManager : MonoBehaviour
{
    private static ProgressManager _instance;
    public static ProgressManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ProgressManager");
                _instance = go.AddComponent<ProgressManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<string, float> _moduleTimers = new Dictionary<string, float>();
    private string _currentActiveModule;

    private void Start()
    {
        StartCoroutine(AutoSyncRoutine());
    }

    public void StartTrackingModule(string moduleId)
    {
        _currentActiveModule = moduleId;
        if (!_moduleTimers.ContainsKey(moduleId))
        {
            _moduleTimers[moduleId] = 0f;
        }
    }

    public void StopTracking()
    {
        _currentActiveModule = null;
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(_currentActiveModule))
        {
            _moduleTimers[_currentActiveModule] += Time.deltaTime;
        }
    }

    private IEnumerator AutoSyncRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(30f); // Sync every 30s
        while (true)
        {
            yield return wait;
            
            if (AuthManager.Instance.IsLoggedIn)
            {
                // Sync logic here
                // For MVP, we just log
                // Logger.Log("Auto-syncing progress...");
            }
        }
    }
}