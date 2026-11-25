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
                MeshRenderer mount = mounts[slot]; // 편집 가능한 씬 렌더러
                mount.enabled = item != null; // 비어 있으면 외형 숨김
                if (item == null) // 미장착 상태
                {
                    continue; // 외형과 수치 적용 건너뛰기
                }

                FlightBroomPart part = item.Definition; // 기본 외형과 효과
                mount.sharedMaterial = part.material != null ? part.material : defaultMaterials[slot]; // 교체 부품 재질과 기본값
                mount.transform.localScale = Vector3.Scale(defaultScales[slot], part.scale); // 원본 크기에 부품 배율 적용
                bonuses[(int)part.stat] += part.GetValue(item.Grade, item.Upgrade); // 양의 보너스 합산
                bonuses[(int)part.penaltyStat] += part.penalty; // 불이익 합산
            }
        }
    }
}
