using System; // 비교와 기본 자료형
using System.Collections.Generic; // 객체와 배열 목록
using System.Globalization; // 문화권과 무관한 수치 검사
using System.Text; // 문자열 임시 버퍼
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    internal static class ProgressJsonValidation // 저장 필드와 타입 검사
    { // 검사 시작
        private enum JsonKind // JSON 값 종류
        { // 종류 시작
            Object, // 객체
            Array, // 배열
            String, // 문자열
            Number, // 정수 또는 숫자
            Boolean, // 참과 거짓
            Null // 빈 값
        } // 종류 종료
        private sealed class Node // 검사할 JSON 값
        { // 값 시작
            public JsonKind Kind; // 값 종류
            public string Text; // 원문 또는 문자열
            public Dictionary<string, Node> Fields; // 객체 필드
            public List<Node> Items; // 배열 항목
        } // 값 종료
        public static bool IsValid(string raw) // 저장 원문의 필수 타입 검사
        { // 검사 시작
            if (string.IsNullOrEmpty(raw) || raw.Length > 4194304) // 빈 원문과 과도한 크기 확인
            { // 오류 시작
                return false; // 안전하지 않은 원문 차단
            } // 오류 종료
            Reader reader = new Reader(raw); // 엄격한 JSON 검사기
            if (!reader.TryParse(out Node root) || !ObjectHas(root, new string[] // 최상위 필드 확인
            { // 필드 목록 시작
                "schemaVersion", "commonMaterials", "rapidMastery", "sniperMastery", "rapidMaterials", "sniperMaterials", // 성장 숫자 필드
                "activeRunId", "activeCharacter", "rapidUnlocks", "sniperUnlocks", "receipts" // 출격과 해금 필드
            })) // 필드 목록 종료
            { // 오류 시작
                return false; // 누락 또는 알 수 없는 필드 차단
            } // 오류 종료
            string[] numeric = new string[] // 정수 필드 목록
            { // 목록 시작
                "schemaVersion", "commonMaterials", "rapidMastery", "sniperMastery", "rapidMaterials", "sniperMaterials", "activeCharacter" // 숫자 이름
            }; // 목록 종료
            foreach (string name in numeric) // 숫자 필드 순회
            { // 숫자 검사 시작
                if (!Integer(root.Fields[name])) // 정수 범위와 타입 확인
                { // 오류 시작
                    return false; // 숫자가 아닌 값 차단
                } // 오류 종료
            } // 숫자 검사 종료
            if (root.Fields["activeRunId"].Kind != JsonKind.String || !StringArray(root.Fields["rapidUnlocks"]) || !StringArray(root.Fields["sniperUnlocks"])) // 출격과 해금 타입 확인
            { // 오류 시작
                return false; // 잘못된 타입 차단
            } // 오류 종료
            Node receipts = root.Fields["receipts"]; // 정산 영수증 배열
            if (receipts.Kind != JsonKind.Array) // 배열 타입 확인
            { // 오류 시작
                return false; // 잘못된 정산 목록 차단
            } // 오류 종료
            foreach (Node receipt in receipts.Items) // 영수증 순회
            { // 영수증 검사 시작
                if (!ObjectHas(receipt, new string[] // 영수증의 필수 필드
                { // 목록 시작
                    "runId", "character", "cleared", "masteryGained", "commonMaterialsGained", "characterMaterialsGained" // 완전한 정산 기록
                })) // 목록 종료
                { // 오류 시작
                    return false; // 누락 또는 알 수 없는 필드 차단
                } // 오류 종료
                if (receipt.Fields["runId"].Kind != JsonKind.String || receipt.Fields["cleared"].Kind != JsonKind.Boolean || !Integer(receipt.Fields["character"]) || !Integer(receipt.Fields["masteryGained"]) || !Integer(receipt.Fields["commonMaterialsGained"]) || !Integer(receipt.Fields["characterMaterialsGained"])) // 영수증의 값 타입 확인
                { // 오류 시작
                    return false; // 손상된 정산 내역 차단
                } // 오류 종료
            } // 영수증 검사 종료
            return true; // 필드와 타입 확인 완료
        } // 검사 종료
        private static bool ObjectHas(Node node, string[] required) // 객체 필드의 정확한 일치 확인
        { // 검사 시작
            if (node == null || node.Kind != JsonKind.Object || node.Fields.Count != required.Length) // 객체와 필드 개수 확인
            { // 오류 시작
                return false; // 누락과 추가 필드 차단
            } // 오류 종료
            foreach (string name in required) // 필수 이름 순회
            { // 이름 검사 시작
                if (!node.Fields.ContainsKey(name)) // 이름 존재 확인
                { // 누락 시작
                    return false; // 필드 누락 차단
                } // 누락 종료
            } // 이름 검사 종료
            return true; // 모든 이름 확인
        } // 검사 종료
        private static bool Integer(Node node) // 정수 JSON 타입과 범위 확인
        { // 검사 시작
            return node != null && node.Kind == JsonKind.Number && int.TryParse(node.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _); // 32비트 정수 확인
        } // 검사 종료
        private static bool StringArray(Node node) // 문자열 배열 확인
        { // 검사 시작
            if (node == null || node.Kind != JsonKind.Array) // 배열 종류 확인
            { // 오류 시작
                return false; // 배열 이외 차단
            } // 오류 종료
            foreach (Node item in node.Items) // 배열 항목 순회
            { // 항목 검사 시작
                if (item.Kind != JsonKind.String) // 문자열 타입 확인
                { // 오류 시작
                    return false; // 다른 타입 차단
                } // 오류 종료
            } // 항목 검사 종료
            return true; // 문자열 배열 확인
        } // 검사 종료
        private sealed class Reader // 깊이 제한 JSON 판독기
        { // 판독기 시작
            private readonly string raw; // 검사할 원문
            private int index; // 현재 문자 위치
            public Reader(string value) // 판독 원문 연결
            { // 생성 시작
                raw = value; // 원문 보관
            } // 생성 종료
            public bool TryParse(out Node node) // 전체 원문 읽기
            { // 읽기 시작
                node = null; // 결과 초기화
                SkipWhitespace(); // 시작 공백 제거
                if (!Value(0, out node)) // 첫 값 읽기
                { // 오류 시작
                    return false; // 잘못된 JSON 차단
                } // 오류 종료
                SkipWhitespace(); // 끝 공백 제거
                return index == raw.Length; // 남은 문자 없는지 확인
            } // 읽기 종료
            private bool Value(int depth, out Node node) // JSON 값 하나 읽기
            { // 값 읽기 시작
                node = null; // 결과 초기화
                SkipWhitespace(); // 값 앞 공백 제거
                if (depth > 32 || index >= raw.Length) // 중첩과 끝 확인
                { // 오류 시작
                    return false; // 과도한 중첩 차단
                } // 오류 종료
                char current = raw[index]; // 현재 문자
                if (current == '{') // 객체 시작 확인
                { // 객체 처리 시작
                    return Object(depth + 1, out node); // 객체 읽기
                } // 객체 처리 종료
                if (current == '[') // 배열 시작 확인
                { // 배열 처리 시작
                    return Array(depth + 1, out node); // 배열 읽기
                } // 배열 처리 종료
                if (current == '"') // 문자열 시작 확인
                { // 문자열 처리 시작
                    if (!String(out string text)) // 문자열 읽기
                    { // 오류 시작
                        return false; // 잘못된 문자열 차단
                    } // 오류 종료
                    node = new Node(); // 문자열 값 생성
                    node.Kind = JsonKind.String; // 문자열 종류 기록
                    node.Text = text; // 문자열 내용 기록
                    return true; // 문자열 읽기 완료
                } // 문자열 처리 종료
                if (current == '-' || char.IsDigit(current)) // 숫자 시작 확인
                { // 숫자 처리 시작
                    return Number(out node); // 숫자 읽기
                } // 숫자 처리 종료
                if (Literal("true") || Literal("false")) // 불리언 확인
                { // 불리언 처리 시작
                    node = new Node(); // 불리언 값 생성
                    node.Kind = JsonKind.Boolean; // 불리언 종류 기록
                    return true; // 불리언 읽기 완료
                } // 불리언 처리 종료
                if (Literal("null")) // 빈 값 확인
                { // 빈 값 처리 시작
                    node = new Node(); // 빈 값 객체 생성
                    node.Kind = JsonKind.Null; // 빈 값 종류 기록
                    return true; // 빈 값 읽기 완료
                } // 빈 값 처리 종료
                return false; // 알 수 없는 값 차단
            } // 값 읽기 종료
            private bool Object(int depth, out Node node) // JSON 객체 읽기
            { // 객체 읽기 시작
                node = new Node(); // 객체 값 생성
                node.Kind = JsonKind.Object; // 객체 종류 기록
                node.Fields = new Dictionary<string, Node>(StringComparer.Ordinal); // 중복 키 검사 목록
                index++; // 여는 괄호 이동
                SkipWhitespace(); // 첫 필드 앞 공백 제거
                if (Consume('}')) // 빈 객체 확인
                { // 빈 객체 처리 시작
                    return true; // 빈 객체 읽기 완료
                } // 빈 객체 처리 종료
                while (index < raw.Length) // 필드 순회
                { // 필드 처리 시작
                    if (!String(out string key)) // 키 문자열 확인
                    { // 오류 시작
                        return false; // 잘못된 키 차단
                    } // 오류 종료
                    SkipWhitespace(); // 구분자 앞 공백 제거
                    if (!Consume(':') || !Value(depth, out Node value) || node.Fields.ContainsKey(key)) // 구분자와 중복 키 확인
                    { // 오류 시작
                        return false; // 중복 또는 잘못된 필드 차단
                    } // 오류 종료
                    node.Fields.Add(key, value); // 고유 필드 저장
                    SkipWhitespace(); // 필드 뒤 공백 제거
                    if (Consume('}')) // 객체 끝 확인
                    { // 객체 끝 시작
                        return true; // 객체 읽기 완료
                    } // 객체 끝 종료
                    if (!Consume(',')) // 다음 필드 구분 확인
                    { // 오류 시작
                        return false; // 잘못된 구분 차단
                    } // 오류 종료
                    SkipWhitespace(); // 다음 키 앞 공백 제거
                } // 필드 처리 종료
                return false; // 닫히지 않은 객체 차단
            } // 객체 읽기 종료
            private bool Array(int depth, out Node node) // JSON 배열 읽기
            { // 배열 읽기 시작
                node = new Node(); // 배열 값 생성
                node.Kind = JsonKind.Array; // 배열 종류 기록
                node.Items = new List<Node>(); // 배열 값 목록
                index++; // 여는 괄호 이동
                SkipWhitespace(); // 첫 항목 앞 공백 제거
                if (Consume(']')) // 빈 배열 확인
                { // 빈 배열 처리 시작
                    return true; // 빈 배열 읽기 완료
                } // 빈 배열 처리 종료
                while (index < raw.Length) // 배열 항목 순회
                { // 항목 처리 시작
                    if (!Value(depth, out Node item)) // 배열 값 확인
                    { // 오류 시작
                        return false; // 잘못된 항목 차단
                    } // 오류 종료
                    node.Items.Add(item); // 확인한 항목 보관
                    SkipWhitespace(); // 항목 뒤 공백 제거
                    if (Consume(']')) // 배열 끝 확인
                    { // 배열 끝 시작
                        return true; // 배열 읽기 완료
                    } // 배열 끝 종료
                    if (!Consume(',')) // 다음 항목 구분 확인
                    { // 오류 시작
                        return false; // 잘못된 구분 차단
                    } // 오류 종료
                } // 항목 처리 종료
                return false; // 닫히지 않은 배열 차단
            } // 배열 읽기 종료
            private bool String(out string value) // JSON 문자열 읽기
            { // 문자열 읽기 시작
                value = null; // 결과 초기화
                if (!Consume('"')) // 여는 따옴표 확인
                { // 오류 시작
                    return false; // 문자열 이외 차단
                } // 오류 종료
                StringBuilder text = new StringBuilder(); // 문자 보관 버퍼
                while (index < raw.Length) // 문자 순회
                { // 문자 처리 시작
                    char current = raw[index++]; // 현재 문자와 위치 이동
                    if (current == '"') // 닫는 따옴표 확인
                    { // 문자열 완료 시작
                        value = text.ToString(); // 문자열 확정
                        return true; // 문자열 읽기 완료
                    } // 문자열 완료 종료
                    if (current < 0x20) // 제어 문자 확인
                    { // 오류 시작
                        return false; // 이스케이프 없는 제어 문자 차단
                    } // 오류 종료
                    if (current != '\\') // 일반 문자 확인
                    { // 일반 처리 시작
                        text.Append(current); // 문자 보관
                        continue; // 다음 문자 처리
                    } // 일반 처리 종료
                    if (index >= raw.Length) // 이스케이프 뒤 문자 확인
                    { // 오류 시작
                        return false; // 불완전한 이스케이프 차단
                    } // 오류 종료
                    char escape = raw[index++]; // 이스케이프 종류
                    if (escape == '"' || escape == '\\' || escape == '/') // 직접 변환 문자 확인
                    { // 변환 시작
                        text.Append(escape); // 원래 문자 보관
                    } // 변환 종료
                    else if (escape == 'b' || escape == 'f' || escape == 'n' || escape == 'r' || escape == 't') // 제어 문자 이스케이프 확인
                    { // 제어 문자 처리 시작
                        text.Append(escape == 'b' ? '\b' : escape == 'f' ? '\f' : escape == 'n' ? '\n' : escape == 'r' ? '\r' : '\t'); // 실제 제어 문자 보관
                    } // 제어 문자 처리 종료
                    else if (escape == 'u') // 유니코드 이스케이프 확인
                    { // 유니코드 처리 시작
                        if (index + 4 > raw.Length || !ushort.TryParse(raw.Substring(index, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code)) // 네 자리 확인
                        { // 오류 시작
                            return false; // 잘못된 유니코드 차단
                        } // 오류 종료
                        text.Append((char)code); // 유니코드 문자 보관
                        index += 4; // 코드 문자 건너뛰기
                    } // 유니코드 처리 종료
                    else // 미지원 이스케이프
                    { // 오류 시작
                        return false; // 잘못된 이스케이프 차단
                    } // 오류 종료
                } // 문자 처리 종료
                return false; // 닫히지 않은 문자열 차단
            } // 문자열 읽기 종료
            private bool Number(out Node node) // JSON 정수 토큰 읽기
            { // 숫자 읽기 시작
                node = null; // 결과 초기화
                int start = index; // 숫자 시작 위치
                if (raw[index] == '-') // 음수 기호 확인
                { // 부호 처리 시작
                    index++; // 다음 문자 이동
                } // 부호 처리 종료
                if (index >= raw.Length || raw[index] < '0' || raw[index] > '9') // 숫자 첫 자리 확인
                { // 오류 시작
                    return false; // 잘못된 숫자 차단
                } // 오류 종료
                if (raw[index] == '0') // 영 자리 확인
                { // 영 처리 시작
                    index++; // 영 자리 이동
                    if (index < raw.Length && raw[index] >= '0' && raw[index] <= '9') // 선행 영 확인
                    { // 오류 시작
                        return false; // 선행 영 차단
                    } // 오류 종료
                } // 영 처리 종료
                else // 일반 정수
                { // 숫자 순회 시작
                    while (index < raw.Length && raw[index] >= '0' && raw[index] <= '9') // 정수 자리 확인
                    { // 자리 처리 시작
                        index++; // 다음 자리 이동
                    } // 자리 처리 종료
                } // 숫자 순회 종료
                node = new Node(); // 숫자 값 생성
                node.Kind = JsonKind.Number; // 숫자 종류 기록
                node.Text = raw.Substring(start, index - start); // 정수 원문 보관
                return true; // 정수 토큰 읽기 완료
            } // 숫자 읽기 종료
            private bool Literal(string expected) // 고정 JSON 토큰 확인
            { // 토큰 확인 시작
                if (index + expected.Length > raw.Length || string.CompareOrdinal(raw, index, expected, 0, expected.Length) != 0) // 토큰 일치 확인
                { // 불일치 시작
                    return false; // 위치 유지
                } // 불일치 종료
                index += expected.Length; // 일치한 토큰 이동
                return true; // 토큰 확인 완료
            } // 토큰 확인 종료
            private bool Consume(char expected) // 구분 문자 확인
            { // 문자 확인 시작
                if (index >= raw.Length || raw[index] != expected) // 현재 문자 비교
                { // 불일치 시작
                    return false; // 위치 유지
                } // 불일치 종료
                index++; // 확인한 문자 이동
                return true; // 구분 문자 확인
            } // 문자 확인 종료
            private void SkipWhitespace() // JSON 허용 공백 건너뛰기
            { // 공백 처리 시작
                while (index < raw.Length && (raw[index] == ' ' || raw[index] == '\t' || raw[index] == '\n' || raw[index] == '\r')) // JSON 공백 확인
                { // 공백 이동 시작
                    index++; // 다음 문자 이동
                } // 공백 이동 종료
            } // 공백 처리 종료
        } // 판독기 종료
    } // 검사 종료
} // 영역 종료
