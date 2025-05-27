using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Collider))]
public class EnemySearch : MonoBehaviour
{
    [SerializeField] private UnityEvent<Collider> onTriggerStayEvent = new UnityEvent<Collider>();

    [SerializeField] private UnityEvent<Collider> onTriggerExitEvent = new UnityEvent<Collider>();

    // Collider�͈͓̔��̍s��
    private void OnTriggerStay(Collider other)
    {
        onTriggerStayEvent.Invoke(other);
    }

    // Collider�͈̔͊O�̍s��
    private void OnTriggerExit(Collider other)
    {
        onTriggerExitEvent.Invoke(other);
    }
}