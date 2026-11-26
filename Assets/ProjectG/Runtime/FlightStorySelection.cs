using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectG
{
    internal sealed class FlightStoryRequest
    {
        internal string StageId;
        internal FlightStoryPlayMode Mode;
    }

    internal static class FlightStorySelection
    {
        internal static FlightStoryRequest Pending { get; private set; }
        private static FlightStoryCatalog catalog;
        internal static FlightStoryCatalog Catalog
        {
            get
            {
                if (catalog != null) return catalog;
                TextAsset asset = Resources.Load<TextAsset>("Story/ProjectGStory");
                if (asset == null) return null;
                FlightStoryCatalog candidate = JsonUtility.FromJson<FlightStoryCatalog>(asset.text);
                if (candidate == null || candidate.Validate().Length != 0) return null;
                catalog = candidate;
                return catalog;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Pending = null; catalog = null; }

        internal static bool IsUnlocked(FlightStoryStage stage, FlightStoryProgressData progress)
        {
            if (stage == null || progress == null || Catalog == null) return false;
            if (stage.optional)
            {
                int chapter = SideChapter(stage.id);
                return chapter != 0 && progress.CompletedChapters.Contains("C" + chapter.ToString("00"))
                    && progress.SceneEvents.Contains("EV_C" + chapter.ToString("00") + "_S07_COMPLETE");
            }
            FlightStoryStage[] main = Catalog.stages.Where(x => !x.optional).ToArray();
            int index = Array.FindIndex(main, x => x.id == stage.id);
            if (index < 0) return false;
            if (index == 0) return true;
            return main[index - 1].missionIds.All(id => progress.CompletedMissions.Contains(id));
        }

        internal static bool IsUrgent(FlightStoryProgressData progress)
            => !progress.OperationClear && progress.SceneEvents.Any(x => x.StartsWith("EV_C07_", StringComparison.Ordinal) || x.StartsWith("EV_C08_", StringComparison.Ordinal));

        internal static bool Request(string stageId, FlightBroomPilot pilot, FlightStoryPlayMode mode)
        {
            FlightStoryStage stage = Catalog?.Stage(stageId);
            FlightStoryProgressData progress = FlightProgress.Current.StoryProgress;
            if (!IsUnlocked(stage, progress) || stage.optional) return false;
            FlightStoryStageBinding prefab = Resources.Load<GameObject>("StoryBindings/" + stageId)?.GetComponent<FlightStoryStageBinding>();
            if (prefab == null || prefab.Validate(Catalog).Length != 0) return false;
            Pending = new FlightStoryRequest { StageId = stageId, Mode = mode };
            FlightSortieChoice.Launch(pilot);
            SceneManager.LoadSceneAsync(FlightSortieChoice.StageScene);
            return true;
        }

        internal static void StartInScene(FlightEnemyEncounter encounter, FlightPilotVitals pilot, FlightThreatPool pool)
        {
            FlightStoryRequest request = Pending;
            Pending = null;
            if (request == null) return;
            FlightStoryStageBinding prefab = Resources.Load<GameObject>("StoryBindings/" + request.StageId)?.GetComponent<FlightStoryStageBinding>();
            if (prefab == null) { Debug.LogError("Story binding was not available at scene entry.", encounter); return; }
            FlightStoryStageBinding binding = UnityEngine.Object.Instantiate(prefab);
            FlightStoryDirector director = encounter.gameObject.AddComponent<FlightStoryDirector>();
            encounter.AttachStory(director);
            director.Initialize(Catalog, binding, encounter, pilot, pool, request.Mode, prefab);
            FlightStoryHud hud = encounter.gameObject.AddComponent<FlightStoryHud>();
            hud.Bind(director);
        }

        internal static int SideChapter(string id)
        {
            switch (id)
            {
                case "M-SC01": return 6;
                case "M-SC02": case "M-SR01": return 2;
                case "M-SC03": case "M-SC04": case "M-SC06": case "M-SR02": return 3;
                case "M-SC05": case "M-SR04": return 5;
                case "M-SC07": case "M-SR03": return 4;
                default: return 0;
            }
        }
    }
}
