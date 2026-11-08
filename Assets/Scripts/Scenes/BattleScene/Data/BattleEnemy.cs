using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Data
{
    // 戦闘に出ている敵1体分。使うカードと行動回数を持つアセット(Data)と、
    // その1体のHP・状態効果(Status)を結び付けるだけの入れ物。
    // 同じEnemyDataを複数体が共有できる（Statusだけが1体ごとに作られる）
    public sealed class BattleEnemy
    {
        public EnemyData Data { get; }
        public UnitStatusModel Status { get; }

        public bool IsDefeated => Status.CurrentHp == 0;

        /// <summary>
        /// ほかの敵が生成するまで戦場にいない枠（ボスが出すボットネット）。
        /// 戦闘の始めから倒れた状態で用意しておき、生成で戻す。倒されたらまた生成を待つ
        /// </summary>
        public bool IsReserve { get; }

        /// <summary>生きている味方がいないまま終えた、自分のターンの数（行動を決めるときの戦況として渡す）</summary>
        public int TurnsAlone { get; private set; }

        /// <summary>
        /// 次の敵の番に使う予定のカード（実行順）。プレイヤーの番の頭で決め、画面に予告する。
        /// 停止などで実際に行う数が減ることはあるが、予定そのものは変わらない
        /// </summary>
        public IReadOnlyList<CardData> PlannedCards { get; private set; } = Array.Empty<CardData>();

        public event Action OnPlanChanged;

        public void SetPlan(IReadOnlyList<CardData> cards)
        {
            PlannedCards = cards ?? Array.Empty<CardData>();
            OnPlanChanged?.Invoke();
        }

        /// <summary>自分のターンを終えた。そのとき生きている味方がいなければ独りのターンとして数え、いれば数え直す</summary>
        public void CountTurnEnd(bool hasLivingAlly)
        {
            TurnsAlone = hasLivingAlly ? 0 : TurnsAlone + 1;
        }

        public BattleEnemy(EnemyData data, UnitStatusModel status, bool isReserve = false)
        {
            Data = data;
            Status = status;
            IsReserve = isReserve;
        }
    }
}
