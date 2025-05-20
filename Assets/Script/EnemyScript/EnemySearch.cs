using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Collider))]
public class EnemySearch : MonoBehaviour
{
    [SerializeField] private UnityEvent<Collider> onTriggerStayEvent = new UnityEvent<Collider>();

    [SerializeField] private UnityEvent<Collider> onTriggerExitEvent = new UnityEvent<Collider>();

    private void OnTriggerStay(Collider other)
    {
        onTriggerStayEvent.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExitEvent.Invoke(other);
    }
}