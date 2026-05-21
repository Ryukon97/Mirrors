using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;
public class CutScene : MonoBehaviour
{
    [Header("Cut Scene")]
    public PlayableDirector playableDirector;
    public CinemachineCamera[] CutSceneCamera;

    public void UltimateSkill() // 컷씬 호출
    {
        foreach (var cam in CutSceneCamera)
        {
            if (cam != null)
            {
                cam.Priority = 20;
            }
        }
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnCutsceneEnded;
            playableDirector.stopped += OnCutsceneEnded;
            playableDirector.Play();
        }
    }
    private void OnCutsceneEnded(PlayableDirector director) // 컷씬 종료 호출
    {
        foreach (var cam in CutSceneCamera)
        {
            if (cam != null)
            {
                cam.Priority = 0;
            }
        }
    }
}
