using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

public class CutScene : MonoBehaviour
{
    [Header("Cut Scene Elements")]
    public PlayableDirector playableDirector;
    public CinemachineCamera[] CutSceneCamera;

  
    public void UltimateSkill()
    {
      

     
        this.gameObject.SetActive(true);

        Invoke(nameof(PlayTimelineDelayed), 0.05f);
    }

    private void PlayTimelineDelayed()
    {
        foreach (var cam in CutSceneCamera)
        {
            if (cam != null) cam.Priority = 20;
        }

        playableDirector.stopped -= OnCutsceneEnded;
        playableDirector.stopped += OnCutsceneEnded;

        playableDirector.Play();
       
    }

    private void OnCutsceneEnded(PlayableDirector director)
    {
   

        foreach (var cam in CutSceneCamera)
        {
            if (cam != null) cam.Priority = 0;
        }

        this.gameObject.SetActive(false);
        
    }
}