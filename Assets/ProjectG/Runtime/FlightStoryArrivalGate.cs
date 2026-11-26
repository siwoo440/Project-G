using UnityEngine;

namespace ProjectG
{
    /// <summary>Only an explicitly identified, bound roster actor can confirm this authored destination.</summary>
    public sealed class FlightStoryArrivalGate : MonoBehaviour
    {
        [SerializeField] private string destinationId;
        private FlightStoryDirector director;
        private string runId;

        public string DestinationId => destinationId;
        public string BoundRunId => runId;

        public void Configure(string destination)
        {
            destinationId = destination;
            director = null;
            runId = null;
        }

        public string ValidateConfiguration() => FlightStoryValue.Id(destinationId) ? string.Empty : "An arrival gate needs an approved safe destination ID.";

        public bool Bind(FlightStoryDirector owner)
        {
            director = null;
            runId = null;
            if (owner == null || owner.Attempt == null || ValidateConfiguration().Length != 0) return false;
            director = owner;
            runId = owner.Attempt.RunId;
            return true;
        }

        public bool TryArrive(FlightStoryRosterMember member)
        {
            if (!isActiveAndEnabled || !FlightStorySignals.Current(director, runId) || member == null || !member.BoundTo(director, runId)) return false;
            return director.ReportArrival(runId, member.EntityId, destinationId);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null) TryArrive(other.GetComponentInParent<FlightStoryRosterMember>());
        }
    }
}
