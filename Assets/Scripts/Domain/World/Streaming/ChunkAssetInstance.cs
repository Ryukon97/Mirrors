using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

// Instantiated GameObject
public class ChunkAssetInstance
{
    public AsyncOperationHandle<GameObject> handle;
    public GameObject go;

    public ChunkAssetInstance(AsyncOperationHandle<GameObject> handle, GameObject go)
    {
        this.handle = handle;
        this.go = go;
    }
}