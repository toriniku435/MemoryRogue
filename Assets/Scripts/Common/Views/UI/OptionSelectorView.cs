using System;
using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // 「◀ 値 ▶」で選ぶ項目（言語・画面モードなど）。矢印が押されたことを伝え、渡された値の名前を見せるだけ。
    // どの値が並ぶか・回すと何になるかは Presenter（とそのモデル）が決める
    public sealed class OptionSelectorView : MonoBehaviour
    {
        [SerializeField] private CommonText _labelText;
        [SerializeField] private CommonText _valueText;
        [SerializeField] private CommonButton _previousButton;
        [SerializeField] private CommonButton _nextButton;

        /// <summary>矢印が押された（前なら -1、次なら +1）</summary>
        public event Action<int> OnStep;

        private void Awake()
        {
            _previousButton.onClick.AddListener(HandlePrevious);
            _nextButton.onClick.AddListener(HandleNext);
        }

        private void OnDestroy()
        {
            _previousButton.onClick.RemoveListener(HandlePrevious);
            _nextButton.onClick.RemoveListener(HandleNext);
        }

        public void SetLabel(string label)
        {
            _labelText.text = label;
        }

        public void SetValue(string value)
        {
            _valueText.text = value;
        }

        /// <summary>選べる値が1つしかないときは矢印を押せなくする</summary>
        public void SetInteractable(bool interactable)
        {
            _previousButton.interactable = interactable;
            _nextButton.interactable = interactable;
        }

        private void HandlePrevious() => OnStep?.Invoke(-1);
        private void HandleNext() => OnStep?.Invoke(1);
    }
}
