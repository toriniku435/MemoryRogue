using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.TitleScene.Views
{
    // モード選択ボタン1つ分。自分がどのモードのボタンかだけを知っていて、
    // 押されたときに何が起きるかは知らない（Presenterがボタンに紐付ける）
    public sealed class TitleModeButtonView : MonoBehaviour
    {
        [SerializeField] private GameMode _mode;
        [SerializeField] private CommonButton _button;
        [SerializeField] private CommonText _label;

        public GameMode Mode => _mode;
        public CommonButton Button => _button;

        public void SetLabel(string label)
        {
            _label.text = label;
        }
    }
}
