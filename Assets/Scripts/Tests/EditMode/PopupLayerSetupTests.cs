using System.Linq;
using Live2DCardRogue.Common.Rendering;
using Live2DCardRogue.Common.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // 一番手前のウインドウだけをぼかしの後に描く仕組みは、アセットの設定の組み合わせで成り立っている。
    //   - Popup Camera は UI_Popup と UI_PopupTop の両方を映す
    //   - Popup の Renderer の通常の描画からは UI_PopupTop を外す（外し忘れると、ぼかしの前にも描かれる）
    //   - PopupBlurRendererFeature がぼかしの後に UI_PopupTop を描く（入れ忘れると、一番手前のウインドウが消える）
    //   - ウインドウのプレハブのルートに PopupLayerMember と入れ子のCanvasがある（無いと段が切り替わらない）
    // どれも崩れてもエラーにならず見た目だけが変わるので、ここで縛る
    public sealed class PopupLayerSetupTests
    {
        private const string PopupRendererPath = "Assets/Settings/UniversalRenderer_Popup.asset";
        private const string CameraRigPath = "Assets/Prefabs/UICameraRig.prefab";

        private static int FrontLayer => LayerMask.NameToLayer(PopupLayerMember.FrontLayerName);
        private static int BackLayer => LayerMask.NameToLayer(PopupLayerMember.BackLayerName);

        [Test]
        public void Layers_Exist()
        {
            Assert.GreaterOrEqual(FrontLayer, 0, $"レイヤー {PopupLayerMember.FrontLayerName} がありません");
            Assert.GreaterOrEqual(BackLayer, 0, $"レイヤー {PopupLayerMember.BackLayerName} がありません");
        }

        [Test]
        public void PopupRenderer_DefaultDrawing_ExcludesFrontLayer()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PopupRendererPath);
            Assert.IsNotNull(rendererData, PopupRendererPath);
            var so = new SerializedObject(rendererData);

            foreach (var mask in new[] { "m_OpaqueLayerMask", "m_TransparentLayerMask" })
            {
                uint bits = so.FindProperty(mask).FindPropertyRelative("m_Bits").uintValue;
                Assert.AreEqual(0u, bits & (1u << FrontLayer), $"{mask} に {PopupLayerMember.FrontLayerName} が入っています（ぼかしの前にも描かれてしまう）");
                Assert.AreNotEqual(0u, bits & (1u << BackLayer), $"{mask} に {PopupLayerMember.BackLayerName} が入っていません");
            }
        }

        [Test]
        public void PopupBlurFeature_DrawsFrontLayerAfterTheBlur()
        {
            // 型で探すとURPのアセンブリへの参照が要るので、名前で探す
            var feature = AssetDatabase.LoadAllAssetsAtPath(PopupRendererPath).FirstOrDefault(asset => asset != null && asset.GetType().Name == "PopupBlurRendererFeature");
            Assert.IsNotNull(feature, "Popup の Renderer に PopupBlurRendererFeature がありません");

            var so = new SerializedObject(feature);
            uint bits = so.FindProperty("_frontLayers").FindPropertyRelative("m_Bits").uintValue;
            Assert.AreEqual(1u << FrontLayer, bits, $"ぼかしの後に描くのは {PopupLayerMember.FrontLayerName} だけのはずです");
        }

        [Test]
        public void PopupCamera_SeesBothPopupLayers()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPath).GetComponent<UICameraRig>();
            Assert.IsNotNull(rig.PopupCamera);
            int mask = rig.PopupCamera.cullingMask;
            Assert.AreNotEqual(0, mask & (1 << BackLayer), $"Popup Camera が {PopupLayerMember.BackLayerName} を映していません");
            Assert.AreNotEqual(0, mask & (1 << FrontLayer), $"Popup Camera が {PopupLayerMember.FrontLayerName} を映していません");
        }

        // ウインドウ（WindowTransition）を持つプレハブは、ルートに PopupLayerMember と入れ子のCanvas（とクリック用のRaycaster）を持つ
        [Test]
        public void EveryWindowPrefab_HasALayerMemberAtItsRoot()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/Resources" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var transition in prefab.GetComponentsInChildren<WindowTransition>(true))
                {
                    var member = transition.GetComponentInParent<PopupLayerMember>(true);
                    Assert.IsNotNull(member, $"{path} の {transition.name}: 親に PopupLayerMember がありません");
                    Assert.IsNotNull(member.GetComponent<UnityEngine.UI.GraphicRaycaster>(), $"{path} の {member.name}: GraphicRaycaster がありません（入れ子のCanvasの中がクリックできない）");
                }
            }
        }
    }
}
