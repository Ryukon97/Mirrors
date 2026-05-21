using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class DoCallbackOnTriggerEnter : MonoBehaviour
{
    [SerializeField] private UnityEvent onTriggered;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
            onTriggered?.Invoke();
    }
}
