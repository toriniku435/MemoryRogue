using System.Collections.Generic;

namespace Live2DCardRogue.Common.Models
{
    // 開いているポップアップの重なり順（奥から手前）。
    // 開いたものを一番手前へ、閉じたものを抜くだけ。何を手前に描くかの判断は呼び出し側（PopupStack）が行う。
    // 中身の型を問わないので、UnityEngine に依存せずにテストできる
    public sealed class PopupStackModel<T> where T : class
    {
        private readonly List<T> _items = new List<T>();

        /// <summary>奥から手前の順。最後が一番手前</summary>
        public IReadOnlyList<T> Items => _items;

        /// <summary>一番手前（無ければ null）</summary>
        public T Front => _items.Count > 0 ? _items[_items.Count - 1] : null;

        /// <summary>一番手前へ置く。既にあれば手前へ移す</summary>
        public void Bring(T item)
        {
            if (item == null) return;
            _items.Remove(item);
            _items.Add(item);
        }

        /// <summary>抜く。抜いたら true（無かったら false）</summary>
        public bool Remove(T item)
        {
            return item != null && _items.Remove(item);
        }

        /// <summary>条件に合うものをまとめて抜く（破棄されたものの掃除など）。抜いた数を返す</summary>
        public int RemoveAll(System.Predicate<T> match)
        {
            return _items.RemoveAll(match);
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
