using System; // 실제 정산 형식과 시험 경로
using System.IO; // 독립 영구 저장 파일 검증
using NUnit.Framework; // 시작·정산·복구 결과의 단언
using UnityEngine; // 격리 시험용 임시 경로

namespace ProjectG.Tests // 프로젝트 자동 검증
{
    public sealed class FlightProgressTests // 영구 보상과 출격 ID 검증
    {
        [Test] // 재실행과 같은 영수증 재요청
        public void SettlementPersistsExactlyOnceAcrossStoreInstances() // 성공·실패·복구와 조종사 분리
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp"); // 정식 저장 구현 확인
            Assert.That(type, Is.Not.Null, "출격 정산 저장소"); // 별도 시제품이 아닌 실제 저장소
            string directory = Path.Combine(Application.temporaryCachePath, "Progress-" + Guid.NewGuid().ToString("N")); // 사용자 데이터와 격리
            string path = Path.Combine(directory, "progress.json"); // 재실행이 읽을 동일한 디스크 파일
            try // 테스트 완료 후 임시 파일 정리
            {
                Type pilotType = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 열거형
                object aria = Enum.Parse(pilotType, "Aria"); // 첫 조종사의 누적 보상
                object rian = Enum.Parse(pilotType, "Rian"); // 다른 조종사의 개별 기록
                string interrupted; // 다음 실행에서 복구할 실제 ID
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 디스크에 보상을 지급하는 첫 실행
                {
                    string run = (string)Call(type, store, "Begin", aria); // 첫 웨이브 참여 기록
                    Assert.That(run, Is.Not.Empty); // 파일과 연결된 출격 ID
                    Assert.That(Call(type, store, "Begin", rian), Is.Null); // 두 출격의 참여 충돌 방지
                    Assert.That(Call(type, store, "Complete", run, true), Is.Not.Null); // 클리어 최초 지급 확인
                    Assert.That(Call(type, store, "Complete", run, false), Is.Not.Null); // 동일 ID에 기존 영수증 반환
                    Assert.That(Crystals(type, store), Is.EqualTo(6)); // 중복으로 공통 재료가 오르지 않음
                    Assert.That(Call(type, store, "GetMastery", aria), Is.EqualTo(20)); // 참여 및 클리어 숙련도
                    Assert.That(Call(type, store, "GetMastery", rian), Is.Zero); // 다른 조종사의 숙련도 유지
                    interrupted = (string)Call(type, store, "Begin", rian); // 보스 격파 전 앱 종료 가정
                    Assert.That(File.ReadAllText(path), Does.Contain(interrupted)); // 재실행 전에 참여 자체가 영구 저장
                }
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 모의 재실행에서 기존 파일 불러오기
                {
                    Assert.That(Call(type, store, "Recover"), Is.Not.Null); // 중단된 리안 출격의 기본 보상
                    Assert.That(Call(type, store, "Recover"), Is.Null); // 같은 중단 출격의 중복 보상 방지
                    Assert.That(Call(type, store, "GetMastery", rian), Is.EqualTo(10)); // 리안의 기본 참여 숙련도
                    Assert.That(Crystals(type, store), Is.EqualTo(9)); // 실패 이후 공통 재료 합계
                    for (int i = 0; i < 2; i++) // 누적 숙련도 60의 경계 통과
                    {
                        string run = (string)Call(type, store, "Begin", aria); // 아리아의 새 출격 ID
                        Assert.That(Call(type, store, "Complete", run, true), Is.Not.Null); // 두 번의 신규 클리어
                    }
                    Assert.That(Call(type, store, "GetMastery", aria), Is.EqualTo(60)); // 누적 숙련도 경계
                    Assert.That(Call(type, store, "GetMarks", aria), Is.EqualTo(1)); // 첫 조율의 증표
                    Assert.That(Call(type, store, "GetMarks", rian), Is.Zero); // 조율의 증표 캐릭터별 분리
                }
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 세 번째 실행에서 중복 및 누적 확인
                {
                    Assert.That(Call(type, store, "Recover"), Is.Null); // 완료한 영수증의 재처리 없음
                    Assert.That(Call(type, store, "Complete", interrupted, false), Is.Null); // 오래된 ID의 이중 지급 없음
                    Assert.That(Crystals(type, store), Is.EqualTo(21)); // 실제 저장한 세 차례 성공과 한 차례 실패
                    Assert.That(Call(type, store, "GetMarks", aria), Is.EqualTo(1)); // 증표의 영구 저장
                }
            }
            finally // 사용자 저장과 무관한 시험 폴더 정리
            {
                if (Directory.Exists(directory)) // 파일을 쓴 시험만 정리
                {
                    foreach (string file in Directory.GetFiles(directory)) // 생성한 전용 정산 파일
                    {
                        File.Delete(file); // 시험 데이터 삭제
                    }
                    Directory.Delete(directory); // 빈 시험 폴더 삭제
                }
            }
        }

        private static object Call(Type type, object store, string method, params object[] supplied) // 실제 게임 어셈블리의 정산 호출
        {
            return type.GetMethod(method).Invoke(store, supplied); // 참조 없는 테스트 조립과 연결
        }

        private static int Crystals(Type type, object store) // 정산 전후에 비교할 공통 재료
        {
            return (int)type.GetProperty("SharedCrystals").GetValue(store); // 현재 저장된 천공 결정
        }
    }
}
