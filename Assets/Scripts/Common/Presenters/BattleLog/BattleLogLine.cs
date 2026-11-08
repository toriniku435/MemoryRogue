using UnityEngine;

namespace Live2DCardRogue.Common.Presenters
{
    // バトルログの1行を、画面に出す形にしたもの。BattleLogFormatter が BattleLogEntry から作る
    public readonly struct BattleLogLine
    {
        public string Text { get; }

        /// <summary>誰の行動か。View はこれで色を変える（味方・敵・ターンの区切り）</summary>
        public BattleLogSide Side { get; }

        /// <summary>字下げの段。0: 行動・ターンの区切り、1: その結果、2: 結果に添える詳しい説明</summary>
        public int Indent { get; }

        /// <summary>行の主語のユニットの顔。ターンの区切りでは null</summary>
        public Sprite Icon { get; }

        public BattleLogLine(string text, BattleLogSide side, int indent, Sprite icon)
        {
            Text = text;
            Side = side;
            Indent = indent;
            Icon = icon;
        }
    }

    public enum BattleLogSide
    {
        Neutral,
        Player,
        Enemy,
    }
}
