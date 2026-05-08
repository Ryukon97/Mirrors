using System;
using TMPro;
using UnityEngine;

public class BalloonTextInstance : MonoBehaviour
{
    public TextMeshProUGUI tmpro;
    public bool applyTargetRotation;
    Transform target;
    private void Awake()
    {
        target = Camera.main.transform;
    }
    private void Update()
    {
        if (target == null) return;
        if (applyTargetRotation)
        {
            transform.rotation = Quaternion.Euler(target.forward);
        }
        else
        {
            transform.rotation = Quaternion.Euler(Camera.main.transform.forward);
        }
    }
}
