using System; // 프리셋 식별자와 저장 자료
using System.Collections.Generic; // 캐릭터별 프리셋 목록
using UnityEngine; // 사용자 설정 저장

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightBroomPresetStore // 캐릭터별 이름 있는 빗자루 조합
    {
        [Serializable] // 하나의 프리셋을 영구 저장
        internal sealed class Preset // 부품과 마법의 독립 참조
        {
            public string id; // 복제본과 구분하는 고유 식별자
            public string name; // 사용자가 정한 표시 이름
            public int pilot; // 소유한 캐릭터 번호
            public string[] parts; // 여덟 부위의 개별 보유 ID
            public string[] recipes; // 두 마법의 실제 구성 ID
            public string[] references; // 두 마법의 개별 저장 사본 ID
            public int[] priorities; // 두 마법의 유지 우선순위
        }

        [Serializable] // 버전과 전체 프리셋을 함께 저장
        private sealed class SaveData // 저장 구조의 독립 버전
        {
            public int version; // 알 수 없는 미래 형식 보호
            public Preset[] items; // 캐릭터별 프리셋 전체 목록
        }

        private const string UserKey = "ProjectG.Flight.Broom.Presets.v1"; // 사용자별 프리셋 저장 키
        private readonly List<Preset> items = new(); // 캐릭터별 독립 기록
        private readonly string key; // 검증 또는 실제 사용자 저장
        private bool writable = true; // 손상된 기존 저장의 덮어쓰기 차단

        internal FlightBroomPresetStore() // 저장 키 선택과 기존 기록 복원
        {
            key = Environment.GetEnvironmentVariable("PROJECTG_BROOM_PRESET_TEST_KEY") ?? (FlightBindings.UsesPreferences ? UserKey : null); // 시험 환경과 사용자 저장 분리
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key)) // 이전 저장이 없는 실행
            {
                return; // 빈 프리셋 목록으로 시작
            }
            try // 기존 저장의 완전성 확인
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key)); // 버전과 기록 읽기
                if (data == null || data.version != 1 || data.items == null) // 손상 또는 미래 저장 형식
                {
                    writable = false; // 원본 텍스트의 덮어쓰기 차단
                    return; // 해석하지 못한 데이터 보존
                }
                foreach (Preset item in data.items) // 저장한 각 프리셋 확인
                {
                    if (!Valid(item) || Count(item.pilot) >= 5 || items.Exists(value => value.id == item.id)) // 잘못된 참조·한도·중복 ID
                    {
                        items.Clear(); // 부분적으로 읽은 목록 제거
                        writable = false; // 기존 원본의 쓰기 차단
                        return; // 사용자 데이터 보존
                    }
                    items.Add(item); // 확인한 프리셋만 원래 순서대로 보존
                }
            }
            catch (ArgumentException) // 형식이 깨진 사용자 저장
            {
                items.Clear(); // 불완전한 읽기 결과 제거
                writable = false; // 기존 텍스트의 덮어쓰기 차단
            }
        }

        internal bool References(string reference) // 어떤 프리셋이 마법 사본을 참조하는지 확인
        {
            return !writable || !string.IsNullOrEmpty(reference) && items.Exists(item => Array.Exists(item.references, value => value == reference)); // 손상 저장도 원본 보호를 위해 삭제 차단
        }

        internal int Count(int pilot) // 해당 캐릭터의 저장 개수
        {
            return items.FindAll(item => item.pilot == pilot).Count; // 다른 캐릭터의 기록 제외
        }

        internal Preset At(int pilot, int index) // 해당 캐릭터의 저장 순서 조회
        {
            if (index < 0) // 음수 인덱스
            {
                return null; // 실제 프리셋 없음
            }
            foreach (Preset item in items) // 전체 기록의 원래 순서
            {
                if (item.pilot == pilot && index-- == 0) // 해당 캐릭터의 원하는 번째 기록
                {
                    return item; // 저장된 실제 기록
                }
            }
            return null; // 해당 인덱스의 기록 없음
        }

        internal bool Add(int pilot, string name, string[] parts, string[] recipes, string[] references, int[] priorities) // 현재 조합의 새 독립 저장
        {
            if (!writable || pilot < 0 || pilot >= 7 || Count(pilot) >= 5 || !ValidName(name) || parts == null || parts.Length != 8 || recipes == null || recipes.Length != 2 || references == null || references.Length != 2 || priorities == null || priorities.Length != 2 || Array.Exists(priorities, value => value < 0 || value > 2)) // 형식과 캐릭터별 상한
            {
                return false; // 기존 기록을 변경하지 않음
            }
            items.Add(new Preset { id = Guid.NewGuid().ToString("N"), name = name.Trim(), pilot = pilot, parts = (string[])parts.Clone(), recipes = (string[])recipes.Clone(), references = (string[])references.Clone(), priorities = (int[])priorities.Clone() }); // 입력 배열과 독립적인 새 기록
            Write(); // 확정된 목록 저장
            return true; // 실제 추가 완료
        }

        internal bool SaveCurrent(FlightBroomEquipment equipment, FlightCraftedSpellBook book, string name) // 실제 현재 장착과 마법을 한 기록으로 저장
        {
            if (equipment == null || book == null || !book.UsesEquipment(equipment)) // 같은 캐릭터의 장비와 도감 확인
            {
                return false; // 서로 다른 씬의 조합 저장 방지
            }
            var recipes = new string[2]; // 현재 양쪽 마법 구성
            var references = new string[2]; // 선택한 개별 마법 사본
            var priorities = new int[2]; // 슬롯별 유지 순서
            for (int i = 0; i < recipes.Length; i++) // 실제 저장된 두 슬롯
            {
                recipes[i] = book.Slot(i)?.Id; // 빈 슬롯 또는 구성의 고유 ID
                references[i] = book.SlotReference(i); // 개별 사본 또는 이전 기본 구성
                priorities[i] = book.Priority(i); // 저장한 슬롯별 우선순위
            }
            return Add((int)equipment.ActivePilot, name, equipment.PresetParts(), recipes, references, priorities); // 여덟 부품과 두 마법의 독립 기록
        }

        internal bool CanApply(int pilot, int index, FlightBroomEquipment equipment, FlightCraftedSpellBook book, out string reason) // 현재 저장 조합의 적용 가능 여부
        {
            reason = null; // 성공한 검사에는 오류 없음
            Preset item = At(pilot, index); // 선택한 실제 프리셋
            if (item == null || equipment == null || book == null || !book.UsesEquipment(equipment)) // 기록과 같은 장비·마법 도감 확인
            {
                reason = "프리셋과 현재 캐릭터를 확인하세요."; // 유효하지 않은 선택 안내
                return false; // 실제 장착을 변경하지 않음
            }
            if (!equipment.CanApplyPreset(pilot, item.parts, out float attack, out reason)) // 여덟 개별 부품과 예상 능력치
            {
                return false; // 부품이 없는 조합 제외
            }
            return book.PreparePreset(item.recipes, item.references, item.priorities, attack, out _, out reason); // 마법 사본·연구·한도 검사
        }

        internal bool TryApply(int pilot, int index, FlightBroomEquipment equipment, FlightCraftedSpellBook book, out string reason) // 전체 사전 검증 후 현재 장착 확정
        {
            reason = null; // 성공 시 표시할 오류 없음
            Preset item = At(pilot, index); // 실제 선택한 캐릭터 프리셋
            if (item == null || equipment == null || book == null || !book.UsesEquipment(equipment)) // 없는 기록이나 다른 씬 장비
            {
                reason = "프리셋과 현재 캐릭터를 확인하세요."; // 변경하지 않은 이유 안내
                return false; // 전체 상태 보존
            }
            if (!equipment.CanApplyPreset(pilot, item.parts, out float magicAttack, out reason)) // 여덟 사본과 새 조합의 공격력 검사
            {
                return false; // 어느 부위도 적용하지 않음
            }
            if (!book.PreparePreset(item.recipes, item.references, item.priorities, magicAttack, out CraftedSpellRecipe[] prepared, out reason)) // 정확한 마법 사본과 새 공격력 검사
            {
                return false; // 장비와 마법을 모두 보존
            }
            if (!equipment.ApplyPresetParts(pilot, item.parts)) // 실제 확정 직전 충돌 재확인
            {
                reason = "부품 상태가 변경되어 적용하지 않았습니다."; // 동시 변경의 명확한 안내
                return false; // 마법 편성은 변경하지 않음
            }
            book.ApplyPresetMagic(prepared, item.references, item.priorities); // 검증된 두 마법과 우선순위 확정
            return true; // 전체 부품과 마법 적용 완료
        }

        internal bool Copy(int pilot, int index) // 다른 식별자의 독립 기록 복제
        {
            Preset source = At(pilot, index); // 복제할 실제 프리셋
            if (source == null) // 존재하지 않는 원본
            {
                return false; // 다른 기록 변경 없음
            }
            string prefix = source.name.Length > 29 ? source.name.Substring(0, 29) : source.name; // 이름 길이 상한 보존
            return Add(pilot, prefix + " 복제", source.parts, source.recipes, source.references, source.priorities); // 사본별 배열과 ID 생성
        }

        internal bool Rename(int pilot, int index, string name) // 기존 기록의 이름 변경
        {
            Preset item = At(pilot, index); // 이름을 바꿀 실제 기록
            if (!writable || item == null || !ValidName(name)) // 손상 저장·범위·이름 확인
            {
                return false; // 기존 이름 보존
            }
            item.name = name.Trim(); // 해당 사본만 이름 변경
            Write(); // 변경한 이름 저장
            return true; // 유효한 이름 변경
        }

        internal bool Remove(int pilot, int index) // 프리셋 기록만 삭제
        {
            Preset item = At(pilot, index); // 지울 실제 기록
            if (!writable || item == null) // 보호된 저장 또는 없는 기록
            {
                return false; // 부품과 마법을 포함한 기존 상태 보존
            }
            items.Remove(item); // 해당 기록만 목록에서 제외
            Write(); // 나머지 프리셋 저장
            return true; // 개별 기록 삭제 완료
        }

        private static bool ValidName(string name) // 공백 제거 후 저장 가능한 이름
        {
            return !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 32; // 빈 이름과 긴 이름 제한
        }

        private static bool Valid(Preset item) // 원본 저장의 최소 구조 검사
        {
            return item != null && !string.IsNullOrEmpty(item.id) && ValidName(item.name) && item.pilot >= 0 && item.pilot < 7 && item.parts != null && item.parts.Length == 8 && item.recipes != null && item.recipes.Length == 2 && item.references != null && item.references.Length == 2 && item.priorities != null && item.priorities.Length == 2 && !Array.Exists(item.priorities, value => value < 0 || value > 2); // 캐릭터·슬롯·우선순위 확인
        }

        private void Write() // 전체 프리셋의 확정 저장
        {
            if (!string.IsNullOrEmpty(key)) // 저장이 허용된 실제 실행
            {
                PlayerPrefs.SetString(key, JsonUtility.ToJson(new SaveData { version = 1, items = items.ToArray() })); // 버전과 전체 목록을 한 항목으로 기록
                PlayerPrefs.Save(); // 재실행 뒤 복원할 확정 저장
            }
        }
    }
}
