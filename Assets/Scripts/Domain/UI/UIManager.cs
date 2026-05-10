using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
  
    public GameObject TitlePanel;
    public GameObject MenuPanel;
    public GameObject playerSelectionPanel;


    public SceneManager scenceManager; //인스펙터에 오픈월드관련을 드래그해서 불러오기 Find같이 일일히 찾기보다 찾아주는 역할
    public InputAction AnyKeyAction;

    
    private bool isTitleActive = true;

  
    private void OnEnable()
    {
        
        AnyKeyAction.Enable();
      
        AnyKeyAction.performed += OnAnyKeyPressed;
    }

    private void OnDisable()
    {
        
        AnyKeyAction.performed -= OnAnyKeyPressed;
        AnyKeyAction.Disable();
    }

    private void Start()
    {
        TitlePanel.SetActive(true);
        MenuPanel.SetActive(false);
    }

  
    public void OnClickStart()
    {
        MenuPanel.SetActive(false);
        playerSelectionPanel.SetActive(true);
       
    }

    public void SelectAndStart(int CharID) //캐릭터 선택 부분
    {
        PlayerPrefs.SetInt("SelectedCharacterID", CharID);
        PlayerPrefs.Save();

        SceneManager.Instance.NextSence(); // 캐릭터 선택후 넘어가는 싱글톤 오픈월드로 넘어감
    }

    public void OnClickExit()
    {
        Debug.Log("게임을 종료합니다");
        Application.Quit();
    }

   
    private void OnAnyKeyPressed(InputAction.CallbackContext context)
    {
        if (isTitleActive)
        {
            ShowMenu();
        }
    }

    private void ShowMenu()
    {
        isTitleActive = false;
        TitlePanel.SetActive(false);
        MenuPanel.SetActive(true);
    }
}