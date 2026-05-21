using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [Header("UI 패널")]
    public GameObject SettingPanel;    
    public GameObject PanelVolume;     
    public GameObject PanelOther;      

    [Header("밝기 조절")]
    public Image BrightnessOverlay;
    public Slider BrightnessSlider;

    [Header("사운드 조절")]
    public Slider SoundSlider;

    private void Start()
    {
        InitializeSettings();

        // 초기 UI 상태 설정
        if (PanelVolume != null) PanelVolume.SetActive(false);
        if (PanelOther != null) PanelOther.SetActive(false);
        if (SettingPanel != null) SettingPanel.SetActive(false);
       

        ShowVolumeTab();
    }

    public void ShowVolumeTab()
    {
        if (PanelVolume != null) PanelVolume.SetActive(true);
        if (PanelOther != null) PanelOther.SetActive(false);
    }

    public void ShowOtherTab()
    {
        if (PanelVolume != null) PanelVolume.SetActive(false);
        if (PanelOther != null) PanelOther.SetActive(true);

        PanelOther.transform.SetAsLastSibling();
    }

    public void SetBrightness(float value)
    {
        if (BrightnessOverlay != null)
        {
            Color color = BrightnessOverlay.color;
            color.a = value;
            BrightnessOverlay.color = color;
        }
    }

    public void SetSound(float value) => AudioListener.volume = value;

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("SavedBrightness", BrightnessSlider.value);
        PlayerPrefs.SetFloat("SavedSound", SoundSlider.value);
    
        PlayerPrefs.Save();
      
    }

    public void OpenSettingPanel()
    {
        if (SettingPanel != null)
        {
            SettingPanel.SetActive(true);
        }
    }

    public void CloseSettingPanel()
    {
        if (SettingPanel != null)
        {
            SettingPanel.SetActive(false);
      
        }
    }


    public void GameExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void InitializeSettings()
    {
        float savedBrightness = PlayerPrefs.GetFloat("SavedBrightness", 0f);
        float savedSound = PlayerPrefs.GetFloat("SavedSound", 1.0f);
     

        if (BrightnessSlider != null)
        {
            BrightnessSlider.value = savedBrightness;
            BrightnessSlider.onValueChanged.RemoveAllListeners();
            BrightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        if (SoundSlider != null)
        {
            SoundSlider.value = savedSound;
            SoundSlider.onValueChanged.RemoveAllListeners();
            SoundSlider.onValueChanged.AddListener(SetSound);
        }

        SetBrightness(savedBrightness);
        SetSound(savedSound);
    }
}