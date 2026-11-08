using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // ユニット（プレイヤー/敵）のHP・スタミナ（コスト）表示と、カードの対象選択領域を持つView
    // _staminaText を未設定のままにしておくと、そのユニットはコスト非表示になる（敵側などで利用）。
    // スタミナは画面上では「メモリー残量」と呼び、コマ型のゲージで見せる
    public sealed class BattleUnitStatusView : MonoBehaviour
    {
        [SerializeField] private CommonText _hpText;
        [SerializeField] private CommonText _staminaText;
        [SerializeField] private BattleUnitTargetView _targetView;
        [SerializeField] private GameObject _staminaParent;
        // スタミナのゲージ（未設定なら数字だけ）
        [SerializeField] private SegmentGaugeView _staminaGauge;

        // 状態効果表示。表示しないユニットは未設定のままでよい
        [SerializeField] private Transform _statusEffectParent;
        [SerializeField] private StatusEffectView _statusEffectViewPrefab;
        // 状態効果の一覧の見出し（行動予告と見分けるため）。効果が1つも無ければ隠す
        [SerializeField] private CommonText _statusHeader;

        // 見た目の差し替え口。どれも未設定のままでよい
        [SerializeField] private Image _portraitImage;

        // 敵の名前。未設定でもよい（名前の無いユニットでは隠す）
        [SerializeField] private CommonText _nameText;
        // HPバーの中身。右端のアンカーを残りHPの割合に合わせて縮める（Filled と違い、角丸の9-sliceが潰れない）
        [SerializeField] private RectTransform _hpFill;
        [SerializeField] private BattleUnitEffectView _effectView;
        // ブロックを壁として見せる（未設定なら状態効果の一覧に名前が出るだけ）
        [SerializeField] private BattleBlockBarrierView _blockBarrier;
        // 敵の次の行動の予告。プレイヤーには無い（未設定のまま）
        [SerializeField] private BattleEnemyIntentView _intentView;
        // 頭上の優先ターゲットの印（未設定なら出さない）。優先ターゲットの間は名前もこの色にする
        [SerializeField] private BattlePriorityTargetView _priorityTargetView;
        [SerializeField] private Color _priorityNameColor = new Color(1f, 0.902f, 0f, 1f);

        private Color? _normalNameColor;
        [SerializeField] private Color _defeatedPortraitColor = new Color(0.6f, 0.55f, 0.65f, 0.45f);

        // 動く見た目（アイドル・行動・被弾）。ApplyVisual で作るまでは null で、立ち絵の1枚絵が出ている
        private IUnitVisual _visual;

        // 一度生成したViewは破棄せずアクティブ制御で使い回す（手札のCardViewと同じ方針）
        private readonly List<StatusEffectView> _statusEffectViews = new List<StatusEffectView>();
        // タッチ用: 状態効果の説明をカーソルで開くか（false ならタップを OnStatusEffectClicked で伝え、開くのは SetExpandedStatusEffect）
        private bool _statusExpandsOnHover = true;
        private int _expandedStatusIndex = -1;

        /// <summary>状態効果の行がタップされた（引数は上からの番号）</summary>
        public event Action<int> OnStatusEffectClicked;

        // 対象として選ばれたことの通知。Viewは誰に伝わるかを知らず、Presenterが受けて使用側へ流す
        public event Action OnTargetClicked
        {
            add { _targetView.OnClicked += value; }
            remove { _targetView.OnClicked -= value; }
        }

        public void SetTargetSelectable(bool isSelectable)
        {
            _targetView.SetSelectable(isSelectable);
        }

        public void SetTargetFocused(bool isFocused)
        {
            _targetView.SetFocused(isFocused);
        }

        public BattleEnemyIntentView IntentView => _intentView;

        public void SetPriorityTarget(bool isPriority)
        {
            if (_priorityTargetView != null) _priorityTargetView.SetVisible(isPriority);

            if (_nameText == null) return;
            if (_normalNameColor == null) _normalNameColor = _nameText.color;
            _nameText.color = isPriority ? _priorityNameColor : _normalNameColor.Value;
        }

        // 今表示している立ち絵（対象の候補一覧のアイコンに使う）
        public Sprite PortraitSprite => _portraitImage != null ? _portraitImage.sprite : null;

        public void SetStaminaActive(bool isActive)
        {
            _staminaParent.SetActive(isActive);
        }

        public void SetHp(int currentHp, int maxHp)
        {
            _hpText.text = $"{currentHp}/{maxHp}";

            if (_hpFill != null)
            {
                float ratio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
                _hpFill.anchorMax = new Vector2(ratio, _hpFill.anchorMax.y);
                _hpFill.gameObject.SetActive(ratio > 0f);
            }
        }

        /// <summary>名前を出す。null か空なら名前の欄ごと隠す</summary>
        public void SetName(string unitName)
        {
            if (_nameText == null) return;
            bool hasName = !string.IsNullOrEmpty(unitName);
            _nameText.gameObject.SetActive(hasName);
            if (hasName) _nameText.text = unitName;
        }

        // null のときはシーンに置いてある絵をそのまま使う
        public void SetPortrait(Sprite portrait)
        {
            if (_portraitImage == null || portrait == null) return;
            _portraitImage.sprite = portrait;
        }

        /// <summary>
        /// 立ち絵の枠の中に動く見た目を作り、以後の被弾・倒れた表示をそちらへ流す。立ち絵の1枚絵はアイコン用に残して隠す。
        /// 定義が無ければ何もせず null（1枚絵のまま）
        /// </summary>
        public IUnitVisual ApplyVisual(UnitVisualDefinition definition)
        {
            if (definition == null || _portraitImage == null || _visual != null) return _visual;

            if (definition.Icon != null) _portraitImage.sprite = definition.Icon;
            _visual = definition.Create(_portraitImage.rectTransform);
            // 1枚絵は描かないが、スプライトはアイコンとして読まれるので残す
            _portraitImage.enabled = false;
            if (_effectView != null) _effectView.SetColorTarget(_visual);
            return _visual;
        }

        public void PlayAction()
        {
            _visual?.PlayAction();
        }

        public void SetBlock(int value, bool animate)
        {
            if (_blockBarrier != null) _blockBarrier.SetValue(value, animate);
        }

        public void PlayStatusText(string text, bool isBlocked)
        {
            if (_effectView != null) _effectView.PlayStatusText(text, isBlocked);
        }

        public void PlayHpChanged(int delta, string cause)
        {
            if (_effectView != null) _effectView.PlayHpChanged(delta, cause);
            if (delta < 0) _visual?.PlayHit();
        }

        public void SetDefeated(bool isDefeated)
        {
            var color = isDefeated ? _defeatedPortraitColor : Color.white;
            _visual?.SetDefeated(isDefeated);
            if (_effectView != null)
            {
                _effectView.SetBaseColor(color);
            }
            else if (_visual != null)
            {
                _visual.SetColor(color);
            }
            else if (_portraitImage != null)
            {
                _portraitImage.color = color;
            }
        }

        public void SetStamina(int stamina, int maxStamina)
        {
            if (_staminaText == null || !_staminaParent.activeSelf) return;
            _staminaText.text = $"{stamina}/{maxStamina}";
            if (_staminaGauge != null) _staminaGauge.SetValue(stamina, maxStamina);
        }

        public void SetStatusHeader(string text)
        {
            if (_statusHeader != null) _statusHeader.text = text;
        }

        public void SetStatusEffects(IReadOnlyList<StatusEffectDisplayData> statusEffects)
        {
            if (_statusEffectParent == null || _statusEffectViewPrefab == null) return;
            if (_statusHeader != null) _statusHeader.gameObject.SetActive(statusEffects.Count > 0);

            for (int i = 0; i < statusEffects.Count; i++)
            {
                if (i >= _statusEffectViews.Count)
                {
                    var created = Instantiate(_statusEffectViewPrefab, _statusEffectParent);
                    int index = i;
                    created.OnClicked += () => OnStatusEffectClicked?.Invoke(index);
                    created.ExpandsOnHover = _statusExpandsOnHover;
                    _statusEffectViews.Add(created);
                }

                _statusEffectViews[i].SetStatusEffect(statusEffects[i].Name, statusEffects[i].Description);
                _statusEffectViews[i].SetActive(true);
            }

            for (int i = statusEffects.Count; i < _statusEffectViews.Count; i++)
            {
                _statusEffectViews[i].SetActive(false);
            }
            if (!_statusExpandsOnHover) ApplyExpandedStatusEffect();
        }

        public void SetStatusEffectsExpandOnHover(bool expandsOnHover)
        {
            _statusExpandsOnHover = expandsOnHover;
            foreach (var view in _statusEffectViews) view.ExpandsOnHover = expandsOnHover;
        }

        /// <summary>その番号の状態効果だけ説明を開く（-1 で全部閉じる）。一覧が付け直されても開いたままにする</summary>
        public void SetExpandedStatusEffect(int index)
        {
            _expandedStatusIndex = index;
            ApplyExpandedStatusEffect();
        }

        private void ApplyExpandedStatusEffect()
        {
            for (int i = 0; i < _statusEffectViews.Count; i++)
            {
                if (_statusEffectViews[i].gameObject.activeSelf) _statusEffectViews[i].SetExpanded(i == _expandedStatusIndex);
            }
        }
    }
}
