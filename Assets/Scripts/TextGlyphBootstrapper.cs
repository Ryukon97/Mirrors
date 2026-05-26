using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TextGlyphBootstrapper : MonoBehaviour
{
    static TextGlyphBootstrapper instance;

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    private void OnSceneUnloaded(Scene scene)
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
            font.ClearFontAssetData(setAtlasSizeToZero: false);
    }
}
