using UnityEngine;
using UnityEngine.InputSystem;

public class TitleManager : MonoBehaviour
{
  
    public GameObject TitlePanel;
    public GameObject MenuPanel;

   
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

  
    public void OnClickStart() => Debug.Log("게임을 시작합니다");

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