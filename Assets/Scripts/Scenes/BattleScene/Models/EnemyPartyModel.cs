using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.BattleScene.Models
{
    // 戦闘に出ている敵の並び。手前が先頭で、この並びがそのまま行動順になる。
    // 「次の敵は誰か」を知っているのはここだけで、行動順(Director)も対象の振り直し(UseCase経由)も同じ答えを見る
    public sealed class EnemyPartyModel
    {
        private readonly List<BattleEnemy> _enemies;

        // 倒れた瞬間を数えるため、敵ごとに直前まで生きていたかを覚えておく
        private readonly Dictionary<BattleEnemy, bool> _wasAlive = new Dictionary<BattleEnemy, bool>();

        public EnemyPartyModel(IEnumerable<BattleEnemy> enemies)
        {
            _enemies = new List<BattleEnemy>(enemies);
            foreach (var enemy in _enemies)
            {
                var captured = enemy;
                _wasAlive[enemy] = !enemy.IsDefeated;
                enemy.Status.OnHpChanged += (current, _) => HandleHpChanged(captured, current);
                enemy.Status.OnRevived += () => _wasAlive[captured] = true;
            }
        }

        /// <summary>
        /// この戦闘で倒した敵の数。生きていた敵のHPが0になるたびに数えるので、生成で戻ってきた敵は倒すたびに増える。
        /// 生成を待つ枠は倒れた状態で作られるが、その分は数えない
        /// </summary>
        public int DefeatedCount { get; private set; }

        private void HandleHpChanged(BattleEnemy enemy, int currentHp)
        {
            bool alive = currentHp > 0;
            if (_wasAlive[enemy] && !alive) DefeatedCount++;
            _wasAlive[enemy] = alive;
        }

        public IReadOnlyList<BattleEnemy> Enemies => _enemies;

        public bool AreAllDefeated
        {
            get
            {
                foreach (var enemy in _enemies)
                {
                    if (!enemy.IsDefeated) return false;
                }

                return true;
            }
        }

        /// <summary>まだ生きている敵のステータスを、手前から順に並べた新しいリストで返す（全体攻撃の当たり先）</summary>
        public IReadOnlyList<UnitStatusModel> GetLivingStatuses()
        {
            var living = new List<UnitStatusModel>();
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsDefeated) living.Add(enemy.Status);
            }

            return living;
        }

        /// <summary>
        /// 指定した敵の「次の敵」で、まだ生きているものを返す。
        /// 手前から奥へ探し、末尾まで行ったら手前に戻る。
        /// 生きている敵が他に居ない場合と、そもそも敵でないユニット（プレイヤー自身など）を渡された場合は null
        /// </summary>
        public UnitStatusModel FindNextLivingTarget(UnitStatusModel target)
        {
            int index = IndexOf(target);
            if (index < 0) return null;

            for (int offset = 1; offset <= _enemies.Count; offset++)
            {
                var candidate = _enemies[(index + offset) % _enemies.Count];
                if (!candidate.IsDefeated) return candidate.Status;
            }

            return null;
        }

        /// <summary>
        /// 生きている敵のうち、一番弱っている（最大HPに対する今のHPの割合が一番低い）ものを返す（敵の味方回復の対象）。
        /// 割合が同じなら手前の敵。生きている敵が居なければ null
        /// </summary>
        public UnitStatusModel FindWeakestLiving()
        {
            UnitStatusModel weakest = null;
            float weakestRatio = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                if (enemy.IsDefeated) continue;

                float ratio = enemy.Status.MaxHp > 0 ? enemy.Status.CurrentHp / (float)enemy.Status.MaxHp : 0f;
                if (ratio < weakestRatio)
                {
                    weakest = enemy.Status;
                    weakestRatio = ratio;
                }
            }

            return weakest;
        }

        /// <summary>自分以外の生きている敵の数（行動を決めるときの戦況）</summary>
        public int CountLivingAllies(BattleEnemy self)
        {
            int count = 0;
            foreach (var enemy in _enemies)
            {
                if (enemy != self && !enemy.IsDefeated) count++;
            }
            return count;
        }

        /// <summary>敵が生成された（倒れていた枠が戦場に戻った）</summary>
        public event System.Action<BattleEnemy> OnSummoned;

        /// <summary>
        /// その敵の、倒れている生成用の枠を1つ戦場に戻す。初めからいる状態効果（耐性など）も付け直す。
        /// 空いている枠が無ければ（すでに出ている・枠を用意していない）何もせず false
        /// </summary>
        public bool Summon(Common.ScriptableObjects.EnemyData data)
        {
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsReserve || enemy.Data != data || !enemy.IsDefeated) continue;

                enemy.Status.Revive();
                foreach (var effect in data.CreateInitialStatusEffects()) enemy.Status.AddStatusEffect(effect);
                enemy.SetPlan(null);
                OnSummoned?.Invoke(enemy);
                return true;
            }
            return false;
        }

        private int IndexOf(UnitStatusModel status)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Status == status) return i;
            }

            return -1;
        }
    }
}
