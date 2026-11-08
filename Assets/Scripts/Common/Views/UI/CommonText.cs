using TMPro;
using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // プロジェクト内のテキストはすべてこのクラスを使う。
    // TextMeshProUGUIを直接使わず1枚かぶせておくことで、全テキスト共通の振る舞い
    // (既定フォント、ローカライズ時の字詰めなど)を後から足すときに、
    // シーン・プレハブ側のコンポーネント差し替えが不要になる。
    // 中身が空なのはまだ共通化したい振る舞いが無いからで、意図的。
    [AddComponentMenu("UI (Canvas)/Common Text")]
    [ExecuteAlways] // 基底クラスにも付いているが、編集中の再描画に必須なので明示しておく
    public class CommonText : TextMeshProUGUI
    {
    }
}
