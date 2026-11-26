using System;
using System.Linq;
using UnityEngine;

namespace ProjectG
{
    /// <summary>Reachable mission selection and read-only original-script review, separate from world completion.</summary>
    public sealed class FlightStoryMenu : MonoBehaviour
    {
        private bool visible;
        private FlightBroomPilot selectedPilot;
        private Vector2 scroll, scriptScroll;
        private FlightStoryStage selected;
        private string selectedScene;
        private GUIStyle body, title;
        public bool IsOpen => visible;

        public void Open(FlightBroomPilot pilot) { selectedPilot = pilot; visible = true; }
        public void Close() { visible = false; }

        private void OnGUI()
        {
            if (!visible) return;
            FlightStoryCatalog catalog = FlightStorySelection.Catalog;
            if (body == null) { body = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 18 }; title = new GUIStyle(body) { fontSize = 24, fontStyle = FontStyle.Bold }; }
            float width = Mathf.Min(Screen.width - 40, 1200), height = Mathf.Min(Screen.height - 40, 850);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 20, width, height), GUI.skin.box);
            GUILayout.BeginHorizontal(); GUILayout.Label("프로젝트 G · 본편 임무", title);
            if (GUILayout.Button("닫기", GUILayout.Width(80))) Close();
            GUILayout.EndHorizontal();
            if (catalog == null) { GUILayout.Label("시나리오를 불러오지 못했습니다.", body); GUILayout.EndArea(); return; }
            FlightStoryProgressData progress = FlightProgress.Current.StoryProgress;
            GUILayout.BeginHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(310));
            foreach (FlightStoryStage stage in catalog.stages)
            {
                bool unlocked = FlightStorySelection.IsUnlocked(stage, progress);
                string state = stage.missionIds.All(x => progress.CompletedMissions.Contains(x)) ? "완료" : unlocked ? "진행 가능" : "잠김";
                if (GUILayout.Button(stage.chapter + " · " + stage.title + "\n" + state, GUILayout.MinHeight(52)))
                { selected = stage; selectedScene = stage.sceneIds[0]; scriptScroll = Vector2.zero; }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginVertical();
            if (selected == null) GUILayout.Label("임무를 선택해 목표와 시나리오를 확인하세요.", body);
            else
            {
                GUILayout.Label(selected.title, title);
                GUILayout.Label("시나리오 열람은 임무 완료나 전투 보상을 발생시키지 않습니다.", body);
                GUILayout.BeginHorizontal();
                foreach (string id in selected.sceneIds)
                    if (GUILayout.Button(id)) { selectedScene = id; scriptScroll = Vector2.zero; }
                GUILayout.EndHorizontal();
                scriptScroll = GUILayout.BeginScrollView(scriptScroll);
                FlightStoryScene scene = catalog.Scene(selectedScene);
                GUILayout.Label(scene.title + " · " + scene.location, title);
                GUILayout.Label(catalog.Objective(selectedScene).goal, body);
                foreach (FlightStoryLine line in catalog.lines.Where(x => x.sceneId == selectedScene))
                    GUILayout.Label((string.IsNullOrWhiteSpace(line.speaker) ? "" : line.speaker + ": ") + line.text, body);
                GUILayout.EndScrollView();
                bool unlocked = FlightStorySelection.IsUnlocked(selected, progress);
                FlightStoryStageBinding prefab = Resources.Load<GameObject>("StoryBindings/" + selected.id)?.GetComponent<FlightStoryStageBinding>();
                bool authored = prefab != null && prefab.Validate(catalog).Length == 0;
                bool canLaunch = unlocked && authored && !selected.optional;
                GUI.enabled = canLaunch;
                if (GUILayout.Button(canLaunch ? "선택한 임무 시작" : unlocked ? "장면 준비 중" : "선행 임무를 완료해 주세요", GUILayout.Height(42)))
                {
                    FlightStoryPlayMode mode = selected.missionIds.All(x => progress.CompletedMissions.Contains(x)) ? FlightStoryPlayMode.StoryReplay : FlightStoryPlayMode.Story;
                    if (FlightStorySelection.Request(selected.id, selectedPilot, mode)) Close();
                }
                GUI.enabled = true;
                if (selected.optional && progress.OperationClear && unlocked && !FlightStorySelection.IsUrgent(progress) && GUILayout.Button("이 장면을 회상 기록에 표시"))
                    FlightProgress.Current.SaveStoryCheckpoint(new FlightStoryCheckpoint { MissionId = selected.missionIds[0], SeenScenes = new[] { selectedScene }, RecollectionOnly = true });
                if (selected.id == "M-E-1" && selectedScene == "E-S05" && progress.CompletedMissions.Contains("M-E-1") && GUILayout.Button("선택 후일담을 읽음으로 표시"))
                    FlightProgress.Current.SaveStoryCheckpoint(new FlightStoryCheckpoint { MissionId = "M-E-1", SeenScenes = new[] { "E-S05" }, RecollectionOnly = true });
            }
            GUILayout.EndVertical(); GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
    }
}
