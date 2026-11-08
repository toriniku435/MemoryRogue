using System;

namespace Live2DCardRogue.BattleScene.Models
{
    public sealed class BattlePhaseModel
    {
        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.Draw;

        public event Action<BattlePhase> OnPhaseChanged;

        public void SetPhase(BattlePhase phase)
        {
            if (CurrentPhase == phase) return;

            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
