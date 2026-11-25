using System; // 열거형 항목 수
using System.Collections.Generic; // 고유 ID 검색
using UnityEngine; // 비행체와 부품 렌더러

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 비행체별 장착 구성 한 개
    public sealed class FlightBroomEquipment : MonoBehaviour // 보유 부품과 캐릭터별 조합 관리
    {
        private sealed class OwnedPart // 같은 종류도 분리한 개별 보유 부품
        {
            public string Id; // 종류와 개체 순번을 포함한 ID
            public FlightBroomPart Definition; // 같은 종류가 공유하는 효과와 외형
            public int Grade; // 개별 등급
            public int Upgrade; // 개별 강화 단계
            public int Owner = -1; // 미장착 또는 장착 캐릭터 번호
        }

        [SerializeField] private FlightBroomCatalog catalog; // 씬에 연결한 부품 목록
        [SerializeField] private MeshRenderer[] mounts; // 슬롯 순서의 씬 렌더러
        private readonly Dictionary<string, OwnedPart> owned = new(); // 개별 보유 ID로 빠른 조회
        private readonly OwnedPart[,] equipped = new OwnedPart[Enum.GetValues(typeof(FlightBroomPilot)).Length, Enum.GetValues(typeof(FlightBroomSlot)).Length]; // 캐릭터별 슬롯
        private readonly float[] bonuses = new float[Enum.GetValues(typeof(FlightBroomStat)).Length]; // 활성 조합의 효과 합계
        private Material[] defaultMaterials; // 부착점 기본 재질
        private Vector3[] defaultScales; // 부착점 기본 크기
        private const string LoadoutKey = "ProjectG.Flight.Broom.Loadouts.v1"; // 버전별 현재 장착 저장 키

        [Serializable] // 캐릭터·슬롯 순서로 기록하는 저장 형식
        private sealed class LoadoutData
        {
            public string[] ids; // 선형 배열의 개별 장착 ID
        }

        internal readonly struct PartOption // 조립 화면에서 쓰는 읽기 전용 부품 정보
        {
            public readonly string Id; // 개별 보유 부품
            public readonly string Name; // 부품 목록 표시 이름
            public readonly int Grade; // 등급
            public readonly int Upgrade; // 강화 단계
            public readonly int Owner; // 장착 캐릭터 또는 미장착
            public readonly int Copies; // 같은 종류로 보유한 수량

            public PartOption(string id, string name, int grade, int upgrade, int owner, int copies) // 보유 부품의 화면 표시 정보
            {
                Id = id; // 보유 ID 기록
                Name = name; // 표시 이름 기록
                Grade = grade; // 등급 기록
                Upgrade = upgrade; // 강화 기록
                Owner = owner; // 현재 소유자 기록
                Copies = copies; // 복수 보유 여부 기록
            }
        }

        private static string SaveKey => Environment.GetEnvironmentVariable("PROJECTG_BROOM_TEST_KEY") ?? (FlightBindings.UsesPreferences ? LoadoutKey : null); // 실제 설정과 격리 검증 분리

        public int OwnedCount => owned.Count; // 전체 보유 부품 수
        public FlightBroomPilot ActivePilot { get; private set; } // 씬에서 조작하는 캐릭터

        private void Awake() // 씬 참조와 시작 장착 초기화
        {
            defaultMaterials = new Material[mounts.Length]; // 렌더러별 원본 재질 공간
            defaultScales = new Vector3[mounts.Length]; // 렌더러별 원본 크기 공간
            for (int i = 0; i < mounts.Length; i++) // 부착점 원본 기록
            {
                defaultMaterials[i] = mounts[i].sharedMaterial; // 적용 전 재질
                defaultScales[i] = mounts[i].transform.localScale; // 적용 전 크기
            }

            foreach (FlightBroomPart definition in catalog.Parts) // 카탈로그에서 시작 보유 부품 구성
            {
                for (int copy = 1; copy <= definition.ownedCopies; copy++) // 같은 종류의 개별 보유 수량
                {
                    OwnedPart item = new OwnedPart // 개별 부품의 시작 정보
                    {
                        Id = $"{definition.id}:{copy}", // 고정 종류 ID와 순번
                        Definition = definition, // 공통 부품 데이터
                        Grade = definition.grade, // 시작 등급
                        Upgrade = definition.upgrade // 시작 강화 단계
                    };
                    owned.Add(item.Id, item); // 개별 부품 등록
                    if (definition.starter && copy == 1) // 기본 조합에 첫 번째 부품 장착
                    {
                        Equip(item, (int)definition.pilot, (int)definition.slot); // 최초 캐릭터 장착
                    }
                }
            }

            RestoreLoadouts(); // 재실행 시 마지막으로 확정한 부품 적용
            ApplyLoadout(); // 첫 비행의 외형과 수치 적용
        }

        public bool TryEquip(FlightBroomPilot pilot, FlightBroomSlot slot, string ownedId) // 지정 부위로 개별 부품 교체
        {
            int pilotIndex = (int)pilot; // 캐릭터 배열 위치
            int slotIndex = (int)slot; // 부위 배열 위치
            if (!HasSlot(pilotIndex, slotIndex) || string.IsNullOrEmpty(ownedId) || !owned.TryGetValue(ownedId, out OwnedPart item)) // 유효한 캐릭터·슬롯·보유 ID 확인
            {
                return false; // 알 수 없는 부품 요청 거절
            }

            if (item.Definition.slot != slot || (item.Owner != -1 && item.Owner != pilotIndex)) // 다른 부위 또는 타 캐릭터 장착 여부
            {
                return false; // 슬롯 불일치와 중복 장착 방지
            }

            if (equipped[pilotIndex, slotIndex] == item) // 이미 같은 슬롯에 장착한 부품
            {
                return true; // 기존 부품과 표시 상태 유지
            }

            Equip(item, pilotIndex, slotIndex); // 이전 부품 해제와 신규 장착
            if (pilot == ActivePilot) // 현재 비행체의 변경만 출력
            {
                ApplyLoadout(); // 외형과 비행 수치 재계산
            }

            return true; // 장착 성공
        }

        public bool Unequip(FlightBroomPilot pilot, FlightBroomSlot slot) // 캐릭터의 슬롯 비우기
        {
            int pilotIndex = (int)pilot; // 캐릭터 배열 위치
            int slotIndex = (int)slot; // 부위 배열 위치
            if (!HasSlot(pilotIndex, slotIndex) || equipped[pilotIndex, slotIndex] == null) // 없는 캐릭터·부위 또는 빈 슬롯
            {
                return false; // 해제할 부품이 없는 요청 거절
            }

            Equip(null, pilotIndex, slotIndex); // 부품을 미장착으로 반환
            if (pilot == ActivePilot) // 현재 눈에 보이는 조합
            {
                ApplyLoadout(); // 제거된 부품의 외형과 수치 반영
            }

            return true; // 장착 해제 성공
        }

        public string GetEquippedId(FlightBroomPilot pilot, FlightBroomSlot slot) // 슬롯의 개별 부품 조회
        {
            int pilotIndex = (int)pilot; // 캐릭터 배열 위치
            int slotIndex = (int)slot; // 부위 배열 위치
            return HasSlot(pilotIndex, slotIndex) ? equipped[pilotIndex, slotIndex]?.Id : null; // 장착 ID 또는 빈 슬롯
        }

        public float GetBonus(FlightBroomStat stat) // 활성 조합의 수치 합
        {
            return bonuses[(int)stat]; // 개별 효과와 불이익의 합
        }

        internal string GetEquippedName(FlightBroomSlot slot) // 출격 요약의 실제 장착 이름
        {
            return equipped[(int)ActivePilot, (int)slot]?.Definition.displayName ?? "미장착"; // 준비 화면과 같은 장착 상태
        }

        internal void ListParts(FlightBroomSlot slot, List<PartOption> result) // 선택한 슬롯에 장착할 수 있는 종류 목록
        {
            result.Clear(); // 앞서 선택한 슬롯 결과 제거
            foreach (OwnedPart item in owned.Values) // 공용 인벤토리의 모든 보유 개체
            {
                if (item.Definition.slot == slot) // 현재 보고 있는 부위
                {
                    result.Add(new PartOption(item.Id, item.Definition.displayName, item.Grade, item.Upgrade, item.Owner, item.Definition.ownedCopies)); // 목록 행의 표시 정보
                }
            }
            result.Sort((left, right) => CompareOptions(left, right)); // 현재 장착·장착 가능·타 캐릭터 순서
        }

        private int CompareOptions(PartOption left, PartOption right) // 장착 상태와 보유 ID 기준 목록 정렬
        {
            int active = (int)ActivePilot; // 현재 캐릭터 번호
            int leftOrder = left.Owner == active ? 0 : left.Owner < 0 ? 1 : 2; // 왼쪽 부품의 장착 상태 순서
            int rightOrder = right.Owner == active ? 0 : right.Owner < 0 ? 1 : 2; // 오른쪽 부품의 장착 상태 순서
            return leftOrder == rightOrder ? string.CompareOrdinal(left.Id, right.Id) : leftOrder.CompareTo(rightOrder); // 동일 상태에서는 개별 ID 순서
        }

        internal bool ConfirmPart(FlightBroomSlot slot, string id) // 조립 화면에서 현재 조합 확정
        {
            bool applied = id == null ? Unequip(ActivePilot, slot) : TryEquip(ActivePilot, slot, id); // 같은 장착 규칙과 외형 적용
            if (applied) // 유효한 변경만 저장
            {
                SaveLoadouts(); // 게임 재시작 후 장착 복원
            }
            return applied; // 적용 결과 표시
        }

        internal float ComparePart(FlightBroomSlot slot, string id, FlightBroomStat stat) // 후보 장착 시 활성 조합과의 차이
        {
            OwnedPart prior = equipped[(int)ActivePilot, (int)slot]; // 현재 슬롯의 보유 부품
            OwnedPart candidate = id == null ? null : owned[id]; // 선택한 후보 또는 해제
            return Contribution(candidate, stat) - Contribution(prior, stat); // 이전 부품 제거와 새 부품 가산
        }

        internal void ShowPreview(MeshRenderer[] preview, FlightBroomSlot slot, string id) // 미리보기 전용 렌더러에 부품 외형 적용
        {
            for (int i = 0; i < preview.Length; i++) // 모든 미리보기 부착점
            {
                OwnedPart item = i == (int)slot ? (id == null ? null : owned[id]) : equipped[(int)ActivePilot, i]; // 현재 후보를 포함한 시험 조합
                ApplyAppearance(preview[i], item, i); // 본체를 건드리지 않는 외형 적용
            }
        }

        internal void SelectPilot(FlightBroomPilot pilot) // 기존 캐릭터 전환에 장착 조합 연결
        {
            ActivePilot = pilot; // 현재 캐릭터 갱신
            ApplyLoadout(); // 선택 캐릭터의 외형과 수치 복원
        }

        private bool HasSlot(int pilot, int slot) // 배열 경계 검사
        {
            return pilot >= 0 && pilot < equipped.GetLength(0) && slot >= 0 && slot < equipped.GetLength(1); // 지원하는 캐릭터와 부위
        }

        private void Equip(OwnedPart item, int pilot, int slot) // 단일 슬롯의 소유자 갱신
        {
            OwnedPart previous = equipped[pilot, slot]; // 기존 장착 부품
            if (previous != null) // 이전 부품 반환
            {
                previous.Owner = -1; // 다른 캐릭터에 장착 가능
            }

            equipped[pilot, slot] = item; // 새 부품 또는 빈 슬롯
            if (item != null) // 신규 부품 소유자 기록
            {
                item.Owner = pilot; // 현재 장착 캐릭터
            }
        }

        private void ApplyLoadout() // 활성 캐릭터의 부착점과 효과 갱신
        {
            Array.Clear(bonuses, 0, bonuses.Length); // 누적 없이 현재 조합 재계산
            int pilot = (int)ActivePilot; // 활성 캐릭터 배열 위치
            for (int slot = 0; slot < equipped.GetLength(1); slot++) // 전체 장착 슬롯 순회
            {
                OwnedPart item = equipped[pilot, slot]; // 슬롯의 개별 부품
                ApplyAppearance(mounts[slot], item, slot); // 실전 부착점 외형 적용
                if (item == null) // 미장착 상태
                {
                    continue; // 외형과 수치 적용 건너뛰기
                }

                FlightBroomPart part = item.Definition; // 기본 외형과 효과
                bonuses[(int)part.stat] += part.GetValue(item.Grade, item.Upgrade); // 양의 보너스 합산
                bonuses[(int)part.penaltyStat] += part.penalty; // 불이익 합산
            }
        }

        private void ApplyAppearance(MeshRenderer mount, OwnedPart item, int slot) // 실전과 미리보기에서 공유하는 부착점 표시
        {
            mount.enabled = item != null; // 비어 있는 슬롯 숨김
            if (item != null) // 실제 부품 외형
            {
                FlightBroomPart part = item.Definition; // 공통 부품 재질과 배율
                mount.sharedMaterial = part.material != null ? part.material : defaultMaterials[slot]; // 지정 재질 우선
                mount.transform.localScale = Vector3.Scale(defaultScales[slot], part.scale); // 원본 크기에서 계산한 배율
            }
        }

        private static float Contribution(OwnedPart item, FlightBroomStat stat) // 장착 부품 하나가 추가하는 같은 항목의 효과
        {
            if (item == null) // 빈 슬롯의 효과
            {
                return 0f; // 합산값 없음
            }
            FlightBroomPart part = item.Definition; // 주 효과와 불이익
            float primary = part.stat == stat ? part.GetValue(item.Grade, item.Upgrade) : 0f; // 같은 주 효과만 합산
            return primary + (part.penaltyStat == stat ? part.penalty : 0f); // 같은 불이익까지 합산
        }

        private void SaveLoadouts() // 확정 장착 형태 저장
        {
            string key = SaveKey; // 현재 실행의 환경별 저장 키
            if (string.IsNullOrEmpty(key)) // 별도 저장을 쓰지 않는 시험
            {
                return; // 전역 사용자 설정 보존
            }
            string[] ids = new string[equipped.Length]; // 캐릭터별 전체 슬롯
            int slots = equipped.GetLength(1); // 한 캐릭터의 슬롯 수
            for (int i = 0; i < ids.Length; i++) // 캐릭터와 슬롯을 고정 순서로 저장
            {
                ids[i] = equipped[i / slots, i % slots]?.Id ?? string.Empty; // 빈 슬롯도 기록
            }
            PlayerPrefs.SetString(key, JsonUtility.ToJson(new LoadoutData { ids = ids })); // 전체 장착을 한 값으로 저장
            PlayerPrefs.Save(); // 실전 재실행에 대비한 즉시 기록
        }

        private void RestoreLoadouts() // 장착 ID 검증 후 한 번에 복원
        {
            string key = SaveKey; // 실제 실행 또는 격리 테스트의 저장 키
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key)) // 첫 실행과 독립 검증
            {
                return; // 기본 시작 장착 유지
            }

            LoadoutData data; // 복원 전 검사할 원본
            try // 손상된 저장도 게임 시작 허용
            {
                data = JsonUtility.FromJson<LoadoutData>(PlayerPrefs.GetString(key)); // 장착 ID 목록 읽기
            }
            catch (ArgumentException) // 잘못된 JSON 형식
            {
                return; // 유효한 기본 조합 유지
            }

            if (data?.ids == null || data.ids.Length != equipped.Length) // 전체 슬롯 배열 확인
            {
                return; // 일부 장착만 복원되는 상황 방지
            }
            int slots = equipped.GetLength(1); // 캐릭터별 슬롯 크기
            OwnedPart[] restored = new OwnedPart[data.ids.Length]; // 아직 게임에 적용하지 않은 안전한 복원본
            HashSet<string> seen = new(); // 중복된 개별 보유 부품 확인
            for (int i = 0; i < restored.Length; i++) // 저장한 각 슬롯의 부품 검사
            {
                string id = data.ids[i]; // 개별 장착 ID
                if (string.IsNullOrEmpty(id)) // 비워 둔 장착 슬롯
                {
                    continue; // 빈 슬롯의 정상 복원
                }
                if (!owned.TryGetValue(id, out OwnedPart item) || (int)item.Definition.slot != i % slots || !seen.Add(id)) // 미보유·슬롯 불일치·중복 장착 검사
                {
                    return; // 오류 없는 기존 기본 조합 유지
                }
                restored[i] = item; // 유효한 부품 복원 후보
            }
            for (int i = 0; i < restored.Length; i++) // 원래 보유자의 장착 해제
            {
                Equip(null, i / slots, i % slots); // 다른 캐릭터로 옮겨진 부품의 이전 상태 제거
            }
            for (int i = 0; i < restored.Length; i++) // 검증된 조합을 일괄 반영
            {
                Equip(restored[i], i / slots, i % slots); // 복원된 부품과 현재 소유자
            }
        }
    }
}
