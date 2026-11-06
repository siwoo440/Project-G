Shader "ProjectG/StageBackgroundScroll" // 하늘섬 배경의 연속 이동
{ // 셰이더 시작
    Properties // 배경 표시 속성
    { // 속성 시작
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" // 원본 배경 이미지
        { // 이미지 기본 옵션 시작
        } // 이미지 기본 옵션 종료
        _ScrollPhase("Scroll Phase", Float) = 0 // 반복 이동 위치
    } // 속성 종료
    SubShader // 유니버설 렌더 파이프라인 표시
    { // 렌더 시작
        Tags // 투명 스프라이트 순서
        { // 태그 시작
            "Queue"="Transparent" // 투명 렌더 순서
            "RenderType"="Transparent" // 투명 표시 분류
            "RenderPipeline"="UniversalPipeline" // 유니버설 파이프라인
        } // 태그 종료
        Cull Off // 양면 표시
        ZWrite Off // 깊이 기록 제외
        Blend SrcAlpha OneMinusSrcAlpha // 배경 투명도 혼합
        Pass // 유니버설 2D 표시 단계
        { // 단계 시작
            Tags // 2D 렌더러 연결
            { // 태그 시작
                "LightMode"="Universal2D" // 유니버설 2D 표시 단계
            } // 태그 종료
            HLSLPROGRAM // 셰이더 코드 시작
            #pragma vertex Vert // 꼭짓점 함수 지정
            #pragma fragment Frag // 픽셀 함수 지정
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 유니버설 좌표 기능
            TEXTURE2D(_MainTex); // 배경 이미지 선언
            SAMPLER(sampler_MainTex); // 이미지 추출 설정
            CBUFFER_START(UnityPerMaterial) // 재질별 상수 시작
            float _ScrollPhase; // 이미지의 반복 위치
            CBUFFER_END // 재질별 상수 종료
            struct Attributes // 꼭짓점 입력
            { // 입력 시작
                float4 positionOS : POSITION; // 물체 기준 위치
                float2 uv : TEXCOORD0; // 원본 이미지 좌표
                half4 color : COLOR; // 스프라이트 색상
            }; // 입력 종료
            struct Varyings // 픽셀 전달 값
            { // 전달 시작
                float4 positionCS : SV_POSITION; // 화면 투영 위치
                float2 uv : TEXCOORD0; // 이미지 좌표
                half4 color : COLOR; // 스프라이트 색상
            }; // 전달 종료
            Varyings Vert(Attributes input) // 꼭짓점 화면 변환
            { // 변환 시작
                Varyings output; // 전달값 준비
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); // 화면 좌표 변환
                output.uv = input.uv; // 이미지 좌표 전달
                output.color = input.color; // 스프라이트 색상 전달
                return output; // 변환 결과 반환
            } // 변환 종료
            half4 Frag(Varyings input) : SV_Target // 배경 이미지 혼합
            { // 혼합 시작
                float firstY = frac(input.uv.y + _ScrollPhase); // 아래쪽 이동 좌표
                float secondY = frac(firstY + 0.5); // 같은 방향의 보조 좌표
                float blend = smoothstep(0.1, 0.3, firstY) * (1.0 - smoothstep(0.7, 0.9, firstY)); // 반복 경계의 부드러운 혼합
                half4 first = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(input.uv.x, firstY)); // 주 배경 표본
                half4 second = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(input.uv.x, secondY)); // 보조 배경 표본
                return lerp(second, first, blend) * input.color; // 방향을 유지한 배경 색상
            } // 혼합 종료
            ENDHLSL // 셰이더 코드 종료
        } // 단계 종료
    } // 렌더 종료
} // 셰이더 종료
