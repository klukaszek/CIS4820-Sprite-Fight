using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class OptionsMenu : MonoBehaviour
{

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private TMPro.TMP_Dropdown resolutionDropdown;
    [SerializeField] private GameObject audioSlider;
    [SerializeField] private AudioMixer mixer;
    private GameObject optionsMenu;
    private Resolution[] resolutions;

    void Start() {
        optionsMenu = transform.gameObject;
        resolutions = Screen.resolutions;

        //Clear anything that may exist in the list
        resolutionDropdown.ClearOptions();
        
        //Used by dropdown to build a scrollable list
        List<string> options = new List<string>();

        int curResIndex = 0;
        Resolution curRes = Screen.currentResolution;

        //Build list of possible resolutions and find current resolution
        for(int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " " + resolutions[i].refreshRate + "hz";
            options.Add(option);

            //Set default resolution
            if(resolutions[i].width == curRes.width && resolutions[i].height == curRes.height && resolutions[i].refreshRate == curRes.refreshRate)
            {
                curResIndex = i;
            }
        }

        //Make sure slider is set to the right audio level when reopening the options menu after an adjustment 
        Slider audio = audioSlider.GetComponent<Slider>();
        float value;
        mixer.GetFloat("Master Volume", out value);
        audio.value = value;

        //Setup our resolution dropdown with our list and recommended resolution
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = curResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    //Set resolution on dropdown change
    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen, res.refreshRate);
    }

    //Toggle fullscreen on value change
    public void ToggleFullScreen(bool value)
    {
        Screen.fullScreen = value;
    }

    //Set volume on slider value change
    public void SetVolume(float volume)
    {
        mixer.SetFloat("Master Volume", volume);
    }

    //Go back to main menu
    public void BackButton()
    {
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

}
