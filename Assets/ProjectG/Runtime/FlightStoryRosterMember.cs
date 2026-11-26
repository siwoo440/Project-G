using UnityEngine;

namespace ProjectG
{
    public sealed class FlightStoryRosterMember : MonoBehaviour
    {
        [SerializeField] private string entityId;
        private FlightStoryDirector director;
        private string runId;

        public string EntityId => entityId;
        public string BoundRunId => runId;

        public void Configure(string identity)
        {
            entityId = identity;
            director = null;
            runId = null;
        }

        public string ValidateConfiguration() => FlightStoryValue.Id(entityId) ? string.Empty : "A roster actor needs an approved stable entity ID.";

        public bool Bind(FlightStoryDirector owner)
        {
            director = null;
            runId = null;
            if (owner == null || owner.Attempt == null || ValidateConfiguration().Length != 0) return false;
            director = owner;
            runId = owner.Attempt.RunId;
            return true;
        }

        public bool ReportDeparture(FlightStoryDirector owner, string capturedRunId)
        {
            if (!isActiveAndEnabled || owner == null || capturedRunId != runId) return false;
            return BoundTo(owner, capturedRunId) && owner.ReportDeparture(capturedRunId, entityId);
        }

        internal bool BoundTo(FlightStoryDirector owner, string currentRunId)
            => isActiveAndEnabled && director == owner && runId == currentRunId && FlightStorySignals.Current(owner, runId) && FlightStoryValue.Id(entityId);
    }
}
