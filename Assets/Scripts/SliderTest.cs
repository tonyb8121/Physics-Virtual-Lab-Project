using UnityEngine;
using UnityEngine.UI;

public class SliderTest : MonoBehaviour
{
    public Slider heightSlider;

    void Start()
    {
        if (heightSlider != null)
        {
            heightSlider.onValueChanged.AddListener(OnSliderChanged);
            Debug.Log($"Slider initial value: {heightSlider.value}");
        }
        else
        {
            Debug.LogError("Height slider not assigned!");
        }
    }

    void OnSliderChanged(float value)
    {
        Debug.Log($"SLIDER MOVED TO: {value}");
    }
}