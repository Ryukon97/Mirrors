using UnityEngine;

public class BalloonTextView : TextView
{
    [SerializeField] float duration;
    [SerializeField] Vector3 offset;

    BalloonTextInstance balloonInstance;
    float viewRequestedTime;
    public override void InitView()
    {
        balloonInstance = TextInstancePool.Instance.GetBalloonTextInstance();
        balloonInstance.gameObject.SetActive(false);
    }
    public override void HideView()
    {
        if (balloonInstance != null && balloonInstance.gameObject.activeSelf)
        {
            balloonInstance.gameObject.SetActive(false);
        }
    }
    private void Update()
    {
        if(balloonInstance != null && balloonInstance.gameObject.activeSelf && Time.time - viewRequestedTime > duration)
        {
            balloonInstance.gameObject.SetActive(false);
        }
    }
    public override void UpdateView(string[] text)
    {
        if (balloonInstance.gameObject.activeSelf == false)
            balloonInstance.gameObject.SetActive(true);

        balloonInstance.tmpro.text = text[0];
        viewRequestedTime = Time.time;
    }

    public override void UpdateViewOwner(TextController ownerController)
    {
        balloonInstance.transform.position = ownerController.transform.position + offset;
        balloonInstance.transform.rotation.SetLookRotation(ownerController.transform.forward);
    }
}
