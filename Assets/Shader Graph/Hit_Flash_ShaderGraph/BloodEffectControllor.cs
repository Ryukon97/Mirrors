using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BloodEggectControllor : MonoBehaviour
{
    private List<Material> targetMaterials = new List<Material>();
    private readonly string propertyName = "_HitFlashAmount";

    [Header("Effect Setting")]
    public float flashIntensity = 0.8f;
    public float duration = 0.4f;

    private Coroutine Hit_Effect_Coroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            targetMaterials.AddRange(renderer.materials);
        }
    }

    [ContextMenu("Test Hit")]

    public void OnHit()
    {
        if(Hit_Effect_Coroutine != null) StopCoroutine(Hit_Effect_Coroutine);
        Hit_Effect_Coroutine = StartCoroutine (PlayHitEffect());
    }

    private IEnumerator PlayHitEffect()
    {
        float elapsedTime = 0f;

        while(elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float ratio = elapsedTime / duration;

            float CurrentIntensity = Mathf.Lerp(flashIntensity, 0, ratio);

            foreach (Material material in targetMaterials)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, CurrentIntensity);
                }
            }
            yield return null;
        }
        SetAllMaterialsIntensity(0f);
    }

    private void SetAllMaterialsIntensity (float Value)
    {
        foreach(Material material in targetMaterials)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, Value);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
      
    }
}
