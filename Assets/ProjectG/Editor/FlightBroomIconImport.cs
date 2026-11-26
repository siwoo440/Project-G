using System; // 리소스 경로의 안전한 비교
using UnityEditor; // 부품 이미지 가져오기 설정
using UnityEngine; // 선명한 축소 필터

namespace ProjectG.Editor // 프로젝트 편집기 전용 영역
{
    internal sealed class FlightBroomIconImport : AssetPostprocessor // 도감 이미지의 원본 크기와 투명도 보존
    {
        private const string Folder = "Assets/ProjectG/Resources/BroomIcons/"; // 부품 도감 전용 이미지 경로

        private void OnPreprocessTexture() // Unity가 실제 이미지를 가져오기 전 설정
        {
            if (!assetPath.StartsWith(Folder, StringComparison.OrdinalIgnoreCase)) // 다른 리소스의 기존 설정
            {
                return; // 사용자와 다른 UI 이미지 변경 금지
            }
            var texture = (TextureImporter)assetImporter; // 해당 부품 이미지의 가져오기 설정
            texture.textureType = TextureImporterType.Default; // 원본 텍스처를 실제 도감에서 읽기
            texture.npotScale = TextureImporterNPOTScale.None; // 1536 등 원본 가로세로 비율 보존
            texture.maxTextureSize = 2048; // 현재 제작된 원본 해상도 유지
            texture.mipmapEnabled = true; // 작은 화면의 계단 현상 완화
            texture.filterMode = FilterMode.Trilinear; // 축소 시 부드럽고 선명한 필터
            texture.alphaIsTransparency = true; // 투명 가장자리의 색상 보존
            texture.textureCompression = TextureImporterCompression.Uncompressed; // 아이콘의 세부 무늬 손실 방지
        }

        internal static void ReimportAll() // 이미 가져온 도감 이미지도 같은 설정 적용
        {
            string[] ids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ProjectG/Resources/BroomIcons" }); // 도감 폴더의 실제 이미지
            foreach (string id in ids) // 해당 폴더의 각 텍스처
            {
                string path = AssetDatabase.GUIDToAssetPath(id); // 실제 부품 이미지 경로
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // 전용 가져오기 설정 재적용
            }
        }
    }
}
