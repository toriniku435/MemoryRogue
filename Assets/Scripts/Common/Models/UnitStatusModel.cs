using Live2DCardRogue.Common.Statuses;
using System;
using System.Collections.Generic;

namespace Live2DCardRogue.Common.Models
{
    public class UnitStatusModel
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        /// <summary>状態効果が掛かる前のスタミナ（メモリー）の最大値</summary>
        public int BaseMaxStamina { get; private set; }

        /// <summary>状態効果（メモリー増設など）込みのスタミナの最大値</summary>
        public int MaxStamina => Math.Max(0, CalculateLimit(StatLimitType.MaxStamina, BaseMaxStamina));

        public int Stamina { get; private set; }

        public List<IStatusEffect> StatusEffects { get; } = new List<IStatusEffect>();

        public event Action<int, int> OnHpChanged;

        /// <summary>直前の HP 変動の種別（OnHpChanged の購読側が、何による変動かを知るため）</summary>
        public HpChangeType LastHpChangeType { get; private set; }

        /// <summary>
        /// 直前の HP 変動を起こした状態効果（ウイルスの継続ダメージ・リジェネなど、ターンのフックの中で起きたもの）。
        /// カードなど状態効果の外で起きた変動なら null
        /// </summary>
        public IStatusEffect LastHpChangeSource { get; private set; }

        // ターンのフックを実行中の状態効果。その間に起きた HP 変動の原因として記録する
        private IStatusEffect _runningEffect;
        public event Action<int> OnStaminaChanged;

        /// <summary>StatusEffectsの中身が増減したときに発火する（値の変化では発火しない）</summary>
        public event Action OnStatusEffectsChanged;

        /// <summary>
        /// 既にある状態効果に、同じ効果が重ねられたときに発火する（ブロックの重ねがけなど、数は変わらず値だけが変わる場合）。
        /// ダメージの割り込みで値が減った場合は OnHpChanged の方で分かるので、ここでは発火しない
        /// </summary>
        public event Action OnStatusEffectValuesChanged;

