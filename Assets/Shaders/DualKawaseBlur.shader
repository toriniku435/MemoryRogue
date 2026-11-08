// ポップアップ背後にかけるブラー。Dual Kawase（縮小しながらぼかし、拡大しながら戻す）。
// ガウシアンより少ないサンプル数で大きなぼけ幅が出せるので、全画面にかけても軽い。
Shader "Hidden/Live2DCardRogue/DualKawaseBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Blitterが使うのはCore側のBlit.hlsl（Runtime/Utilities以下。ShaderLibrary以下ではない）。
        // Varyings / _BlitTexture / 全画面三角形の頂点シェーダーVertはここで定義されている
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurOffset;

        // 入力テクセルサイズ。縮小・拡大で解像度が変わるので毎パス渡す
        float4 _BlurTexelSize;

        // ぼかしの強さ(0..1)。最後の拡大パスだけが使い、元の絵とこの割合で混ぜる
        float _BlurStrength;

        // 矩形の中だけ別の強さにする（ウインドウを重ねて開閉するとき、すぐ後ろのウインドウだけを徐々にぼかす・戻す）。
        // _BlurRegion は書き込み先の UV で (最小x, 最小y, 最大x, 最大y)。_BlurRegionEnabled が 0 なら使わない
        float4 _BlurRegion;
        float _BlurRegionStrength;
        float _BlurRegionEnabled;

        float4 SampleAt(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        }

        float4 UpSample(float2 uv)
        {
            float2 halfPixel = _BlurTexelSize.xy * 0.5;
            float2 offset = halfPixel * _BlurOffset;

            float4 sum = SampleAt(uv + float2(-offset.x * 2.0, 0.0));
            sum += SampleAt(uv + float2(-offset.x, offset.y)) * 2.0;
            sum += SampleAt(uv + float2(0.0, offset.y * 2.0));
            sum += SampleAt(uv + float2(offset.x, offset.y)) * 2.0;
            sum += SampleAt(uv + float2(offset.x * 2.0, 0.0));
            sum += SampleAt(uv + float2(offset.x, -offset.y)) * 2.0;
            sum += SampleAt(uv + float2(0.0, -offset.y * 2.0));
            sum += SampleAt(uv + float2(-offset.x, -offset.y)) * 2.0;
            return sum / 12.0;
        }
        ENDHLSL

        // 縮小しながらぼかす
        Pass
        {
            Name "DualKawaseDown"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDown

            float4 FragDown(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 halfPixel = _BlurTexelSize.xy * 0.5;
                float2 offset = halfPixel * _BlurOffset;

                float4 sum = SampleAt(uv) * 4.0;
                sum += SampleAt(uv - offset);
                sum += SampleAt(uv + offset);
                sum += SampleAt(uv + float2(offset.x, -offset.y));
                sum += SampleAt(uv - float2(offset.x, -offset.y));
                return sum / 8.0;
            }
            ENDHLSL
        }

        // 拡大しながらぼかす
        Pass
        {
            Name "DualKawaseUp"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUp

            float4 FragUp(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return UpSample(input.texcoord);
            }
            ENDHLSL
        }

        // 最後の拡大。元の絵(書き込み先にまだ残っている)の上に、強さぶんだけ重ねる。
        // 書き込み先のアルファはそのまま残す
        Pass
        {
            Name "DualKawaseUpBlend"
            Blend SrcAlpha OneMinusSrcAlpha, Zero One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpBlend

            float4 FragUpBlend(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = UpSample(input.texcoord);
                float2 uv = input.texcoord;
                float2 inside = step(_BlurRegion.xy, uv) * step(uv, _BlurRegion.zw);
                float strength = lerp(_BlurStrength, _BlurRegionStrength, inside.x * inside.y * step(0.5, _BlurRegionEnabled));
                return float4(color.rgb, saturate(strength));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
