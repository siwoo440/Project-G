Shader "ProjectG/Outer Islands Sky" // 하늘과 먼 해안의 배경
{
    Properties // 스테이지 머티리얼에서 편집할 값
    {
        _SurfaceMap ("Surface Map", 2D) = "white" {} // 실제 해안의 탑뷰 이미지
        _ZenithTint ("Zenith", Color) = (0.19, 0.39, 0.63, 1) // 하늘 위쪽의 색
        _HorizonTint ("Horizon", Color) = (0.68, 0.78, 0.88, 1) // 먼 수평선의 대기색
        _PlaneAltitude ("Surface Altitude", Float) = -165 // 배경으로 보이는 지표 고도
        _MapSpan ("Map Width", Float) = 1650 // 이미지가 덮는 가로 길이
        _MapCenter ("Map Center XZ", Vector) = (-150, -120, 0, 0) // 해안이 보이는 지표 중심
    }
    SubShader // 카메라의 하늘 렌더링 패스
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } // 하늘로 처리하는 렌더 순서
        Cull Off // 모든 방향의 하늘 표시
        ZWrite Off // 섬과 적탄 깊이를 가로막지 않음

        Pass // 하늘과 지표의 단일 패스
        {
            CGPROGRAM // Unity 하늘 셰이더
            #pragma vertex Vert // 배경 꼭짓점 처리
            #pragma fragment Frag // 하늘 방향의 색 계산
            #include "UnityCG.cginc" // Unity 카메라와 월드 좌표

            sampler2D _SurfaceMap; // 멀리서 내려다본 바다와 육지
            half4 _ZenithTint; // 하늘 상단의 색
            half4 _HorizonTint; // 수평선과 원경 안개의 색
            float _PlaneAltitude; // 지표가 있는 월드 높이
            float _MapSpan; // 지표의 전체 월드 너비
            float4 _MapCenter; // 지표의 월드 기준 중심

            struct VertexInput // 카메라가 여는 하늘 메시 데이터
            {
                float4 vertexPosition : POSITION; // 하늘 메시의 꼭짓점
            };

            struct Varyings // 픽셀까지 전달할 하늘 좌표
            {
                float4 clip : SV_POSITION; // 현재 화면 위치
                float3 ray : TEXCOORD0; // 카메라의 하늘 쪽 방향
            };

            Varyings Vert(VertexInput input) // 화면과 하늘 방향 변환
            {
                Varyings output; // 픽셀 셰이더의 입력값
                output.clip = UnityObjectToClipPos(input.vertexPosition); // 실제 하늘 화면 위치
                output.ray = input.vertexPosition.xyz; // 로컬 하늘 방향
                return output; // 두 좌표 전달
            }

            half4 Frag(Varyings input) : SV_Target // 하늘 그라데이션과 지표 투영
            {
                float3 ray = normalize(input.ray); // 하늘 한 픽셀의 단위 방향
                half3 atmosphere = lerp(_HorizonTint.rgb, _ZenithTint.rgb, smoothstep(0.0, 0.82, ray.y)); // 부드럽게 이어지는 푸른 하늘
                float down = max(0.0, -ray.y); // 지표를 향하는 시선의 정도
                float height = max(1.0, _WorldSpaceCameraPos.y - _PlaneAltitude); // 지표에서 카메라까지의 높이
                float travel = height / max(0.001, down); // 시선과 낮은 지표가 만나는 거리
                float2 surface = _WorldSpaceCameraPos.xz + ray.xz * travel; // 카메라 이동에 따라 달라지는 실제 지표 좌표
                float2 uv = (surface - _MapCenter.xy) / max(1.0, _MapSpan) + 0.5; // 지표 이미지의 샘플 좌표
                float2 edge = min(uv, 1.0 - uv); // 이미지 바깥의 경계까지 남은 거리
                half mapBlend = smoothstep(0.0, 0.10, min(edge.x, edge.y)); // 지표 이미지의 끝부분을 대기에 혼합
                half clear = smoothstep(0.03, 0.58, down) * mapBlend; // 지평선에서는 흐리고 아래에서는 선명한 대기
                half3 land = tex2D(_SurfaceMap, saturate(uv)).rgb; // 투영한 바다와 해안의 색
                return half4(lerp(atmosphere, land, clear * 0.92), 1); // 하늘과 낮은 지표를 하나의 배경으로 표시
            }
            ENDCG // 하늘 셰이더 종료
        }
    }
    Fallback Off // 다른 셰이더로 대체하지 않음
}
