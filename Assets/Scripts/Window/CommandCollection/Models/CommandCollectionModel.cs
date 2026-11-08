using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Window.CommandCollection.Models
{
    /// <summary>コレクションのリストの1行: コマンドと、一度でも手に入れたことがあるか</summary>
    public readonly struct CommandCollectionEntry
    {
        public CardData Card { get; }
        public bool IsDiscovered { get; }

        public CommandCollectionEntry(CardData card, bool isDiscovered)
        {
            Card = card;
            IsDiscovered = isDiscovered;
        }
    }

    // コレクション（コマンド）の中身。絞り込み（0 が「すべて」、1 からがチップごと）と、今リストに並べる行を持つ。
    // 手に入れたことがあるかは ID の集合で受け取るので、セーブのことは知らない
    public sealed class CommandCollectionModel
    {
        private readonly IReadOnlyList<CardData> _all;
        private readonly IReadOnlyList<IReadOnlyList<CardData>> _filters;
        private readonly ISet<string> _discoveredIds;
        private readonly List<CommandCollectionEntry> _entries = new List<CommandCollectionEntry>();

        /// <summary>絞り込みを変えた（リストを並べ直す）</summary>
        public event Action OnChanged;

        /// <param name="all">「すべて」に並べるコマンド（並べる順）</param>
        /// <param name="packFilters">チップごとの絞り込み（タブの順。中身は並べる順）</param>
        /// <param name="discoveredIds">一度でも手に入れたことがあるコマンドの ID</param>
        public CommandCollectionModel(IReadOnlyList<CardData> all, IReadOnlyList<IReadOnlyList<CardData>> packFilters, ISet<string> discoveredIds)
        {
            _all = all ?? new CardData[0];
            var filters = new List<IReadOnlyList<CardData>> { _all };
            if (packFilters != null) filters.AddRange(packFilters);
            _filters = filters;
            _discoveredIds = discoveredIds ?? new HashSet<string>();
            Build();
        }

        /// <summary>絞り込みの数（「すべて」を含む）</summary>
        public int FilterCount => _filters.Count;

        /// <summary>選んでいる絞り込み（0 が「すべて」）</summary>
        public int SelectedFilter { get; private set; }

        /// <summary>今リストに並べる行</summary>
        public IReadOnlyList<CommandCollectionEntry> Entries => _entries;

        /// <summary>「すべて」のうち手に入れたことがある数</summary>
        public int DiscoveredCount
        {
            get
            {
                int count = 0;
                foreach (var card in _all)
                {
                    if (IsDiscovered(card)) count++;
                }
                return count;
            }
        }

        /// <summary>「すべて」の数</summary>
        public int TotalCount => _all.Count;

        public bool IsDiscovered(CardData card)
        {
            return card != null && _discoveredIds.Contains(card.Id);
        }

        /// <summary>絞り込みを選ぶ。範囲外や今と同じなら何もしない</summary>
        public void SelectFilter(int index)
        {
            if (index < 0 || index >= _filters.Count || index == SelectedFilter) return;
            SelectedFilter = index;
            Build();
            OnChanged?.Invoke();
        }

        private void Build()
        {
            _entries.Clear();
            foreach (var card in _filters[SelectedFilter])
            {
                if (card != null) _entries.Add(new CommandCollectionEntry(card, IsDiscovered(card)));
            }
        }
    }
}
