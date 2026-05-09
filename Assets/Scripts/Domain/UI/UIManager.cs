using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
  
    public GameObject TitlePanel;
    public GameObject MenuPanel;
    public GameObject CharacterSelectPanel;


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
        CharacterSelectPanel.SetActive(true);
    }

    public void SelectAndStart(int CharID) //캐릭터 선택 부분
    {
        PlayerPrefs.SetInt("SelectedCharacterID", CharID);
        PlayerPrefs.Save();

        FindAnyObjectByType<ScenceManager>().NextSence(); // Find형식을 썼음 차후 최적화 생각중
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