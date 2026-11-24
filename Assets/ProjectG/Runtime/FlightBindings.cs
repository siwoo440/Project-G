using System; // 변경 알림과 슬롯 이름
using UnityEngine; // 씬에서 편집하는 조작 설정
using UnityEngine.InputSystem; // 실제 키 입력

namespace ProjectG // 프로젝트 게임 영역
{
    public enum FlightAction // 설정과 게임이 공유하는 조작
    {
        Forward, Left, Back, Right, Rise, Descend, Focus, Special, Common, Ultimate, Trial, Wave, Pilot // 키 설명과 설정 표시 순서
    }

    public sealed class FlightBindings : MonoBehaviour // 실제 키와 표시의 단일 설정
    {
        private static readonly Key[] Defaults = // 처음 설정한 키 차례
        {
            Key.W, Key.A, Key.S, Key.D, Key.Space, Key.LeftCtrl, Key.LeftShift, // 비행
            Key.Q, Key.E, Key.R, Key.T, Key.Y, Key.F2 // 스킬과 시험 조작
        };
        private readonly Key[] bindings = (Key[])Defaults.Clone(); // 현재 사용자의 입력 키
        public event Action Changed; // 조작 그림과 HUD 키 표기의 동시 갱신

        internal static bool UsesPreferences => !Application.isBatchMode && Environment.GetEnvironmentVariable("PROJECTG_CAPTURE_NEW3D") != "1"; // 격리 검증과 실제 설정 분리

        private void Awake() // 실제 출격에서 이전 조작 설정 읽기
        {
            if (!UsesPreferences) // 독립 검증은 기본 키 사용
            {
                return; // 사용자 설정을 시험에 사용하지 않음
            }
            for (int index = 0; index < bindings.Length; index++) // 키별 저장값 적용
            {
                Key value = (Key)PlayerPrefs.GetInt(PrefName(index), (int)Defaults[index]); // 설정한 키 저장값
                bindings[index] = IsSupported(value) ? value : Defaults[index]; // 현재 그림에서 표시 가능한 키 사용
            }
        }

        public Key GetKey(FlightAction action) // 현재 조작의 물리 키
        {
            return bindings[(int)action]; // 저장된 조작 설정
        }

        public bool TrySet(FlightAction action, Key key) // 해당 키로 실제 조작 교체
        {
            int index = (int)action; // 현재 조작의 슬롯
            key = Normalize(key); // 양쪽 보조키를 같은 조작으로 표시
            int previous = Array.IndexOf(bindings, key); // 기존 조작과의 중복 확인
            if (!IsSupported(key) || (previous >= 0 && previous != index)) // 예약 또는 이미 사용 중인 키 제외
            {
                return false; // 기존 키 유지
            }

            bindings[index] = key; // 실제 비행과 스킬의 변경 반영
            if (UsesPreferences) // 사용자 설정 보존
            {
                PlayerPrefs.SetInt(PrefName(index), (int)key); // 적용한 키 저장
                PlayerPrefs.Save(); // 게임 재실행에도 키 복원
            }
            Changed?.Invoke(); // 그림과 설명에 즉시 반영
            return true; // 변경 완료
        }

        public void ResetBindings() // 처음 조작 방식으로 되돌리기
        {
            Array.Copy(Defaults, bindings, bindings.Length); // 다른 화면과 같은 기본 키
            if (UsesPreferences) // 실제 설정을 기본값으로 저장
            {
                for (int index = 0; index < bindings.Length; index++) // 개별 이전 설정 삭제
                {
                    PlayerPrefs.DeleteKey(PrefName(index)); // 기본값을 다시 사용
                }
                PlayerPrefs.Save(); // 재실행에서도 초기 복원
            }
            Changed?.Invoke(); // 설명 그림과 HUD 새로 고침
        }

        public bool Held(Keyboard device, FlightAction action) // 누르는 동안 처리하는 비행 입력
        {
            Key key = GetKey(action); // 활성 키
            if (key == Key.LeftShift) // 두 Shift 중 하나
            {
                return device.leftShiftKey.isPressed || device.rightShiftKey.isPressed; // 키보드 양쪽의 집중
            }
            if (key == Key.LeftCtrl) // 두 Ctrl 중 하나
            {
                return device.leftCtrlKey.isPressed || device.rightCtrlKey.isPressed; // 키보드 양쪽의 하강
            }
            return device[key].isPressed; // 설정한 일반 키
        }

        public bool Pressed(Keyboard device, FlightAction action) // 누른 프레임에 발동하는 입력
        {
            Key key = GetKey(action); // 실제 선택된 키
            if (key == Key.LeftShift) // 양쪽 Shift 지원
            {
                return device.leftShiftKey.wasPressedThisFrame || device.rightShiftKey.wasPressedThisFrame; // 해당 키 누른 순간
            }
            if (key == Key.LeftCtrl) // 양쪽 Ctrl 지원
            {
                return device.leftCtrlKey.wasPressedThisFrame || device.rightCtrlKey.wasPressedThisFrame; // 해당 키 누른 순간
            }
            return device[key].wasPressedThisFrame; // 설정한 키 누른 순간
        }

        public static string Label(Key key) // 범용 키캡과 스킬의 짧은 이름
        {
            return Normalize(key) switch // 양쪽 보조키를 짧게 표기
            {
                Key.LeftShift => "Shift", // 집중 또는 새로 묶은 동작
                Key.LeftCtrl => "Ctrl", // 하강 또는 새로 묶은 동작
                _ => key.ToString() // 영문자·Space·F2
            };
        }

        internal static bool IsSupported(Key key) // 평면 키보드에 표시 가능한 범위
        {
            return (key >= Key.A && key <= Key.Z) || key == Key.Space || key == Key.LeftShift || key == Key.LeftCtrl || key == Key.F2; // 숫자와 ESC·BKSP는 메뉴용 고정 키
        }

        internal static Key Normalize(Key key) // 보조키 두 위치의 동일한 의미
        {
            return key == Key.RightShift ? Key.LeftShift : (key == Key.RightCtrl ? Key.LeftCtrl : key); // 사용 아이콘과 실제 키 동일 취급
        }

        private static string PrefName(int index) // 이 출격의 조작 저장 위치
        {
            return $"ProjectG.Flight.Binding.{index}"; // 설정별 독립 키
        }
    }
}
