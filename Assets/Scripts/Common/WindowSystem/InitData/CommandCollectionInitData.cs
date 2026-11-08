using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // コマンドのコレクション
    public sealed class CommandCollectionInitData : WindowInitData
    {
        public override WindowId Id => WindowId.CommandCollection;

        /// <summary>並べるコマンドとチップ</summary>
        public CommandCollectionData Data { get; }

        /// <summary>手に入れたことがあるコマンドの ID（開くたびに聞き直す）</summary>
        public Func<ISet<string>> GetDiscoveredIds { get; }

        public CommandCollectionInitData(CommandCollectionData data, Func<ISet<string>> getDiscoveredIds)
        {
            Data = data;
            GetDiscoveredIds = getDiscoveredIds;
        }
    }
}
