#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;

namespace Live2DCardRogue.Common.DebugMenu
{
    // 決まった文字の並び（隠しコマンド）が入力されたかを判定する。入力の読み取りは持たず、
    // 1文字ずつ受け取るだけなので、UnityEngine に依存せずテストできる（キーの数字も、画面の隅のタップも文字にして渡す）。
    //
    // 直近の入力が並びと一致した時点で true を返し、入力をいったん空にする
    // （"070707" のように続けて打っても、重なった部分で二度反応しないように）。
    //
    // maxDuration を渡すと、並びの最初から最後までがその秒数に収まったときだけ一致とみなす
    // （隅のタップは遊んでいる最中にも起きるので、時間をかけて偶然揃ったものでは開かないように）
    public sealed class DebugCommandSequence
    {
        private readonly string _sequence;
        private readonly float _maxDuration;
        private readonly StringBuilder _recent = new StringBuilder();
        private readonly List<float> _times = new List<float>();

        public DebugCommandSequence(string sequence, float maxDuration = float.PositiveInfinity)
        {
            _sequence = sequence ?? string.Empty;
            _maxDuration = maxDuration;
        }

        /// <summary>1文字入力する。これで並びが揃ったら true（時間の制限は見ない）</summary>
        public bool Input(char c)
        {
            return Input(c, 0f);
        }

        /// <summary>1文字を、入力した時刻（秒）とともに入力する。これで並びが制限時間内に揃ったら true</summary>
        public bool Input(char c, float time)
        {
            if (_sequence.Length == 0) return false;

            _recent.Append(c);
            _times.Add(time);
            if (_recent.Length > _sequence.Length)
            {
                int overflow = _recent.Length - _sequence.Length;
                _recent.Remove(0, overflow);
                _times.RemoveRange(0, overflow);
            }

            if (_recent.ToString() != _sequence) return false;
            if (time - _times[0] > _maxDuration) return false;

            _recent.Clear();
            _times.Clear();
            return true;
        }
    }
}
#endif
