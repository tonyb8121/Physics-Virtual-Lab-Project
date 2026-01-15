using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the 3-tab Teacher interface: Profile, Create, Grading.
/// </summary>
public class TeacherHubUI : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button btnProfile;
    [SerializeField] private Button btnCreate;
    [SerializeField] private Button btnGrading;

    [Header("Content Panels")]
    [SerializeField] private GameObject panelProfile;
    [SerializeField] private GameObject panelCreate;
    [SerializeField] private GameObject panelGrading;

    private void Start()
    {
        btnProfile.onClick.AddListener(() => SwitchTab(0));
        btnCreate.onClick.AddListener(() => SwitchTab(1));
        btnGrading.onClick.AddListener(() => SwitchTab(2));
        
        SwitchTab(0); // Default to Profile
    }

    private void SwitchTab(int index)
    {
        panelProfile.SetActive(index == 0);
        panelCreate.SetActive(index == 1);
        panelGrading.SetActive(index == 2);
        
        Logger.Log($"Switched to tab {index}");
    }
}