using System;
using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;

namespace Live2DCardRogue.BattleScene.Models
{
    // 実行待ちカードの並び。実行ボタンを押すと先頭から順に実行される。
    // 枚数に上限があること、並び順そのものが意味を持つ（入れ替えられる）ことがCardPileModelとの違いで、
    // 山札系の使い回しではなく別モデルにしてある。
    public sealed class CardCommandModel
    {
        private readonly List<CardCommand> _commands = new List<CardCommand>();
        private readonly int _maxCount;

        // 表示も判定も常にリスト全体を見るので、追加・削除・並べ替えを区別せず1本の通知にまとめている
        // (CardPileModelが3本に分かれているのは、あちらが「1枚の出入り」を伝える必要があるため)
        public event Action OnChanged;

        /// <summary>
        /// 実行ボタンが押され、積んだ分の実行が求められた。実際の進行（1枚ずつの間合い）は購読する Director が持つ。
        /// 求める側（UseCase）と進める側（Director）が互いを知らずに済むよう、両者が触るこのモデルに置いてある
        /// </summary>
        public event Action OnExecuteRequested;

        public CardCommandModel(int maxCount)
        {
            _maxCount = maxCount;
        }

        public IReadOnlyList<CardCommand> Commands => _commands;
        public int Count => _commands.Count;
        public int MaxCount => _maxCount;
        public bool IsFull => _commands.Count >= _maxCount;

        /// <summary>末尾に積む。上限に達している場合は積まずに false を返す</summary>
        public bool TryAdd(CardCommand command)
        {
            if (IsFull) return false;

            _commands.Add(command);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>指定位置のカードを取り出す。範囲外なら null（空リストへの実行要求もここで吸収する）</summary>
        public CardCommand RemoveAt(int index)
        {
            if (!IsInRange(index)) return null;

            var command = _commands[index];
            _commands.RemoveAt(index);
            OnChanged?.Invoke();
            return command;
        }

        /// <summary>隣同士の入れ替え。端のカードは片側が範囲外になるので、その場合は何もしない</summary>
        public void Swap(int index, int otherIndex)
        {
            if (!IsInRange(index) || !IsInRange(otherIndex) || index == otherIndex) return;

            (_commands[index], _commands[otherIndex]) = (_commands[otherIndex], _commands[index]);
            OnChanged?.Invoke();
        }

        public void Clear()
        {
            if (_commands.Count == 0) return;

            _commands.Clear();
            OnChanged?.Invoke();
        }

        public void RequestExecute()
        {
            OnExecuteRequested?.Invoke();
        }

        private bool IsInRange(int index)
        {
            return index >= 0 && index < _commands.Count;
        }
    }
}
