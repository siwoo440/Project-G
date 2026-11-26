using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectG
{
    public sealed class FlightStoryHud : MonoBehaviour
    {
        private FlightStoryDirector director;
        public void Bind(FlightStoryDirector value) { director = value; }
        private void Update()
        {
            if (director != null && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) director.RequestAdvance();
        }
        private void OnGUI()
        {
            if (director == null || director.Finished) return;
            if (!director.Ready)
            {
                GUI.Box(new Rect(20, 160, Mathf.Min(560, Screen.width - 40), 80), string.IsNullOrEmpty(director.Error) ? "임무 준비 중" : director.Error);
                return;
            }
            FlightStoryObjective objective = director.Catalog.Objective(director.Attempt.CurrentSceneId);
            GUI.Box(new Rect(20, 160, Mathf.Min(560, Screen.width - 40), 120), GUIContent.none);
            GUI.Label(new Rect(32, 170, 530, 32), director.Catalog.Scene(objective.sceneId).title);
            var body = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16 };
            GUI.Label(new Rect(32, 204, 530, 70), string.IsNullOrEmpty(director.Error) ? objective.goal + "\n현장 확인 후 F8로 진행" : director.Error, body);
        }
    }
}
