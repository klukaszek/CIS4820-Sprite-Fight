using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Bar : MonoBehaviour
{
    public Slider slider;

    public void SetMaxSliderValue(float value)
    {
        slider.maxValue = value;
        slider.value = value;
    }

    public void SetSlider(float value)
    {
        slider.value = value;
    }
}
