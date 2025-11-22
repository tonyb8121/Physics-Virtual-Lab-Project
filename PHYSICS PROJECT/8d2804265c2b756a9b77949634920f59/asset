using UnityEngine;
using UnityEngine.UI;

public class PendulumUI : MonoBehaviour
{
    [Header("References")]
    public Pendulum3D pendulum;
    public Slider angleSlider;
    public Slider lengthSlider;
    public Button playButton;
    public Button stopButton;

    void Start()
    {
        // Initial setup
        pendulum.Configure(angleSlider.value, lengthSlider.value);

        // Sliders update configuration immediately
        angleSlider.onValueChanged.AddListener(val => pendulum.Configure(val, lengthSlider.value));
        lengthSlider.onValueChanged.AddListener(val => pendulum.Configure(angleSlider.value, val));

        // Buttons
        playButton.onClick.AddListener(pendulum.Play);
        stopButton.onClick.AddListener(pendulum.Stop);
    }
}
