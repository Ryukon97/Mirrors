using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class FadeInOutManager : MonoBehaviour
{
    static FadeInOutManager instance;
    public static FadeInOutManager Instance { get => instance; }

    [SerializeField] float fadeInDuration;
    [SerializeField] float fadeOutDuration;
    [SerializeField] Image fadeImg;
    private void Awake()
    {
        if(instance != null)
        {
            Destroy(instance.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        fadeImg.gameObject.SetActive(false);
    }

    // Stash Awaitable return value and DO NOT re-use Awaitable instance. May occur silent errors.
    // 기다릴 필요 없다면 _ = DoSomething...Async() 과 같이 사용하여 제거하세요. 반환값 쓰면 안됩니다 절대.  
    // 기다려야한다면 await DoSomething...Async() 과 같이 사용하세요. 
    public async Awaitable DoSomethingBtwFadingAsync(Action something)
    {
        if (fadeImg.gameObject.activeSelf) return;
        try
        {
            fadeImg.gameObject.SetActive(true);
            await fadeInAsync();
            something();
            await fadeOutAsync();
            fadeImg.gameObject.SetActive(false);
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Awaitable fadeInAsync()
    {
        float value = 0;
        while(value < 1)
        {
            fadeImg.material.SetFloat("_FadeStep01", value);
            await Awaitable.NextFrameAsync();
            value += Time.unscaledDeltaTime / fadeInDuration;
        }
    }
    private async Awaitable fadeOutAsync()
    {
        float value = 1;
        while (value > 0)
        {
            fadeImg.material.SetFloat("_FadeStep01", value);
            await Awaitable.NextFrameAsync();
            value -= Time.unscaledDeltaTime / fadeOutDuration;
        }
    }
}
