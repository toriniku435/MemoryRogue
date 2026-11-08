using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.Views
{
    // 敵の次の行動の予告（スロットの列）。スロットは並べて置いておき、使う数だけ出す
    public sealed class BattleEnemyIntentView : MonoBehaviour
    {
        [SerializeField] private BattleEnemyIntentSlotView[] _slots;
        // 見出し（状態効果の一覧と見分けるため）
        [SerializeField] private Live2DCardRogue.Common.Views.CommonText _header;

        public int SlotCount => _slots.Length;

        /// <summary>スロットにカーソルが乗った / 外れた（引数はスロットの番号）</summary>
        public event Action<int> OnSlotHovered;
        public event Action<int> OnSlotUnhovered;
        public event Action<int> OnSlotClicked;

        private void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                int index = i;
                _slots[i].OnHovered += () => OnSlotHovered?.Invoke(index);
                _slots[i].OnUnhovered += () => OnSlotUnhovered?.Invoke(index);
                _slots[i].OnClicked += () => OnSlotClicked?.Invoke(index);
            }
        }

        public void SetHeader(string text)
        {
            if (_header != null) _header.text = text;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>予告を並べる。スロットより多い分は出さない</summary>
        public void SetActions(IReadOnlyList<(string Name, Sprite Illustration, bool WillAct)> actions)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                bool used = i < actions.Count;
                _slots[i].gameObject.SetActive(used);
                if (used) _slots[i].Set(actions[i].Name, actions[i].Illustration, actions[i].WillAct);
            }
        }
    }
}
