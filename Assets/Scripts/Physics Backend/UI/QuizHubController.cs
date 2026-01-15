using UnityEngine;
using UnityEngine.UI;

public class QuizHubController : MonoBehaviour
{
    [Header("Sub-Containers")]
    [SerializeField] private GameObject containerAssignments;
    [SerializeField] private GameObject containerPractice;
    [SerializeField] private GameObject containerTiers;

    [Header("Navigation Buttons")]
    [SerializeField] private Image btnAssignments;
    [SerializeField] private Image btnPractice;
    [SerializeField] private Image btnTiers;

    [Header("Colors")]
    [SerializeField] private Color activeColor = new Color(0.2f, 0.6f, 1f, 1f); // Bright Blue
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey

    private void Start()
    {
        // Add Listeners
        btnAssignments.GetComponent<Button>().onClick.AddListener(ShowAssignments);
        btnPractice.GetComponent<Button>().onClick.AddListener(ShowPractice);
        btnTiers.GetComponent<Button>().onClick.AddListener(ShowTiers);

        // Default View
        ShowAssignments();
    }

    public void ShowAssignments()
    {
        SetActivePanel(0);
    }

    public void ShowPractice()
    {
        SetActivePanel(1);
    }

    public void ShowTiers()
    {
        SetActivePanel(2);
    }

    private void SetActivePanel(int index)
    {
        // 1. Toggle Logic Panels
        containerAssignments.SetActive(index == 0);
        containerPractice.SetActive(index == 1);
        containerTiers.SetActive(index == 2);

        // 2. Update Visuals (Highlight active tab)
        if(btnAssignments) btnAssignments.color = (index == 0) ? activeColor : inactiveColor;
        if(btnPractice) btnPractice.color = (index == 1) ? activeColor : inactiveColor;
        if(btnTiers) btnTiers.color = (index == 2) ? activeColor : inactiveColor;
    }
}