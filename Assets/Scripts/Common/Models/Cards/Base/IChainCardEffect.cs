namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 順番どおりに続けて使うと成立するコマンドの1段（エクスプロイトチェーン）。
    /// 成立したら、その連なりのコマンドをまとめてパージする（何度も使えると強すぎるので、1回の戦闘で1度きりにする）。
    /// カードの効果は山札を知らないので、どのコマンドが連なりに入っているかを覚えてパージするのは実行する側（BattleCommandExecuteUseCase）
    /// </summary>
    public interface IChainCardEffect
    {
        /// <summary>連なりの1段目か（ここから数え直す）</summary>
        bool StartsChain { get; }

        /// <summary>実行する前に聞く: この使用者が今これを実行すると、連なりが成立するか（最後の段で、直前の段まで進んでいる）</summary>
        bool CompletesChain(UnitStatusModel user);
    }
}
