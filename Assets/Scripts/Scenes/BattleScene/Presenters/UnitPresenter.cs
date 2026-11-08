using System;
using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.Presenters
{
    public sealed class UnitPresenter : PresenterBase
    {
        private readonly UnitStatusModel _unitStatusModel;
        private readonly BattleUnitStatusView _unitStatusView;
        private readonly CardSelectionModel _selectionModel;
        private readonly TargetFocusModel _focusModel;
        private readonly bool _showStamina;

        // 立ち絵。null ならシーンに置いてある絵のまま
        private readonly Sprite _portrait;

        // 敵なら、名前を引くためのデータ。プレイヤーは null（名前を出さない）
        private readonly EnemyData _enemyData;

        // 増減量を出すために、前回表示したHPを覚えておく
        private int _lastHp;

        // 今このユニットを対象に選べるか。カードごとに選べる側が違うので、判断はこのPresenterには持たせない
        private readonly Func<UnitStatusModel, bool> _canBeTargeted;

        // 対象として選ばれたことの報告先
        private readonly Action<UnitStatusModel> _onTargetClicked;

        // 今効果を出しているカード。ダメージの数字に「何によるものか」を添えるのに使う（null を返してよい）
        private readonly Func<CardData> _currentActionCard;

        public UnitPresenter(
            UnitStatusModel unitStatusModel,
            BattleUnitStatusView unitStatusView,
            CardSelectionModel selectionModel,
            TargetFocusModel focusModel,
            bool showStamina,
            Sprite portrait,
            EnemyData enemyData,
            Func<UnitStatusModel, bool> canBeTargeted,
            Action<UnitStatusModel> onTargetClicked,
            Func<CardData> currentActionCard = null)
        {
            _currentActionCard = currentActionCard;
            _unitStatusModel = unitStatusModel;
            _unitStatusView = unitStatusView;
            _selectionModel = selectionModel;
            _focusModel = focusModel;
            _showStamina = showStamina;
            _portrait = portrait;
            _enemyData = enemyData;
            _canBeTargeted = canBeTargeted;
            _onTargetClicked = onTargetClicked;
        }

        protected override void OnInitialize()
        {
            _unitStatusModel.OnHpChanged += HandleHpChanged;
            _unitStatusModel.OnStaminaChanged += HandleStaminaChanged;
            _unitStatusModel.OnStatusEffectsChanged += HandleStatusEffectsChanged;
            _unitStatusModel.OnStatusEffectValuesChanged += HandleStatusEffectValuesChanged;
            _unitStatusModel.OnStatusEffectApplied += HandleStatusEffectApplied;
            _unitStatusModel.OnStatusEffectBlocked += HandleStatusEffectBlocked;
            _unitStatusModel.OnRevived += HandleRevived;
            _selectionModel.OnPendingCardChanged += HandlePendingCardChanged;
            _focusModel.OnFocusedTargetChanged += HandleFocusedTargetChanged;
            _unitStatusView.OnTargetClicked += HandleTargetClicked;

            _unitStatusView.SetStaminaActive(_showStamina);
            _unitStatusView.SetPortrait(_portrait);
            SyncName();
            _unitStatusView.SetTargetSelectable(false);
            _unitStatusView.SetHp(_unitStatusModel.CurrentHp, _unitStatusModel.MaxHp);
            _unitStatusView.SetDefeated(_unitStatusModel.CurrentHp == 0);
            _lastHp = _unitStatusModel.CurrentHp;
            if (_showStamina)
            {
                _unitStatusView.SetStamina(_unitStatusModel.Stamina, _unitStatusModel.MaxStamina);
            }

            SyncStatusEffects();
            _unitStatusView.SetBlock(GetBlockValue(), animate: false);
        }

        protected override void OnDispose()
        {
            _unitStatusModel.OnHpChanged -= HandleHpChanged;
            _unitStatusModel.OnStaminaChanged -= HandleStaminaChanged;
            _unitStatusModel.OnStatusEffectsChanged -= HandleStatusEffectsChanged;
            _unitStatusModel.OnStatusEffectValuesChanged -= HandleStatusEffectValuesChanged;
            _unitStatusModel.OnStatusEffectApplied -= HandleStatusEffectApplied;
            _unitStatusModel.OnStatusEffectBlocked -= HandleStatusEffectBlocked;
            _unitStatusModel.OnRevived -= HandleRevived;
            _selectionModel.OnPendingCardChanged -= HandlePendingCardChanged;
            _focusModel.OnFocusedTargetChanged -= HandleFocusedTargetChanged;
            _unitStatusView.OnTargetClicked -= HandleTargetClicked;
        }

        // 見出し・説明はローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            SyncStatusEffects();
            SyncName();
        }

        // 対象選択中だけ、そのカードで選べるユニットであることが分かる見た目にする。
        // 選べるかどうかの判定は受け付け側と同じものを使う（見た目と挙動がずれないように）
        private void HandlePendingCardChanged(CardData pendingCard)
        {
            _unitStatusView.SetTargetSelectable(_canBeTargeted.Invoke(_unitStatusModel));
        }

        // このユニットが指されている間（対象の候補一覧や実行リストのカードにカーソルを合わせている間）、強調する
        private void HandleFocusedTargetChanged(UnitStatusModel focused)
        {
            _unitStatusView.SetTargetFocused(focused == _unitStatusModel);
        }

        // 選べないユニットのクリックは報告先が弾くので、ここでは判定しない
        private void HandleTargetClicked()
        {
            _onTargetClicked.Invoke(_unitStatusModel);
        }

        private void SyncStatusEffects()
        {
            var displayData = new List<StatusEffectDisplayData>();
            foreach (var statusEffect in _unitStatusModel.StatusEffects)
            {
                // 値（ウイルスの強さ、残りターン数など）は見出しに添える。値を持たない効果（無効化など）は名前だけ
                string name = LocalizationHelper.GetStatusEffectName(statusEffect);
                displayData.Add(new StatusEffectDisplayData(
                    statusEffect.Value != 0 ? $"{name} {statusEffect.Value}" : name,
                    LocalizationHelper.GetStatusEffectDescription(statusEffect)));
            }

            _unitStatusView.SetStatusHeader(LocalizationHelper.GetUiText(UiTextKey.StatusHeader));
            _unitStatusView.SetStatusEffects(displayData);
        }

        private void HandleStatusEffectsChanged()
        {
            SyncStatusEffects();
            SyncBlock();
            SyncStamina();
        }

        // メモリー増設が付いた・切れたときは、最大値が変わるので出し直す
        private void SyncStamina()
        {
            if (!_showStamina) return;
            _unitStatusView.SetStamina(_unitStatusModel.Stamina, _unitStatusModel.MaxStamina);
        }

        private void SyncName()
        {
            _unitStatusView.SetName(_enemyData != null ? LocalizationHelper.GetEnemyName(_enemyData) : null);
        }

        // 重ねがけ（数は変わらず値だけ増えた）
        private void HandleStatusEffectValuesChanged()
        {
            SyncStatusEffects();
            SyncBlock();
        }

        // ブロックの残り。吸収で減ったときは HP 変化の通知（吸収し切って HP が減らなくても来る）で、
        // 得た・重ねた・切れたときは状態効果の通知で知る
        private void SyncBlock()
        {
            _unitStatusView.SetBlock(GetBlockValue(), animate: true);
        }

        private int GetBlockValue()
        {
            int value = 0;
            foreach (var effect in _unitStatusModel.StatusEffects)
            {
                if (effect is BlockStatusEffect) value += effect.Value;
            }
            return value;
        }

        // 生成で戦場に戻った: 回復の演出は出さず、HP・倒れた表示・状態効果をそのまま描き直す
        private void HandleRevived()
        {
            _unitStatusView.SetHp(_unitStatusModel.CurrentHp, _unitStatusModel.MaxHp);
            _unitStatusView.SetDefeated(_unitStatusModel.CurrentHp == 0);
            _lastHp = _unitStatusModel.CurrentHp;
            SyncStatusEffects();
            _unitStatusView.SetBlock(GetBlockValue(), animate: false);
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            int delta = currentHp - _lastHp;
            _unitStatusView.SetHp(currentHp, maxHp);
            _unitStatusView.PlayHpChanged(delta, DescribeHpChangeCause(delta));
            PlayHpChangedSe(delta);
            SyncBlock();
            _unitStatusView.SetDefeated(currentHp == 0);
            _lastHp = currentHp;
        }

        // 何による変動か：ターンの継続効果ならその効果の名前、そうでなければ今使われているカードの名前。
        // 回復が反転してダメージになったときは、反転させた効果の名前を添える
        private string DescribeHpChangeCause(int delta)
        {
            string cause = null;
            var source = _unitStatusModel.LastHpChangeSource;
            if (source != null)
            {
                cause = LocalizationHelper.GetStatusEffectName(source);
            }
            else
            {
                var card = _currentActionCard?.Invoke();
                if (card != null) cause = LocalizationHelper.GetCardName(card);
            }

            if (_unitStatusModel.LastHpChangeType == HpChangeType.Heal && delta < 0)
            {
                var reversal = _unitStatusModel.StatusEffects.Find(e => e.Id == HealReversalStatusEffect.EffectId);
                if (reversal != null)
                {
                    string reversalName = LocalizationHelper.GetStatusEffectName(reversal);
                    cause = cause != null ? $"{cause}（{reversalName}）" : reversalName;
                }
            }
            return cause;
        }

        private void PlayHpChangedSe(int delta)
        {
            if (delta > 0)
            {
                AudioUseCase.PlaySe(SeId.Heal);
            }
            else if (delta < 0)
            {
                // ウイルス・ワームの継続ダメージは、攻撃とは別の音にする
                AudioUseCase.PlaySe(_unitStatusModel.LastHpChangeType == HpChangeType.TakePoisonDamage ? SeId.VirusTick : SeId.Hit);
            }
        }

        // 付いた効果を、一覧と同じ見出し（名前と値）で浮かせる
        private void HandleStatusEffectApplied(IStatusEffect effect)
        {
            string name = LocalizationHelper.GetStatusEffectName(effect);
            _unitStatusView.PlayStatusText(effect.Value != 0 ? $"{name} {effect.Value}" : name, isBlocked: false);
            // ブロックは壁の演出（BattleBlockBarrierView）が自分の音を鳴らす
            if (!(effect is BlockStatusEffect)) AudioUseCase.PlaySe(SeId.StatusApply);
        }

        // 無効化されて付かなかった
        private void HandleStatusEffectBlocked(IStatusEffect effect)
        {
            string text = string.Format(LocalizationHelper.GetUiText(UiTextKey.StatusBlocked), LocalizationHelper.GetStatusEffectName(effect));
            _unitStatusView.PlayStatusText(text, isBlocked: true);
            AudioUseCase.PlaySe(SeId.Error);
        }

        private void HandleStaminaChanged(int stamina)
        {
            _unitStatusView.SetStamina(stamina, _unitStatusModel.MaxStamina);
        }
    }
}
