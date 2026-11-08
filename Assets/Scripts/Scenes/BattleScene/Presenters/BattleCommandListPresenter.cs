using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Interaction;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using UnityEngine;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 実行リスト（積んだカードの並び）と実行ボタンを受け持つPresenter。
    // 実行ボタンはリストの一部として使うものなので、別Presenterに割らずここでまとめて面倒を見る
    public sealed class BattleCommandListPresenter : PresenterBase
    {
        private const float CardSpacing = 16f;

        private readonly CardCommandModel _commandModel;
        private readonly BattlePhaseModel _phaseModel;
        private readonly BattleSceneViewContainer _viewContainer;
        private readonly BattleCommandQueueUseCase _queueUseCase;
        private readonly TargetFocusModel _focusModel;
        private readonly CardPreviewModel _previewModel;
        private readonly CardSelectionModel _selectionModel;

        // タッチでは、1回目のタップで説明と矢印を出し（CardPreviewModel の持ち主にする）、2回目で手札に戻す
        private readonly bool _isTouch;
        // タップで選んで、対象のユニットを強調しているコマンド
        private CardCommand _focusedByTap;

        // コマンドごとに View を1枚ずつ結び付ける。CardCommand は積むたびに作られる別インスタンスなので、
        // 同じカードが2枚積まれていても区別でき、並べ替えや実行で「どのカードがどこへ行ったか」をそのまま見せられる
        private readonly Dictionary<CardCommand, BattleCommandCardView> _cardViews = new Dictionary<CardCommand, BattleCommandCardView>();
        private readonly Dictionary<CardCommand, int> _slotIndices = new Dictionary<CardCommand, int>();
        private readonly Stack<BattleCommandCardView> _pool = new Stack<BattleCommandCardView>();

        // プレイヤーがクリックして手札に戻したコマンド。消え方（手札へ落ちる）を選ぶために覚えておく
        private CardCommand _returningCommand;

        public BattleCommandListPresenter(
            CardCommandModel commandModel,
            BattlePhaseModel phaseModel,
            BattleSceneViewContainer viewContainer,
            BattleCommandQueueUseCase queueUseCase,
            TargetFocusModel focusModel,
            CardPreviewModel previewModel,
            CardSelectionModel selectionModel,
            InteractionMode interactionMode)
        {
            _isTouch = interactionMode == InteractionMode.Touch;
            _commandModel = commandModel;
            _phaseModel = phaseModel;
            _viewContainer = viewContainer;
            _queueUseCase = queueUseCase;
            _focusModel = focusModel;
            _previewModel = previewModel;
            _selectionModel = selectionModel;
        }

        protected override void OnInitialize()
        {
            _commandModel.OnChanged += HandleCommandsChanged;
            _phaseModel.OnPhaseChanged += HandlePhaseChanged;
            _viewContainer.ExecuteButton.onClick.AddListener(HandleExecuteClicked);
            _selectionModel.OnPurgeChoiceChanged += RefreshExecuteButton;
            _selectionModel.OnPurgeChosenCountChanged += RefreshExecuteButton;
            if (_isTouch) _previewModel.OnOwnerChanged += RefreshTapSelection;

            Sync();
        }

        protected override void OnDispose()
        {
            _commandModel.OnChanged -= HandleCommandsChanged;
            _phaseModel.OnPhaseChanged -= HandlePhaseChanged;
            _viewContainer.ExecuteButton.onClick.RemoveListener(HandleExecuteClicked);
            _selectionModel.OnPurgeChoiceChanged -= RefreshExecuteButton;
            _selectionModel.OnPurgeChosenCountChanged -= RefreshExecuteButton;
            if (_isTouch) _previewModel.OnOwnerChanged -= RefreshTapSelection;
        }

        // カード名・説明も実行ボタンのラベルもローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            Sync();
        }

        // 実行リストと View を突き合わせる（一度生成した View は破棄せず使い回す）
        private void Sync()
        {
            RefreshExecuteButton();

            var commands = _commandModel.Commands;

            // リストから抜けたコマンド：実行されたのか、手札へ戻されたのか、ターン終了で捨てられたのかで消え方を変える
            var removed = new List<CardCommand>();
            foreach (var command in _cardViews.Keys)
            {
                if (!Contains(commands, command)) removed.Add(command);
            }
            foreach (var command in removed)
            {
                PlayExit(command);
            }
            _returningCommand = null;

            for (int i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                var target = SlotPosition(i);
                bool isNew = !_cardViews.TryGetValue(command, out var view);
                if (isNew)
                {
                    view = Rent();
                    _cardViews.Add(command, view);
                }

                // 並べ替えで位置が変わるので、そのときの位置を閉じ込めたコールバックを毎回渡し直す
                int index = i;
                view.Initialize(
                    command.Card,
                    onClicked: () => HandleCardClicked(command, index),
                    onMoveLeft: () => _queueUseCase.SwapCommands(index, index - 1),
                    onMoveRight: () => _queueUseCase.SwapCommands(index, index + 1),
                    // カーソルを合わせている間、このカードがどのユニットに撃たれるかをユニット側で強調する
                    onHoverChanged: isHovered => HandleHoverChanged(command, isHovered));
                view.SetMovable(canMoveLeft: index > 0, canMoveRight: index < commands.Count - 1);
                view.SetHoverEnabled(!_isTouch);

                if (isNew)
                {
                    AudioUseCase.PlaySe(SeId.MemoryWrite);
                    // 差し込みの演出は View が有効になったとき（OnEnable）に自分で出す
                    view.Place(target);
                    view.SetActive(true);
                }
                else if (_slotIndices[command] != i)
                {
                    // 別の番地へ移った（並べ替え・実行後の前詰め）
                    view.MoveToSlot(target);
                }
                _slotIndices[command] = i;
            }

            // 抜けたコマンドの強調を外し、残ったものの矢印を付け直す
            if (_isTouch) RefreshTapSelection();
        }

        private static bool Contains(IReadOnlyList<CardCommand> commands, CardCommand command)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i] == command) return true;
            }
            return false;
        }

        private BattleCommandCardView Rent()
        {
            if (_pool.Count > 0) return _pool.Pop();
            var view = Object.Instantiate(_viewContainer.CommandCardViewPrefab, _viewContainer.CommandListParent);
            view.SetActive(false);
            return view;
        }

        private void PlayExit(CardCommand command)
        {
            var view = _cardViews[command];
            _cardViews.Remove(command);
            _slotIndices.Remove(command);

            var list = (RectTransform)_viewContainer.CommandListParent;
            var from = view.RectTransform.anchoredPosition;
            CardExitStyle style;
            Vector2 destination;
            if (command == _returningCommand)
            {
                AudioUseCase.PlaySe(SeId.CardReturn);
                style = CardExitStyle.Return;
                destination = CardLayoutUtility.ToChildAnchoredPosition(list, _viewContainer.HandParent);
            }
            else if (_phaseModel.CurrentPhase == BattlePhase.PlayerExecution)
            {
                // 実行された命令は、その場で光って上へ抜けていく
                AudioUseCase.PlaySe(SeId.CommandExecute);
                style = CardExitStyle.Execute;
                destination = from + Vector2.up * 120f;
            }
            else
            {
                style = CardExitStyle.Discard;
                destination = CardLayoutUtility.ToChildAnchoredPosition(list, _viewContainer.DiscardCountView.transform);
            }

            view.PlayExit(destination, style, () =>
            {
                view.SetActive(false);
                _pool.Push(view);
            });
        }

        private void HandleHoverChanged(CardCommand command, bool isHovered)
        {
            // 対象のユニットを強調し、カードの詳細を大きく出す
            if (isHovered)
            {
                _focusModel.Focus(command.Target);
                _previewModel.Show(command, command.Card);
            }
            else
            {
                _focusModel.Unfocus(command.Target);
                _previewModel.Hide(command);
            }
        }

        private void HandleCardClicked(CardCommand command, int index)
        {
            if (_isTouch && _previewModel.Owner != command)
            {
                _previewModel.Show(command, command.Card);
                return;
            }
            HandleReturnClicked(command, index);
        }

        // タップで選んだコマンドだけ矢印を出し、その対象のユニットを強調する
        private void RefreshTapSelection()
        {
            foreach (var pair in _cardViews) pair.Value.SetPinned(_previewModel.Owner == pair.Key);

            var selected = _previewModel.Owner as CardCommand;
            if (selected != null && !_cardViews.ContainsKey(selected)) selected = null;
            if (selected == _focusedByTap) return;

            if (_focusedByTap != null) _focusModel.Unfocus(_focusedByTap.Target);
            _focusedByTap = selected;
            if (selected != null) _focusModel.Focus(selected.Target);
        }

        private void HandleReturnClicked(CardCommand command, int index)
        {
            _returningCommand = command;
            _queueUseCase.ReturnCommandToHand(index);
        }

        // 実行リストの枠の位置。手札と同じくスクリプトで並べる（Layout Groupを使わない理由は手札側と同じ）。
        // 枚数ではなく上限枚数ぶんの枠で並べ、左の枠から詰める。シーンに置いた番号付きの枠と、実行される順番が一致するように
        private Vector2 SlotPosition(int index)
        {
            // プレハブ自体を縮小して置いてあるので、見た目の幅はスケールを掛けたもの
            var prefabRect = (RectTransform)_viewContainer.CommandCardViewPrefab.transform;
            float cardWidth = prefabRect.rect.width * prefabRect.localScale.x;
            int slotCount = Mathf.Max(_commandModel.MaxCount, _commandModel.Count);
            float totalWidth = slotCount * cardWidth + (slotCount - 1) * CardSpacing;
            float startX = -totalWidth / 2f + cardWidth / 2f;
            return new Vector2(startX + index * (cardWidth + CardSpacing), 0f);
        }

        private void HandleCommandsChanged()
        {
            Sync();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            RefreshExecuteButton();

            // タッチでタップして開いた説明は、別のものをタップするまで残るので、実行が始まったら閉じる
            // （実行の結果やログが説明の下に隠れないように）
            if (_isTouch && phase == BattlePhase.PlayerExecution) _previewModel.Clear();
        }

        // 実行中や敵ターン中に押せてしまわないように、入力待ちの間だけ押せるようにする。
        // パージするバッファを選んでいる間は、パネルの決定ボタンと同じ「決定」になる（1個も選んでいなければ押せない）
        private void RefreshExecuteButton()
        {
            bool choosingPurge = _selectionModel.IsChoosingPurge;
            _viewContainer.ExecuteButtonLabel.text = LocalizationHelper.GetUiText(choosingPurge ? UiTextKey.PurgeConfirm : UiTextKey.Execute);
            _viewContainer.ExecuteButton.interactable = _phaseModel.CurrentPhase == BattlePhase.Player
                && (!choosingPurge || _selectionModel.CanConfirmPurge);
        }

        private void HandleExecuteClicked()
        {
            if (_selectionModel.IsChoosingPurge)
            {
                _selectionModel.RequestPurgeConfirm();
                return;
            }
            _queueUseCase.RequestExecuteCommands();
        }
    }
}
