using System;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Models
{
    // ユニット強化。トークンを払って、最大HPとメモリー上限を1段ずつ上げる。
    // 「上げられるか」「いくらか」の判定をここ1か所に集めてあるので、ボタンの活性と実際の強化がズレない。
    // 1段の上がり幅・上限・値段は UnitUpgradeRule（元は CampaignBalance アセット）が持つ。
    //
    // 上げた分はまず仮のもので、Apply で確定する（反映ボタン）。Discard で確定した値まで戻す（破棄）。
    // Tokens / Hp / Memory は仮の値（上げた分を払った後）、Applied* が確定した値
    public sealed class UnitUpgradeModel
    {
        private readonly UnitUpgradeRule _rule;
        private readonly int _baseHp;
        private readonly int _baseMemory;

        public event Action OnChanged;

        /// <param name="baseHp">強化していないときの最大HP（段の数え始め）</param>
        /// <param name="baseMemory">強化していないときのメモリー上限</param>
        public UnitUpgradeModel(int tokens, int hp, int memory, int baseHp, int baseMemory, UnitUpgradeRule rule)
        {
            Tokens = AppliedTokens = tokens;
            Hp = AppliedHp = hp;
            Memory = AppliedMemory = memory;
            _baseHp = baseHp;
            _baseMemory = baseMemory;
            _rule = rule;
        }

        /// <summary>仮に上げた分を払った後のトークン</summary>
        public int Tokens { get; private set; }

        /// <summary>仮に上げた後の最大HP</summary>
        public int Hp { get; private set; }

        /// <summary>仮に上げた後のメモリー上限</summary>
        public int Memory { get; private set; }

        /// <summary>確定しているトークン・最大HP・メモリー上限</summary>
        public int AppliedTokens { get; private set; }
        public int AppliedHp { get; private set; }
        public int AppliedMemory { get; private set; }

        /// <summary>まだ確定していない強化があるか</summary>
        public bool IsDirty => Hp != AppliedHp || Memory != AppliedMemory;

        /// <summary>次の1段を上げたときの値</summary>
        public int NextHp => Hp + _rule.HpStep;
        public int NextMemory => Memory + _rule.MemoryStep;

        public int HpLevel => Math.Max(0, (Hp - _baseHp) / _rule.HpStep);
        public int HpMaxLevel => Math.Max(0, (_rule.HpLimit - _baseHp) / _rule.HpStep);
        public int MemoryLevel => Math.Max(0, (Memory - _baseMemory) / _rule.MemoryStep);
        public int MemoryMaxLevel => Math.Max(0, (_rule.MemoryLimit - _baseMemory) / _rule.MemoryStep);

        public bool IsHpMaxed => NextHp > _rule.HpLimit;
        public bool IsMemoryMaxed => NextMemory > _rule.MemoryLimit;

        /// <summary>次の1段の値段</summary>
        public int HpCost => _rule.HpBaseCost + _rule.HpCostPerLevel * HpLevel;
        public int MemoryCost => _rule.MemoryBaseCost + _rule.MemoryCostPerLevel * MemoryLevel;

        public bool CanUpgradeHp => !IsHpMaxed && Tokens >= HpCost;
        public bool CanUpgradeMemory => !IsMemoryMaxed && Tokens >= MemoryCost;

        /// <summary>
        /// 強化の外でトークンが増減したとき（マーケットで買った）に合わせる。仮に払っている分はそのまま差し引く。
        /// 次にウインドウを開いたときに読み直されるので、通知はしない
        /// </summary>
        public void SetTokens(int tokens)
        {
            int pending = AppliedTokens - Tokens;
            AppliedTokens = tokens;
            Tokens = tokens - pending;
        }

        /// <summary>仮に上げた分を確定する。確定するものが無ければ false</summary>
        public bool Apply()
        {
            if (!IsDirty) return false;
            AppliedTokens = Tokens;
            AppliedHp = Hp;
            AppliedMemory = Memory;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>仮に上げた分を捨て、確定した値に戻す</summary>
        public void Discard()
        {
            if (!IsDirty) return;
            Tokens = AppliedTokens;
            Hp = AppliedHp;
            Memory = AppliedMemory;
            OnChanged?.Invoke();
        }

        public bool TryUpgradeHp()
        {
            if (!CanUpgradeHp) return false;
            Tokens -= HpCost;
            Hp = NextHp;
            OnChanged?.Invoke();
            return true;
        }

        public bool TryUpgradeMemory()
        {
            if (!CanUpgradeMemory) return false;
            Tokens -= MemoryCost;
            Memory = NextMemory;
            OnChanged?.Invoke();
            return true;
        }
    }
}
