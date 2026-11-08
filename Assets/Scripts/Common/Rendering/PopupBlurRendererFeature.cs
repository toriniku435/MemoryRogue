using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Live2DCardRogue.Common.Rendering
{
    // ポップアップの後ろを全画面ぼかすRendererFeature。Popup Camera のRendererに入れて使う。
    //
    // Popup Camera の描画は3つに分かれる:
    //   1. ぼかしの前: Renderer の通常の描画が UI_Popup を描く（一番手前以外の開いているウインドウ）
    //   2. ぼかし: ここまでの絵（下のカメラのマップと、1のウインドウ）をまとめてぼかす（_injectionPoint）
    //   3. ぼかしの後: _frontLayers（UI_PopupTop: 一番手前のウインドウ、トースト、チュートリアル）を描く
    // どのウインドウを3に置くかは PopupStack が開閉に合わせて決める（PopupLayerMember）。
    // 何枚重なってもぼかしは1回だけ。
    // _frontLayers は Renderer の通常の描画（Opaque / Transparent Layer Mask）から外しておくこと。
    // 外し忘れると、ぼかしの前にも描かれてしまう（PopupLayerSetupTests が確かめる）。
    // 2と3の順はFeatureの並び順に頼らず、このFeatureの中で決めている（間に別のFeatureが挟まっても崩れないように）。
    //
    // ぼかしはポップアップが出ている間だけ積む（PopupBlurController）。3の描画は毎回積むが、
    // 何も無ければ描くものが無いだけで、ほぼコストは無い
    public sealed class PopupBlurRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;

        [Tooltip("ぼかしをどのタイミングで割り込ませるか。奥のウインドウ(UI_Popup)を描いた後、一番手前のウインドウを描く前")]
        [SerializeField] private RenderPassEvent _injectionPoint = RenderPassEvent.AfterRenderingTransparents;

        [Tooltip("縮小・拡大の往復回数。増やすほどぼけ幅が広がる")]
        [SerializeField][Range(1, 4)] private int _iterations = 2;

        [Tooltip("1回あたりのサンプル間隔。ぼけ幅の細かい調整に使う")]
        [SerializeField][Range(0.5f, 4f)] private float _offset = 1.5f;

        [Tooltip("ぼかしの後に描くレイヤー（一番手前のウインドウ）。Rendererの通常の描画（Layer Mask）からは外しておく")]
        [SerializeField] private LayerMask _frontLayers;

        private PopupBlurPass _blurPass;
        private DrawLayerPass _frontPass;

        public override void Create()
        {
            _blurPass?.Dispose();
            _blurPass = new PopupBlurPass(_shader, _iterations, _offset) { renderPassEvent = _injectionPoint };
            _frontPass = new DrawLayerPass(_frontLayers, "PopupFront") { renderPassEvent = _injectionPoint + 1 };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView) return;

            if (PopupBlurController.IsActive && _shader != null && _blurPass.IsReady) renderer.EnqueuePass(_blurPass);
            if (_frontLayers.value != 0) renderer.EnqueuePass(_frontPass);
        }

        protected override void Dispose(bool disposing)
        {
            _blurPass?.Dispose();
        }

        // 指定したレイヤーの透明物（UIのCanvas、パーティクル）を、カメラのカラーターゲットへ描く。
        // 並べ方は通常の透明物と同じ（Sorting Layer → Order in Layer → 奥から手前）
        private sealed class DrawLayerPass : ScriptableRenderPass
        {
            // UIのシェーダーにはLightModeが無い（SRPDefaultUnlit として扱われる）。パーティクルは UniversalForward
            private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
            };

            private readonly LayerMask _layers;
            private readonly string _name;

            public DrawLayerPass(LayerMask layers, string name)
            {
                _layers = layers;
                _name = name;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var color = resourceData.activeColorTexture;
                if (!color.IsValid()) return;

                var drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                var filtering = new FilteringSettings(RenderQueueRange.transparent, _layers);
                var listParams = new RendererListParams(renderingData.cullResults, drawing, filtering);

                using var builder = renderGraph.AddRasterRenderPass<DrawPassData>(_name, out var passData);
                passData.RendererList = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.RendererList);
                builder.SetRenderAttachment(color, 0);
                var depth = resourceData.activeDepthTexture;
                if (depth.IsValid()) builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((DrawPassData data, RasterGraphContext context) => context.cmd.DrawRendererList(data.RendererList));
            }

            private sealed class DrawPassData
            {
                public RendererListHandle RendererList;
            }
        }

        private sealed class PopupBlurPass : ScriptableRenderPass
        {
            private static readonly int BlurOffsetId = Shader.PropertyToID("_BlurOffset");
            private static readonly int TexelSizeId = Shader.PropertyToID("_BlurTexelSize");
            private static readonly int StrengthId = Shader.PropertyToID("_BlurStrength");
            private static readonly int RegionId = Shader.PropertyToID("_BlurRegion");
            private static readonly int RegionStrengthId = Shader.PropertyToID("_BlurRegionStrength");
            private static readonly int RegionEnabledId = Shader.PropertyToID("_BlurRegionEnabled");

            private const int DownPass = 0;
            private const int UpPass = 1;
            // 最後の拡大。元の絵に、強さぶんだけ重ねる
            private const int UpBlendPass = 2;

            private readonly int _iterations;
            private readonly float _offset;
            private readonly Material _material;

            public PopupBlurPass(Shader shader, int iterations, float offset)
            {
                _iterations = iterations;
                _offset = offset;

                if (shader != null) _material = CoreUtils.CreateEngineMaterial(shader);
            }

            public bool IsReady => _material != null;

            public void Dispose()
            {
                CoreUtils.Destroy(_material);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var source = resourceData.activeColorTexture;
                if (!source.IsValid()) return;

                var sourceDescriptor = renderGraph.GetTextureDesc(source);
                var sizes = new List<Vector2Int>();

                // 縮小しながらぼかす
                var current = source;
                int width = sourceDescriptor.width;
                int height = sourceDescriptor.height;

                for (int i = 0; i < _iterations; i++)
                {
                    sizes.Add(new Vector2Int(width, height));
                    width = Mathf.Max(1, width >> 1);
                    height = Mathf.Max(1, height >> 1);

                    var target = CreateTexture(renderGraph, sourceDescriptor, width, height, "PopupBlurDown" + i);
                    Blit(renderGraph, current, target, DownPass, sizes[i]);
                    current = target;
                }

                // 拡大しながら戻す。最後の1回だけは元のカラーターゲットへ直接描く
                for (int i = _iterations - 1; i >= 0; i--)
                {
                    var size = sizes[i];
                    var sourceSize = new Vector2Int(Mathf.Max(1, size.x >> 1), Mathf.Max(1, size.y >> 1));

                    var target = i == 0
                        ? source
                        : CreateTexture(renderGraph, sourceDescriptor, size.x, size.y, "PopupBlurUp" + i);

                    Blit(renderGraph, current, target, i == 0 ? UpBlendPass : UpPass, sourceSize);
                    current = target;
                }
            }

            private static TextureHandle CreateTexture(RenderGraph renderGraph, TextureDesc source, int width, int height, string name)
            {
                var descriptor = source;
                descriptor.name = name;
                descriptor.width = width;
                descriptor.height = height;
                descriptor.clearBuffer = false;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = MSAASamples.None;
                return renderGraph.CreateTexture(descriptor);
            }

            // RenderGraphUtils.AddBlitPassではなく自前のRasterPassにしているのは、
            // カリングを明示的に切るため（カメラのカラーターゲットへの書き込みが
            // 「誰も読まない」と判断されて丸ごと消えることがある）。
            // サンプル元の解像度はパスごとに変わるので、描く直前にマテリアルへ渡す
            private void Blit(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, int shaderPass, Vector2Int sourceSize)
            {
                using var builder = renderGraph.AddRasterRenderPass<PassData>("PopupBlur", out var passData);

                passData.Source = source;
                passData.Material = _material;
                passData.ShaderPass = shaderPass;
                passData.Offset = _offset;
                passData.Strength = PopupBlurController.Strength;
                passData.RegionEnabled = PopupBlurController.HasRegion ? 1f : 0f;
                passData.Region = PopupBlurController.Region;
                passData.RegionStrength = PopupBlurController.RegionStrength;
                passData.TexelSize = new Vector4(1f / sourceSize.x, 1f / sourceSize.y, sourceSize.x, sourceSize.y);

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.Material.SetFloat(BlurOffsetId, data.Offset);
                    data.Material.SetVector(TexelSizeId, data.TexelSize);
                    data.Material.SetFloat(StrengthId, data.Strength);
                    data.Material.SetFloat(RegionEnabledId, data.RegionEnabled);
                    data.Material.SetVector(RegionId, data.Region);
                    data.Material.SetFloat(RegionStrengthId, data.RegionStrength);
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1f, 1f, 0f, 0f), data.Material, data.ShaderPass);
                });
            }

            private sealed class PassData
            {
                public TextureHandle Source;
                public Material Material;
                public int ShaderPass;
                public float Offset;
                public float Strength;
                public float RegionEnabled;
                public Vector4 Region;
                public float RegionStrength;
                public Vector4 TexelSize;
            }
        }
    }
}
