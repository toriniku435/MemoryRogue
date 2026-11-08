using System.Collections.Generic;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// プレイヤーがコマンドを実行するときの、2体のユニットだけでは分からない事情（ICommandContextCardEffect に渡す）。
    /// 実行する側（BattleCommandExecuteUseCase）が組み立てる
    /// </summary>
    public sealed class CardExecutionContext
    {
        /// <summary>実行する時点でバッファ（手札）に残っているコマンドの数</summary>
        public int BufferCount { get; }

        /// <summary>生きているユニットすべて（使用者も含む。使用者が先頭）</summary>
        public IReadOnlyList<UnitStatusModel> AllUnits { get; }

        /// <summary>この戦闘で倒した敵の数（生成で戻ってきた敵は、倒すたびに数える）</summary>
        public int DefeatedEnemyCount { get; }

        public CardExecutionContext(int bufferCount, IReadOnlyList<UnitStatusModel> allUnits, int defeatedEnemyCount = 0)
        {
            BufferCount = bufferCount < 0 ? 0 : bufferCount;
            AllUnits = allUnits ?? new UnitStatusModel[0];
            DefeatedEnemyCount = defeatedEnemyCount < 0 ? 0 : defeatedEnemyCount;
        }
    }

    /// <summary>
    /// バッファの数や、敵味方すべてのユニットを見て効く効果。プレイヤーが実行するときは Execute の代わりに
    /// ExecuteWithContext が呼ばれる。敵が使うなど事情の分からない場面では、普通に Execute が呼ばれる
    /// </summary>
    public interface ICommandContextCardEffect
    {
        void ExecuteWithContext(UnitStatusModel user, UnitStatusModel target, CardExecutionContext context);
    }

    /// <summary>
    /// 使うのにトークンを払う効果（クラウドバースト）。積むときに払い、負けても戻らない。
    /// 払えるかの判定と支払いは実行する側（BattleCommandQueueUseCase / BattleCommandExecuteUseCase）が行い、ここは額を持つだけ
    /// </summary>
    public interface ITokenCostCardEffect
    {
        int TokenCost { get; }
    }
}
