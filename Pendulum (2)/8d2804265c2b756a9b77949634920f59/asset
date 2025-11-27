using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PendulumUI : MonoBehaviour
{
    [Header("References")]
    public Pendulum3D pendulum;
    public Slider angleSlider;
    public Slider lengthSlider;
    public Button playButton;
    public Button stopButton;
    public Button resetButton;

    public TextMeshProUGUI oscillationsText;
    public TextMeshProUGUI timerText;

    void Start()
    {
        pendulum.Configure(angleSlider.value, lengthSlider.value);

        angleSlider.onValueChanged.AddListener(val => pendulum.Configure(val, lengthSlider.value));
        lengthSlider.onValueChanged.AddListener(val => pendulum.Configure(angleSlider.value, val));

        playButton.onClick.AddListener(() => pendulum.Play());
        stopButton.onClick.AddListener(() => pendulum.Stop());
        resetButton.onClick.AddListener(() =>
        {
            pendulum.Reset();
            UpdateUI();
        });

        UpdateUI();
    }

    void Update()
    {
        if (pendulum.isRunning)
            UpdateUI();
    }

void UpdateUI()
{
    if (pendulum == null || oscillationsText == null || timerText == null)
        return;

    oscillationsText.text = $"Oscillation Count: {pendulum.oscillationCount}";

    int minutes = Mathf.FloorToInt(pendulum.timer / 60f);
    int seconds = Mathf.FloorToInt(pendulum.timer % 60f);

    timerText.text = $"Time Count: {minutes:00}:{seconds:00}";
}

}
