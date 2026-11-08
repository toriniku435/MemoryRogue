using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // エネミーのコレクション（敵詳細と同じ View の、別のプレハブ）
    public sealed class EnemyCollectionInitData : WindowInitData
    {
        public override WindowId Id => WindowId.EnemyCollection;

        /// <summary>並べる敵</summary>
        public EnemyCollectionData Data { get; }

        /// <summary>一度でも戦ったことがある敵の ID（開くたびに聞き直す）</summary>
        public Func<ISet<string>> GetDiscoveredIds { get; }

        public EnemyCollectionInitData(EnemyCollectionData data, Func<ISet<string>> getDiscoveredIds)
        {
            Data = data;
            GetDiscoveredIds = getDiscoveredIds;
        }
    }
}
