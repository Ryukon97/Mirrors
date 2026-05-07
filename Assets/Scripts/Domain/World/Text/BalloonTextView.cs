using TMPro;
using UnityEngine;

public class BalloonTextView : TextView
{
    [SerializeField] float duration;
    [SerializeField] Vector3 offset;

    BalloonTextInstance balloonInstance;
    float viewRequestedTime;
    public override void InitView()
    {

    }
    private void Update()
    {
        if(balloonInstance != null && balloonInstance.gameObject.activeSelf && Time.time - viewRequestedTime > duration)
        {
            balloonInstance.gameObject.SetActive(false);
        }
    }
    public override void UpdateView(string text)
    {
        if (balloonInstance == null)
        {
            balloonInstance = TextInstancePool.Instance.GetBalloonTextInstance();
            balloonInstance.target = gameObject.transform;
            balloonInstance.transform.position = gameObject.transform.position + offset;
            balloonInstance.gameObject.SetActive(false);
        }

        if (balloonInstance.gameObject.activeSelf == false)
            balloonInstance.gameObject.SetActive(true);

        balloonInstance.tmpro.text = text;
        viewRequestedTime = Time.time;
    }
}
