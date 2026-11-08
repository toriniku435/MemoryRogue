using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Models
{
    // この戦闘で起きたことの記録。増える一方で、消えない（ログウインドウでいつでも振り返れるように）
    public sealed class BattleLogModel
    {
        private readonly List<BattleLogEntry> _entries = new List<BattleLogEntry>();

        /// <summary>1件増えた</summary>
        public event Action<BattleLogEntry> OnAdded;

        public IReadOnlyList<BattleLogEntry> Entries => _entries;

        public void Add(BattleLogEntry entry)
        {
            if (entry == null) return;
            _entries.Add(entry);
            OnAdded?.Invoke(entry);
        }
    }
}