        public UnitStatusModel(int maxHp, int maxStamina)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
            BaseMaxStamina = maxStamina;
            Stamina = maxStamina;
        }

        /// <summary>
        /// 状態効果が付いた（新しく付いた・既にある効果に重なった）ときに発火する。引数は付いた後の効果
        /// （重なった場合は既にあった方。値は重ねた後のもの）
        /// </summary>
        public event Action<IStatusEffect> OnStatusEffectApplied;

        /// <summary>状態効果が無効化されて付かなかったときに発火する（引数は付かなかった効果）</summary>
        public event Action<IStatusEffect> OnStatusEffectBlocked;

        public void AddStatusEffect(IStatusEffect effect)
        {
            // 無効化する効果が付いていれば、付けずに終わる（ウイルス無効など）
            foreach (var existingEffect in StatusEffects)
            {
                if (existingEffect.BlocksStatusEffect(effect))
                {
                    OnStatusEffectBlocked?.Invoke(effect);
                    return;
                }
            }

            // 既に同じ効果が存在する場合は、重複時の振る舞いを効果自身に任せる
            foreach (var existingEffect in StatusEffects)
            {
                if (existingEffect.IsSameEffect(effect))
                {
                    existingEffect.OnDuplicated(effect.Value);
                    OnStatusEffectValuesChanged?.Invoke();
                    OnStatusEffectApplied?.Invoke(existingEffect);
                    return;
                }
            }
            StatusEffects.Add(effect);
            OnStatusEffectsChanged?.Invoke();
            OnStatusEffectApplied?.Invoke(effect);
        }

        /// <summary>
        /// 上限・回数に状態効果の割り込みを適用した結果を返す（メモリー容量・引く手札の枚数・行動回数など）。
        /// 自分の状態は変えない。どの上限を持つかはユニットの外（ルール側）が決めるので、元の値は呼び出し側が渡す
        /// </summary>
        public int CalculateLimit(StatLimitType type, int value)
        {
            int finalValue = value;
            foreach (var effect in StatusEffects)
            {
                finalValue = effect.OnStatLimit(type, finalValue);
            }
            return finalValue;
        }

        public void RestoreStamina()
        {
            Stamina = MaxStamina;
            OnStaminaChanged?.Invoke(Stamina);
        }

        /// <summary>
        /// このユニットが生み出す値に、状態効果の割り込みを適用した結果を返す。
        /// ChangeHp/ChangeStaminaが「受ける側」なのに対し、こちらは「与える側」で、
        /// 自分の状態は変えずに値を返すだけ（適用先の決定は呼び出し側の仕事）。
        ///
        /// 攻撃力上昇のような強化バフをカード効果ごとに書かずに済ませるための入口
        /// </summary>
        /// <param name="type">生み出す値の種別</param>
        /// <param name="value">割り込み適用前の大きさ</param>
        public int CalculateDealtValue(DealtValueType type, int value)
        {
            int finalValue = value;
            foreach (var effect in StatusEffects)
            {
                finalValue = effect.OnDealtValue(type, finalValue);
            }
            return finalValue;
        }

        /// <summary>
        /// スタミナを変動させる汎用処理。ChangeHpと同じく、増減は amount の符号が持つ
        /// </summary>
        /// <param name="type">スタミナ変動の種別。どの割り込みを通すかだけを決める</param>
        /// <param name="amount">割り込み適用前の変動量。マイナスで消費、プラスで獲得</param>
        public void ChangeStamina(StaminaChangeType type, int amount)
        {
            int finalAmount = amount;
            foreach (var effect in StatusEffects)
            {
                finalAmount = effect.OnStaminaChange(type, finalAmount);
            }

            Stamina = Math.Clamp(Stamina + finalAmount, 0, MaxStamina);
            OnStaminaChanged?.Invoke(Stamina);
        }

        /// <summary>条件に合う状態効果を取り除く（ブロック解除など）。1つでも取り除いたら通知し、取り除いた数を返す</summary>
        public int RemoveStatusEffects(Predicate<IStatusEffect> match)
        {
            int removed = StatusEffects.RemoveAll(match);
            if (removed > 0) OnStatusEffectsChanged?.Invoke();
            return removed;
        }

        /// <summary>このユニットに今付いているブロックの合計</summary>
        public int BlockValue
        {
            get
            {
                int total = 0;
                foreach (var effect in StatusEffects)
                {
                    if (effect is BlockStatusEffect) total += effect.Value;
                }
                return total;
            }
        }

        /// <summary>プレイヤーがコマンドを1つ実行し始める。「次に実行するコマンド」に効く効果が、このコマンドに向けて準備する</summary>
        public void BeginCommand()
        {
            foreach (var effect in StatusEffects)
            {
                effect.OnCommandStart();
            }
        }

        /// <summary>実行し終えたコマンドの行き先を、状態効果込みで決める（ループバック・マルチスレッドなど）</summary>
        public CommandDestination CalculateCommandDestination(CommandDestination destination)
        {
            foreach (var effect in StatusEffects)
            {
                destination = effect.OnCommandDestination(destination);
            }
            return destination;
        }

        /// <summary>プレイヤーがコマンドを1つ実行し終えた。このコマンドで使い切った効果を取り除く</summary>
        public void EndCommand()
        {
            foreach (var effect in StatusEffects)
            {
                effect.OnCommandEnd();
            }
            RemoveExpiredStatusEffects();
        }

        public void StartTurn()
        {
            // ターン開始時の状態効果処理
            // 写しを回す: フックの中の回復が最大HPを超えると、オーバーチャージが一覧に加わるため
            foreach (var effect in StatusEffects.ToArray())
            {
                _runningEffect = effect;
                effect.OnTurnStart(this);
            }
            _runningEffect = null;
            RemoveExpiredStatusEffects();
        }

        public void EndTurn()
        {
            // ターン終了時の状態効果処理
            // 写しを回す: フックの中の回復が最大HPを超えると、オーバーチャージが一覧に加わるため
            foreach (var effect in StatusEffects.ToArray())
            {
                _runningEffect = effect;
                effect.OnTurnEnd(this);
            }
            _runningEffect = null;
            RemoveExpiredStatusEffects();
        }

        // 消費済みの状態効果を削除する。実際に減ったときだけ通知する。
        // メモリー増設が切れて最大値が下がったときは、今のスタミナも新しい最大値に収める
        private void RemoveExpiredStatusEffects()
        {
            if (StatusEffects.RemoveAll(effect => effect.IsExpired) > 0)
            {
                OnStatusEffectsChanged?.Invoke();

                if (Stamina > MaxStamina)
                {
                    Stamina = MaxStamina;
                    OnStaminaChanged?.Invoke(Stamina);
                }
            }
        }

        /// <summary>
        /// HPを変動させる汎用処理。ダメージも回復もここを通る。
        /// 増減は amount の符号が持つ（マイナスで減少、プラスで回復）ので、
        /// 状態効果が符号を反転させれば「回復を反転してダメージにする」といった効果も作れる。
        ///
        /// type は「どの割り込みを通すか」だけを決める。
        /// 毒ダメージが攻撃用の軽減まで受けてしまう、といった取り違えを防ぐため、種別は呼び出し側が明示する。
        /// スタミナ変動とenumを分けてあるので、HP変動でない種別はそもそも渡せない
        /// </summary>
        /// <param name="type">HP変動の種別。どの割り込みを通すかだけを決める</param>
        /// <param name="amount">割り込み適用前の変動量。マイナスで減少、プラスで回復</param>
        public void ChangeHp(HpChangeType type, int amount)
        {
            int finalAmount = amount;
            foreach (var effect in StatusEffects)
            {
                finalAmount = effect.OnHpChange(type, finalAmount);
            }

            // オーバーチャージは割り込み（ブロックなど）をすべて通った後に、HPより先に減る。
            // 即死はHPを確実に0にする処理なので肩代わりさせない
            if (finalAmount < 0 && type != HpChangeType.InstantKill)
            {
                finalAmount += AbsorbByOvercharge(-finalAmount);
            }

            long next = (long)CurrentHp + finalAmount;
            CurrentHp = (int)Math.Clamp(next, 0, MaxHp);
            LastHpChangeType = type;
            LastHpChangeSource = _runningEffect;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);

            // 最大HPであふれた回復はオーバーチャージとして溜める（HPの変化を通知した後に付けるので、ログは「回復」→「付いた」の順になる）。
            // 回復反転で符号が変わった回復はここに来ない
            if (CanOvercharge && finalAmount > 0 && next > MaxHp)
            {
                AddStatusEffect(new OverchargeStatusEffect((int)Math.Min(next - MaxHp, int.MaxValue), MaxHp));
            }
        }

        /// <summary>
        /// 最大HPを超えた回復をオーバーチャージとして溜められるか。プレイヤーだけが溜める
        /// （敵にも溜めさせると、HPが満タンのときの自己修復などでボスが強くなりすぎる）。戦闘側が有効にする
        /// </summary>
        public bool CanOvercharge { get; set; }

        /// <summary>今溜まっているオーバーチャージ</summary>
        public int OverchargeValue
        {
            get
            {
                foreach (var effect in StatusEffects)
                {
                    if (effect is OverchargeStatusEffect overcharge) return overcharge.Value;
                }
                return 0;
            }
        }

        // ダメージをオーバーチャージで肩代わりし、肩代わりした量を返す。値が減っただけでは通知しない（ブロックと同じ。OnHpChanged で分かる）
        private int AbsorbByOvercharge(int damage)
        {
            foreach (var effect in StatusEffects)
            {
                if (effect is OverchargeStatusEffect overcharge) return overcharge.Absorb(damage);
            }
            return 0;
        }

        /// <summary>
        /// ブロックを amount だけ消費する（リフレクション）。付いた順に減らし、尽きたものは次の掃除で外れる。
        /// 値の変化として通知する（バリアの表示とログが追えるように）。消費した量を返す
        /// </summary>
        public int SpendBlock(int amount)
        {
            int spent = 0;
            foreach (var effect in StatusEffects)
            {
                if (spent >= amount) break;
                if (effect is BlockStatusEffect block) spent += block.Spend(amount - spent);
            }
            if (spent > 0) OnStatusEffectValuesChanged?.Invoke();
            return spent;
        }

        /// <summary>
        /// 倒れたユニットを戦場に戻すとき（敵の生成）。状態効果をすべて外し、HPを最大まで戻す。
        /// 回復ではないので割り込み（回復反転など）は通さず、OnHpChanged ではなく OnRevived だけを流す
        /// （回復の数字やログを出さず、表示側はこの通知で全体を描き直す）
        /// </summary>
        public void Revive()
        {
            StatusEffects.Clear();
            CurrentHp = MaxHp;
            OnRevived?.Invoke();
        }

        public event Action OnRevived;


    }
}
