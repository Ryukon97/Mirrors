using UnityEngine;

public class WorldCompassRotator : MonoBehaviour
{
    public Transform playerTransform;

    // Update is called once per frame
    void Update()
    {
        var dir = playerTransform.forward;
        dir.y = 0;
        dir.Normalize();
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(dir.x, dir.z));
    }
}
