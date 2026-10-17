using UnityEngine;

namespace PassportFrenzy
{
    public enum FlowTriggerId { HomeDoor, AirportEntrance, Gate }

    /// <summary>Trigger volume that tells GameFlow the player reached a story beat (door, airport, gate).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class FlowTrigger : MonoBehaviour
    {
        public FlowTriggerId id;

        bool _fired;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<RunnerController>() == null) return;
            _fired = true;
            GameRoot.I.flow.OnFlowTrigger(id);
        }
    }
}
