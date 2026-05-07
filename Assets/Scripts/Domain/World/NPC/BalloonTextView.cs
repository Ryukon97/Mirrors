using TMPro;
using UnityEngine;

public class BalloonTextView : TextView
{
    static Transform worldCanvasTransform;
    [SerializeField] GameObject balloonPrefab;
    BalloonTextInstance balloonInstance;
    [SerializeField] float duration;
    [SerializeField] Vector3 offset;
    float viewRequestedTime;
    private void Awake()
    {
        if(worldCanvasTransform == null)
        {
            worldCanvasTransform = GameObject.Find("WorldCanvas").transform;
        }
    }
    public override void InitView()
    {
        balloonInstance = Instantiate(balloonPrefab, worldCanvasTransform).GetComponent<BalloonTextInstance>();
        balloonInstance.target = gameObject.transform;
        balloonInstance.transform.position = gameObject.transform.position + offset;
        balloonInstance.gameObject.SetActive(false);
    }
    private void Update()
    {
        if(balloonInstance.gameObject.activeSelf && Time.time - viewRequestedTime > duration)
        {
            balloonInstance.gameObject.SetActive(false);
        }
    }
    public override void UpdateView(string text)
    {
        if(balloonInstance.gameObject.activeSelf == false)
            balloonInstance.gameObject.SetActive(true);
        balloonInstance.tmpro.text = text;
        viewRequestedTime = Time.time;
    }
}
