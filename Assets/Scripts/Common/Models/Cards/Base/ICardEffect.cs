using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    public interface ICardEffect
    {
        /// <summary>
        /// 使用者以外に作用する効果かどうか。
        /// 攻撃・毒のように target に作用するものは true、ブロック・強化のように user に作用するものは false。
        /// カードはどれも対象選択を挟むので、これは「どちら側のユニットを選べるか」の判断に使う
        /// (実際にどのユニットが選べるかは BattleCommandQueueUseCase.CanSelectTarget が決める)
        /// </summary>
        bool TargetsOpponent { get; }

        /// <summary>
        /// 選んだ1体ではなく、相手側の生きているユニット全員に作用するか（全体攻撃）。
        /// 対象の選び方は変わらず、実行する側（BattleCommandExecuteUseCase）が全員分 Execute を呼ぶ
        /// </summary>
        bool TargetsAllOpponents { get; }

        /// <summary>
        /// 味方の中から1体を選んで作用するか（敵の味方回復）。敵が使うとき、行動する側（BattlePhaseDirector）が
        /// 一番弱っている味方を target に渡す。false なら敵が使うときの target はいつもどおりプレイヤー
        /// </summary>
        bool ChoosesAlly { get; }

        /// <summary>
        /// カード発動時の処理
        /// </summary>
        /// <param name="user">使用者のステータスモデル</param>
        /// <param name="target">対象のステータスモデル</param>
        void Execute(UnitStatusModel user, UnitStatusModel target);

        /// <summary>
        /// 説明文に差し込む数値を、名前付きで values に足す（例: 攻撃なら "damage"）。
        /// カードの説明文（CardDescription テーブル）は Smart String で {damage} のように書いてあり、
        /// 数値を変えれば説明文も追従する。名前は効果ごとに固有にし、1枚のカードの中でぶつからないようにする
        /// </summary>
        void AddDescriptionValues(IDictionary<string, object> values);
    }
}