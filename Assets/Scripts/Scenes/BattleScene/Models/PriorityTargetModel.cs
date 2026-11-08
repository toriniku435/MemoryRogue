using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.BattleScene.Models
{
    // コマンドをクリックしただけで使うときに狙う敵（優先ターゲット）。
    // プレイヤーが敵ユニットをクリックして選ぶ。選んでいない、または選んだ敵が倒れたときは、生きている中で一番手前の敵
    public sealed class PriorityTargetModel
    {
        private readonly Func<IReadOnlyList<UnitStatusModel>> _getLivingOpponents;

        // プレイヤーが選んだ敵（倒れていても覚えておく。Current が倒れた敵を返すことはない）
        private UnitStatusModel _chosen;

        /// <summary>プレイヤーが優先ターゲットを選び直した（倒れて手前の敵に移ったときは発火しない）</summary>
        public event Action OnChanged;

        public PriorityTargetModel(Func<IReadOnlyList<UnitStatusModel>> getLivingOpponents)
        {
            _getLivingOpponents = getLivingOpponents;
        }

        /// <summary>今の優先ターゲット。生きている敵がいなければ null</summary>
        public UnitStatusModel Current
        {
            get
            {
                var living = _getLivingOpponents?.Invoke();
                if (living == null || living.Count == 0) return null;
                return _chosen != null && Contains(living, _chosen) ? _chosen : living[0];
            }
        }

        /// <summary>生きている敵の数（1体なら選ぶ意味が無いので、表示側は印を出さない）</summary>
        public int LivingCount => _getLivingOpponents?.Invoke()?.Count ?? 0;

        /// <summary>生きている敵を優先ターゲットにする。敵でない・倒れている・すでに優先ターゲットなら何もせず false</summary>
        public bool Select(UnitStatusModel unit)
        {
            if (unit == null) return false;
            var living = _getLivingOpponents?.Invoke();
            if (living == null || !Contains(living, unit) || unit == Current) return false;

            _chosen = unit;
            OnChanged?.Invoke();
            return true;
        }

        private static bool Contains(IReadOnlyList<UnitStatusModel> units, UnitStatusModel unit)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] == unit) return true;
            }
            return false;
        }
    }
}
