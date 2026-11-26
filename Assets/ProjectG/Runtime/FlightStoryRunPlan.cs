using System.Linq;

namespace ProjectG
{
    public static class FlightStoryRunPlan
    {
        public static string[] RequiredScenes(FlightStoryStage stage)
        {
            // E-S05 is an optional future-story record. Declining it cannot turn the ending into failure.
            return stage.sceneIds.Where(id => stage.id != "M-E-1" || id != "E-S05").ToArray();
        }
    }
}
