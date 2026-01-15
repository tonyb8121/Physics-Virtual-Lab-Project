using UnityEngine;

/// <summary>
/// Manages gravity settings for experiments (Earth, Mars, Moon, Zero-G)
/// </summary>
public class GravityManager : MonoBehaviour
{
    private static GravityManager _instance;
    public static GravityManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GravityManager");
                _instance = go.AddComponent<GravityManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public enum GravityMode { Earth, Mars, Moon, None }

    private GravityMode _currentMode = GravityMode.Earth;
    public GravityMode CurrentMode => _currentMode;

    public float CurrentGravity
    {
        get
        {
            switch (_currentMode)
            {
                case GravityMode.Earth: return 9.81f;
                case GravityMode.Mars: return 3.71f;
                case GravityMode.Moon: return 1.62f;
                case GravityMode.None: return 0f;
                default: return 9.81f;
            }
        }
    }

    public void SetGravity(GravityMode mode)
    {
        _currentMode = mode;
        Physics.gravity = new Vector3(0, -CurrentGravity, 0);
        Logger.Log($"Gravity changed to: {mode} ({CurrentGravity} m/s²)", Color.cyan);
        UIManager.Instance.ShowToast($"Gravity: {mode}");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        SetGravity(GravityMode.Earth); // Default
    }
}