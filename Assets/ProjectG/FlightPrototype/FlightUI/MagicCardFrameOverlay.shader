Shader "ProjectG/UI/CardFrame" // 첨부 카드 틀의 검은 중심 투과
{
    Properties
    {
        [PerRendererData] _MainTex ("Card frame", 2D) = "white" {} // 편집 가능한 틀 원본
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "CanUseSpriteAtlas" = "True" }
        Cull Off // 양방향 UI 표시
        ZWrite Off // 깊이 쓰기 제외
        ZTest [unity_GUIZTestMode] // 오버레이 깊이 모드
        Blend SrcAlpha OneMinusSrcAlpha // 틀과 아래 그림 합성
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            sampler2D _MainTex; // 사용자 제공 프레임 PNG

            struct Input
            {
                float4 vertex : POSITION; // UI 정점
                float4 color : COLOR; // 그룹 투명도
                float2 uv : TEXCOORD0; // 프레임 원본 좌표
            };

            struct Pixel
            {
                float4 position : SV_POSITION; // 실제 화면 공간
                float4 color : COLOR; // 카드 그룹의 표시색
                float2 uv : TEXCOORD0; // 해당 프레임의 원본 영역
            };

            Pixel Vertex(Input input) // 실제 화면 정점 계산
            {
                Pixel pixel; // 출력 정점
                pixel.position = UnityObjectToClipPos(input.vertex); // 화면 좌표 변환
                pixel.color = input.color; // 카드 투명도 복사
                pixel.uv = input.uv; // 원본 위치 복사
                return pixel; // 그려질 다음 단계 정점
            }

            fixed4 Fragment(Pixel pixel) : SV_Target // 금색 틀과 마법 그림 합성
            {
                fixed4 frame = tex2D(_MainTex, pixel.uv); // 원본 틀의 현재 픽셀
                float light = max(max(frame.r, frame.g), frame.b); // 검은 가운데와 보라색 틀 구분
                frame.a *= smoothstep(0.012, 0.035, light); // 검은 영역만 낮춘 투명도
                return frame * pixel.color; // 카드의 현재 상태 밝기와 알파
            }
            ENDCG
        }
    }
}
