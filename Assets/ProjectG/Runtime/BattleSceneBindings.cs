using UnityEngine; // 직렬화 전투 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattleSceneBindings : MonoBehaviour // 씬에 저장된 실제 전투 구성
    { // 구성 시작
        [SerializeField] private Camera battleCamera; // 편집 가능한 전투 카메라
        [SerializeField] private BattlePlayer player; // 편집 가능한 플레이어
        [SerializeField] private StageBackgroundScroll background; // 편집 가능한 배경
        [SerializeField] private StageDirector stage; // 실제 스테이지 관리자
        [SerializeField] private EnemyBulletPool bulletPool; // 실제 적탄 풀
        [SerializeField] private BattleProgress progress; // 실제 경험치 관리자
        [SerializeField] private UpgradeDropManager drops; // 실제 강화 관리자
        [SerializeField] private SceneBattleHudPresenter hud; // 편집 가능한 전투 HUD
        [SerializeField] private HitboxIndicator hitbox; // 편집 가능한 피격 표시
        [SerializeField] private BattleHud battleHud; // 그레이즈와 캐릭터 표시
        [SerializeField] private SkillHud skillHud; // 실제 스킬 표시
        [SerializeField] private BattleMeasurementRecorder measurement; // 실제 출격 측정
        [SerializeField] private Transform spawn; // 편집 가능한 출격 위치
        [SerializeField] private Sprite sniperSprite; // 저격형 편집 이미지
        public Camera Camera => battleCamera; // 실제 전투 카메라
        public BattlePlayer Player => player; // 실제 전투 플레이어
        public StageDirector Stage => stage; // 실제 스테이지
        public BattleProgress Progress => progress; // 실제 성장 상태
        public SceneBattleHudPresenter Hud => hud; // 실제 전투 HUD
        public bool IsReady => battleCamera != null && player != null && background != null && stage != null && stage.HasSceneTemplates && bulletPool != null && bulletPool.IsConfigured && progress != null && drops != null && hud != null && hitbox != null && hitbox.HasSceneVisual && battleHud != null && skillHud != null && measurement != null && spawn != null && player.GetComponent<SpriteRenderer>() != null && player.GetComponent<SpriteRenderer>().sprite != null && sniperSprite != null && hud.HasSceneReferences && hud.HasPanelReferences; // 필수 씬 연결 확인
        public void ClearAllBullets() // 출격 종료 적탄 전체 회수
        { // 정리 시작
            if (bulletPool != null) // 실제 풀 확인
            { // 회수 시작
                bulletPool.ClearAll(); // 화면 밖과 소거 불가 탄환까지 회수
            } // 회수 종료
        } // 정리 종료
        public bool Prepare(CharacterType character, CommonSkill skill, int seed, out string error) // 저장 전 비활성 전투 준비
        { // 준비 시작
            error = null; // 이전 오류 초기화
            if (!IsReady || gameObject.activeInHierarchy) // 필수 참조와 비활성 상태 확인
            { // 차단 시작
                error = "전투 씬의 필수 객체가 없거나 전투 루트가 활성 상태입니다."; // 실제 오류 안내
                return false; // 잘못된 씬으로 출격 차단
            } // 차단 종료
            player.AssignCamera(battleCamera); // 씬의 실제 카메라 연결
            player.SetSceneSprites(player.GetComponent<SpriteRenderer>().sprite, sniperSprite); // 씬에서 편집한 캐릭터 이미지
            player.Begin(spawn.position, character); // 씬에서 편집한 시작 위치
            player.Skills.Equip(skill); // 선택 공용 스킬 장착
            player.AllowDebugCharacterSwitch = false; // 실제 출격 중 시험 전환 차단
            player.AssignBulletPool(bulletPool); // 씬에 저장된 적탄 풀 연결
            background.Begin(battleCamera, player, false); // 씬에서 편집한 배경 크기 유지
            hitbox.BeginScene(player); // 씬에 저장된 피격 표시 연결
            battleHud.Begin(player, battleCamera, true); // 중복 없는 실제 패널 정보 연결
            skillHud.Begin(player, battleCamera, true); // 중복 없는 실제 패널 스킬 연결
            progress.Begin(player, battleCamera, seed); // 실제 경험치와 카드 연결
            drops.Begin(player, battleCamera, new UpgradeDropEffects(player, progress), seed: seed ^ 1597463007); // 독립 드롭 시드 연결
            stage.Begin(player, battleCamera, bulletPool, progress, null); // 실제 스테이지 연결
            drops.SetStage(stage); // 실제 모달 상태를 드롭 안내에 연결
            measurement.Begin(player, stage, bulletPool, seed); // 실제 출격 측정 연결
            return true; // 저장 가능한 구성 완료
        } // 준비 종료
    } // 구성 종료
} // 영역 종료
