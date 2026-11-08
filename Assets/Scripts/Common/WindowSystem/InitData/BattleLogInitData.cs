using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 戦闘全体のログ
    public sealed class BattleLogInitData : WindowInitData
    {
        public override WindowId Id => WindowId.BattleLog;

        /// <summary>戦闘のログ（戦闘が書き込み続ける）</summary>
        public BattleLogModel Log { get; }

        /// <summary>ログの1件を表示する文にする</summary>
        public BattleLogFormatter Formatter { get; }

        public BattleLogInitData(BattleLogModel log, BattleLogFormatter formatter)
        {
            Log = log;
            Formatter = formatter;
        }
    }
}
