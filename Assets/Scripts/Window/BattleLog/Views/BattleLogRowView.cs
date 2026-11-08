using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.Window.BattleLog.Views
{
    // バトルログの1行（Assets/Prefabs/BattleLogRowView.prefab）。行動中のログとログウインドウの両方で使う。
    // 左端に主語のユニットの顔、敵味方で文字の色を変える（味方シアン・敵マゼンタ・ターンの区切りは黄）。
    // カードを使った行の下にぶら下がる結果の行は字下げし、結果に添える状態効果の詳しい説明はもう一段下げる
    public sealed class BattleLogRowView : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private Image _icon;
        [SerializeField] private CommonText _text;
        // 敵味方を示す左端の細い帯
        [SerializeField] private Image _sideBar;

        [SerializeField] private Color _playerColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _enemyColor = new Color(1f, 0.18f, 0.77f, 1f);
        [SerializeField] private Color _headerColor = new Color(1f, 0.9f, 0f, 1f);
        // 結果の行・詳しい説明の行は少し控えめに（行動の行より目立たないように）
        [SerializeField][Range(0f, 1f)] private float _resultAlpha = 0.8f;
        [SerializeField][Range(0f, 1f)] private float _detailAlpha = 0.6f;
        [SerializeField] private float _indent = 32f;

        public RectTransform RectTransform => (RectTransform)transform;

        public void Bind(BattleLogLine line)
        {
            var color = line.Side == BattleLogSide.Player ? _playerColor
                : line.Side == BattleLogSide.Enemy ? _enemyColor
                : _headerColor;

            _text.text = line.Text;
            float alpha = line.Indent >= 2 ? _detailAlpha : line.Indent == 1 ? _resultAlpha : 1f;
            _text.color = new Color(color.r, color.g, color.b, alpha);
            _text.alignment = line.Side == BattleLogSide.Neutral ? TMPro.TextAlignmentOptions.Center : TMPro.TextAlignmentOptions.Left;

            _icon.sprite = line.Icon;
            _icon.gameObject.SetActive(line.Icon != null);

            // 詳しい説明の行は帯を出さず、上の行にぶら下がって見えるようにする
            _sideBar.gameObject.SetActive(line.Side != BattleLogSide.Neutral && line.Indent < 2);
            _sideBar.color = color;

            _content.offsetMin = new Vector2(line.Indent * _indent, _content.offsetMin.y);
        }
    }
}
