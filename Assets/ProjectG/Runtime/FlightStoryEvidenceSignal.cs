using UnityEngine;

namespace ProjectG
{
    /// <summary>An authored source publishes a measured/action outcome explicitly; enabling it publishes nothing.</summary>
    public sealed class FlightStoryEvidenceSignal : MonoBehaviour
    {
        [SerializeField] private string sceneId;
        [SerializeField] private string factId;
        private FlightStoryDirector director;
        private string runId;

        public string SceneId => sceneId;
        public string FactId => factId;
        public string BoundRunId => runId;

        public void Configure(string scene, string fact)
        {
            sceneId = scene;
            factId = fact;
            director = null;
            runId = null;
        }

        public string ValidateConfiguration()
        {
            if (!FlightStoryValue.Id(sceneId)) return "An evidence source needs an approved scene ID.";
            if (!FlightStoryValue.Id(factId)) return "An evidence source needs an approved fact ID.";
            return string.Empty;
        }

        public bool Bind(FlightStoryDirector owner)
        {
            director = null;
            runId = null;
            if (owner == null || owner.Attempt == null || ValidateConfiguration().Length != 0) return false;
            director = owner;
            runId = owner.Attempt.RunId;
            return true;
        }

        public bool SetPresent(string capturedRunId, bool present)
        {
            if (capturedRunId != runId || !isActiveAndEnabled || !FlightStorySignals.Current(director, capturedRunId)) return false;
            return director.ReportEvidence(runId, sceneId, factId, present);
        }
    }
}
