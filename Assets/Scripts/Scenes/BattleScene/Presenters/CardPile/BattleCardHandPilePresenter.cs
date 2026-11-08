using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Interaction;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 手札（バッファ）のカードへの操作を受け持つ: 選ぶ・浮かせる・使う・パージに選ぶバッファの印。
    // どの入力でどの操作をするか（マウスのホバーか、タッチの2回タップか）は IHandInteraction に任せ、
    // View の出し入れと並べ方は BattleHandLayout に任せる。ここはどの1枚を浮かせるかを決める
    public sealed class BattleCardHandPilePresenter : CardPilePresenterBase, IHandOperations
    {
        // 対象選択中のカードは、選んでいることが分かるように少し浮かせる
        private const float SelectedCardOffsetY = 40f;

        private readonly BattleSceneViewContainer _viewContainer;
        private readonly BattlePhaseModel _phaseModel;
        private readonly CardSelectionModel _selectionModel;
        private readonly BattleCommandQueueUseCase _queueUseCase;

        // どのカードが今選べるか（グレーアウト）を付け直すきっかけにだけ使う。判定そのものは UseCase に聞く
        private readonly UnitStatusModel _playerStatus;
        private readonly CardCommandModel _commandModel;

        // カーソルを合わせているカード（詳細パネルに大きく出す）
        private readonly CardPreviewModel _previewModel;

        private readonly BattleHandLayout _layout;

        // 選択中なのがどの1枚かは View で覚える。CardData はアセットの共有参照なので、
        // 同じカードが手札に複数枚あるとモデル側の PendingCard では押された1枚を特定できない
        private BattleCardView _pendingCardView;

        // 選択が解除された直後の1枚。対象を選んで積まれたときは「解除 → 手札から抜ける」の順に通知が来るので、
        // 抜けるのがどの1枚かをここで覚えておく（同じカードが複数あっても、選んでいた1枚を抜いて見せるため）
        private BattleCardView _releasedCardView;

        // 今は使えない（グレーアウトの）カードを指している1枚。浮かせて見せるだけで、対象選択には入らない。
        // 選択（_pendingCardView）とは別に持つので、ユニットの強調や対象の候補一覧は出ない
        private BattleCardView _inspectedCardView;

        // パージするバッファを選んでいる間の、元のコマンド（浮かせたまま）と、パージに選んだバッファ（選んだ順）
        private BattleCardView _purgeSourceView;
        private readonly List<BattleCardView> _purgeMarkedViews = new List<BattleCardView>();

        // 次の Sync でパージの山へ飛ばす View（積んだ瞬間にバッファから抜けるもの）
        private readonly HashSet<BattleCardView> _purgingViews = new HashSet<BattleCardView>();

        private readonly IHandInteraction _interaction;

        public BattleCardHandPilePresenter(
            CardPileModel cardPileModel,
            BattlePhaseModel phaseModel,
            CardSelectionModel selectionModel,
            BattleSceneViewContainer viewContainer,
            BattleCommandQueueUseCase queueUseCase,
            UnitStatusModel playerStatus,
            CardCommandModel commandModel,
            CardPreviewModel previewModel,
            InteractionMode interactionMode)
            : base(cardPileModel)
        {
            _phaseModel = phaseModel;
            _selectionModel = selectionModel;
            _viewContainer = viewContainer;
            _queueUseCase = queueUseCase;
            _playerStatus = playerStatus;
            _commandModel = commandModel;
            _previewModel = previewModel;
            _interaction = interactionMode == InteractionMode.Touch
                ? (IHandInteraction)new TouchHandInteraction(this)
                : new PointerHandInteraction(this);
            _layout = new BattleHandLayout(viewContainer.CardViewPrefab, (RectTransform)viewContainer.HandParent, Bind);
        }

        // 対象選択中のカードの位置（対象の候補一覧をその上に出すため）。選択中でなければ null
        public RectTransform PendingCardTransform => _pendingCardView != null ? _pendingCardView.RectTransform : null;

        /// <summary>手札の中の、その ID のコマンドのカード（左から最初の1枚）。無ければ null。チュートリアルの目印に使う</summary>
        public RectTransform FindCardTransform(string cardId)
        {
            foreach (var view in _layout.Views)
            {
                if (view != null && view.gameObject.activeInHierarchy && view.CardData != null && view.CardData.Id == cardId) return view.RectTransform;
            }
            return null;
        }

        private RectTransform PurgeSourceTransform => _purgeSourceView != null ? _purgeSourceView.RectTransform : null;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _selectionModel.OnPendingCardChanged += HandlePendingCardChanged;
            _selectionModel.OnPurgeChoiceChanged += HandlePurgeChoiceChanged;
            _selectionModel.OnPurgeConfirmRequested += HandlePurgeConfirmed;
            if (_viewContainer.PurgePicker != null)
            {
                _viewContainer.PurgePicker.OnConfirmClicked += HandlePurgeConfirmed;
                _viewContainer.PurgePicker.Hide();
            }
            _viewContainer.CancelArea.OnClicked += HandleEmptyAreaClicked;
            _viewContainer.TargetPickerView.OnPointerOverChanged += HandlePickerPointerChanged;
            _playerStatus.OnStaminaChanged += HandleStaminaChanged;
            _commandModel.OnChanged += RefreshPlayable;
            _phaseModel.OnPhaseChanged += HandlePhaseChanged;
            RefreshPlayable();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _selectionModel.OnPendingCardChanged -= HandlePendingCardChanged;
            _selectionModel.OnPurgeChoiceChanged -= HandlePurgeChoiceChanged;
            _selectionModel.OnPurgeConfirmRequested -= HandlePurgeConfirmed;
            if (_viewContainer.PurgePicker != null) _viewContainer.PurgePicker.OnConfirmClicked -= HandlePurgeConfirmed;
            _viewContainer.CancelArea.OnClicked -= HandleEmptyAreaClicked;
            _viewContainer.TargetPickerView.OnPointerOverChanged -= HandlePickerPointerChanged;
            _interaction.Dispose();
            _playerStatus.OnStaminaChanged -= HandleStaminaChanged;
            _commandModel.OnChanged -= RefreshPlayable;
            _phaseModel.OnPhaseChanged -= HandlePhaseChanged;
        }

        protected override void Sync(CardPileModel cardPileModel)
        {
            if (_viewContainer.BufferLabel != null) _viewContainer.BufferLabel.text = LocalizationHelper.GetUiText(UiTextKey.BufferLabel);
            if (_viewContainer.PurgePicker != null)
            {
                _viewContainer.PurgePicker.SetTexts(LocalizationHelper.GetUiText(UiTextKey.PurgeChoiceHint), LocalizationHelper.GetUiText(UiTextKey.PurgeConfirm));
            }

            bool isPlayerPhase = _phaseModel.CurrentPhase == BattlePhase.Player;
            // 手札が減るのがプレイヤーフェーズ中なら、それは実行リストへ積まれたということ
            var preferredRemoval = new HashSet<BattleCardView>();
            if (isPlayerPhase && _releasedCardView != null) preferredRemoval.Add(_releasedCardView);
            if (isPlayerPhase) preferredRemoval.UnionWith(_purgingViews);
            _releasedCardView = null;

            var entered = new List<BattleCardView>();
            var removed = new List<BattleCardView>();
            _layout.Diff(cardPileModel.Cards, preferredRemoval, entered, removed);

            foreach (var view in removed)
            {
                if (view == _pendingCardView) _pendingCardView = null;
                if (view == _inspectedCardView) _inspectedCardView = null;
                view.SetPurgeMarked(false);
                // パージに選んで積んだバッファは、パージの山へ飛ぶ
                bool purged = _purgingViews.Remove(view);
                if (purged && _viewContainer.PurgeCountView != null) _layout.PlayExit(view, CardExitStyle.Discard, _viewContainer.PurgeCountView.transform);
                else if (isPlayerPhase && !purged) _layout.PlayExit(view, CardExitStyle.Upload, _viewContainer.CommandListParent);
                else _layout.PlayExit(view, CardExitStyle.Discard, _viewContainer.DiscardCountView.transform);
            }
            _purgingViews.Clear();

            // 引いた札は山札から、実行リストから戻した札はメモリ（実行リスト）から飛んでくる
            var enterFrom = isPlayerPhase ? _viewContainer.CommandListParent : _viewContainer.DeckCountView.transform;
            RepositionCards(entered, enterFrom);
            RefreshPlayable();
        }

        // どの View が押されたかは、この View 専用のコールバックを作ることで特定する
        private void Bind(BattleCardView view, CardData cardData)
        {
            view.Initialize(
                cardData,
                onClicked: () => _interaction.OnCardClicked(view),
                onHovered: () => _interaction.OnCardDwelled(view),
                onHoverChanged: isHovered => _interaction.OnCardPointerChanged(view, isHovered));
        }

        // 使う。対象を選ぶ余地が無ければその対象で、敵が複数いて1体を選ぶカードなら優先ターゲットに向けて、
        // そのまま実行リストへ積む（候補パネルが開いていても同じ。別の敵にしたいときはパネルで選ぶ）。
        // 使えないカードなら理由のメッセージが出る
        private void Play(BattleCardView cardView)
        {
            if (_selectionModel.IsSelectingTarget) _queueUseCase.CancelSelection();

            // 積んだときに手札から抜ける1枚（パージの選択に入るときは浮かせておく1枚）として先に覚えておく
            _releasedCardView = cardView;
            var result = _queueUseCase.TryPlayWithoutChoosing(cardView.CardData, preferFrontTarget: true, _layout.IndexOf(cardView));
            switch (result)
            {
                case QuickPlayResult.Played:
                    _inspectedCardView = null;
                    AudioUseCase.PlaySe(SeId.CardSelect);
                    RepositionCards(null, null);
                    break;
                default:
                    _releasedCardView = null;
                    AudioUseCase.PlaySe(SeId.Error);
                    Inspect(cardView);
                    break;
            }
        }

        // パージするバッファに選ぶ / 外す。元のコマンド自身は選べない。上限まで選んでいたら、それ以上は選べない
        private void TogglePurgeMark(BattleCardView cardView)
        {
            if (cardView == _purgeSourceView) return;

            if (_purgeMarkedViews.Remove(cardView))
            {
                AudioUseCase.PlaySe(SeId.Cancel);
            }
            else if (_purgeMarkedViews.Count < _selectionModel.PurgeCapacity)
            {
                _purgeMarkedViews.Add(cardView);
                AudioUseCase.PlaySe(SeId.CardSelect);
            }
            else
            {
                AudioUseCase.PlaySe(SeId.Error);
                return;
            }

            cardView.SetPurgeMarked(_purgeMarkedViews.Contains(cardView));
            SyncPurgePicker();
            RepositionCards(null, null);
        }

        // 選んだ数はモデルにも伝える（実行ボタンが決定ボタンとして押せるかどうかに使う）
        private void SyncPurgePicker()
        {
            _selectionModel.SetPurgeChosenCount(_purgeMarkedViews.Count);
            if (_viewContainer.PurgePicker == null) return;
            _viewContainer.PurgePicker.SetCount(_purgeMarkedViews.Count, _selectionModel.PurgeCapacity);
        }

        // パージの選択が始まった（対象を選び終えた）/ 終わった（積んだ・取り消した）
        private void HandlePurgeChoiceChanged()
        {
            if (_selectionModel.IsChoosingPurge)
            {
                // 対象選択が終わった通知で「抜けるはずの1枚」になった View を、そのまま浮かせておく
                _purgeSourceView = _releasedCardView;
                _releasedCardView = null;
                _purgeMarkedViews.Clear();
                SyncPurgePicker();
                _viewContainer.PurgePicker?.Show(() => PurgeSourceTransform);
            }
            else
            {
                foreach (var view in _purgeMarkedViews) view.SetPurgeMarked(false);
                _purgeMarkedViews.Clear();
                _purgeSourceView = null;
                _viewContainer.PurgePicker?.Hide();
            }
            RefreshPlayable();
            RepositionCards(null, null);
        }

        // 選び終えた（パネルの決定ボタン、または決定ボタンになっている実行ボタン）。
        // 積めたら、元のコマンドは実行リストへ、選んだバッファはパージの山へ飛ぶ（どちらも次の Sync で抜ける）
        private void HandlePurgeConfirmed()
        {
            if (!_selectionModel.IsChoosingPurge) return;

            var source = _purgeSourceView;
            var marked = new List<BattleCardView>(_purgeMarkedViews);
            var cards = marked.ConvertAll(view => view.CardData);

            _releasedCardView = source;
            _purgingViews.Clear();
            _purgingViews.UnionWith(marked);
            if (!_queueUseCase.ConfirmPurgeChoice(cards, marked.ConvertAll(view => _layout.IndexOf(view))))
            {
                _releasedCardView = null;
                _purgingViews.Clear();
                AudioUseCase.PlaySe(SeId.Error);
            }
        }

        private void HandlePickerPointerChanged(bool isOver)
        {
            _interaction.OnPickerPointerChanged(isOver);
        }

        private bool CanOperate(BattleCardView cardView)
        {
            return _phaseModel.CurrentPhase == BattlePhase.Player && _layout.Contains(cardView);
        }

        // 同じCardDataの別の1枚を選んだ場合もそちらへ選択が移る（CardDataでは区別できないので、この判断はここでしかできない）。
        // 断られた（クリックで選べないカードを押した）場合は、理由のメッセージが出たうえで、そのカードを浮かせて見せる
        private void Select(BattleCardView cardView)
        {
            // 対象選択に入った通知はこの呼び出しの中で飛ぶので、先に覚えておく
            var previous = _pendingCardView;
            _pendingCardView = cardView;
            _inspectedCardView = null;
            if (!_queueUseCase.TrySelectCard(cardView.CardData, _layout.IndexOf(cardView)))
            {
                // 呼ぶ側が NeedsTargetChoice で選べることを確かめてから呼ぶので、ふつうは断られない
                AudioUseCase.PlaySe(SeId.Error);
                _pendingCardView = previous;
                Inspect(cardView);
                return;
            }

            AudioUseCase.PlaySe(SeId.CardSelect);
            RepositionCards(null, null);
        }

        // 使えないカードを指した。前に選んでいたカードの選択は外す
        // （浮いているのはこのカードなのに、前のカードの対象がユニット側で光ったままにならないように）
        private void Inspect(BattleCardView cardView)
        {
            AudioUseCase.PlaySe(SeId.Hover);
            _inspectedCardView = cardView;
            if (_selectionModel.IsSelectingTarget) _queueUseCase.CancelSelection();
            RepositionCards(null, null);
        }

        // 何も無い所がクリックされたら、選択も「指しているだけ」も解除する
        private void HandleEmptyAreaClicked()
        {
            _inspectedCardView = null;
            if (_selectionModel.IsSelectingTarget || _selectionModel.IsChoosingPurge)
            {
                AudioUseCase.PlaySe(SeId.Cancel);
                _queueUseCase.CancelSelection();
            }
            else RepositionCards(null, null);
        }

        private void HandleStaminaChanged(int stamina)
        {
            RefreshPlayable();
        }

        // プレイヤーの番が終わったら、指していたカードも下ろす
        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase != BattlePhase.Player && _inspectedCardView != null)
            {
                _inspectedCardView = null;
                RepositionCards(null, null);
            }
            RefreshPlayable();
        }

        // 今選べないカードをグレーアウトする。選べるかどうかはクリック時と同じ判定（UseCase）に聞く。
        // プレイヤーの番以外は、そもそも何も選べないので暗くしない（手札全体が暗くなって見づらいだけなので）
        private void RefreshPlayable()
        {
            bool isPlayerPhase = _phaseModel.CurrentPhase == BattlePhase.Player;
            foreach (var view in _layout.Views)
            {
                // パージを選んでいる間は、どれも暗くしない（どのバッファでもパージに選べる）
                bool isPlayable = !isPlayerPhase || _selectionModel.IsChoosingPurge || _queueUseCase.CanSelectCard(view.CardData);
                bool isCostShort = isPlayerPhase && !_selectionModel.IsChoosingPurge && !_queueUseCase.CanAfford(view.CardData);
                view.SetPlayable(isPlayable, isCostShort);
            }
        }

        // 浮かせる位置が変わるだけなので、並べ直すだけでよい
        private void HandlePendingCardChanged(CardData cardData)
        {
            if (cardData == null)
            {
                // クリックでそのまま積んだとき（対象選択を経ない）は、抜ける1枚を既に覚えているので上書きしない
                if (_pendingCardView != null) _releasedCardView = _pendingCardView;
                _pendingCardView = null;
            }

            RepositionCards(null, null);
        }

        // 浮かせるのは、選んでいる1枚（パージを選んでいる間はその元のコマンド）と、指しているだけの1枚。
        // パージに選んだバッファは半分だけ浮かせる。浮かせている1枚は最前面に出す
        private void RepositionCards(List<BattleCardView> entered, Transform enterFrom)
        {
            var lifted = _pendingCardView ?? _purgeSourceView ?? _inspectedCardView;
            _layout.Reposition(entered, enterFrom, lifted, LiftOf, IsSelected);
            _interaction.OnLiftedChanged(lifted);
        }

        private bool IsSelected(BattleCardView view) => view == _pendingCardView || view == _purgeSourceView;

        private float LiftOf(BattleCardView view)
        {
            if (IsSelected(view) || view == _inspectedCardView) return SelectedCardOffsetY;
            return _purgeMarkedViews.Contains(view) ? SelectedCardOffsetY * 0.5f : 0f;
        }

        // 入力の解釈（IHandInteraction）から呼ぶ操作
        bool IHandOperations.CanOperate(BattleCardView view) => CanOperate(view);
        bool IHandOperations.IsChoosingPurge => _selectionModel.IsChoosingPurge;
        BattleCardView IHandOperations.PendingView => _pendingCardView;
        BattleCardView IHandOperations.InspectedView => _inspectedCardView;

        bool IHandOperations.NeedsTargetChoice(BattleCardView view)
        {
            return _queueUseCase.CanSelectCard(view.CardData) && _queueUseCase.FindAutoTarget(view.CardData) == null;
        }

        void IHandOperations.Play(BattleCardView view) => Play(view);
        void IHandOperations.Select(BattleCardView view) => Select(view);
        void IHandOperations.Inspect(BattleCardView view) => Inspect(view);

        void IHandOperations.Lower(BattleCardView view)
        {
            if (view != _inspectedCardView) return;
            _inspectedCardView = null;
            RepositionCards(null, null);
        }

        void IHandOperations.TogglePurgeMark(BattleCardView view) => TogglePurgeMark(view);
        void IHandOperations.ShowPreview(BattleCardView view) => _previewModel.Show(view, view.CardData);
        void IHandOperations.HidePreview(BattleCardView view) => _previewModel.Hide(view);

        void IHandOperations.CancelTargetSelection()
        {
            if (_selectionModel.IsSelectingTarget) _queueUseCase.CancelSelection();
        }
    }
}
