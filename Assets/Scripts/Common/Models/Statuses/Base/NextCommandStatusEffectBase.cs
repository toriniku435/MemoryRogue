namespace Live2DCardRogue.Common.Statuses
{
    // 「次に実行するコマンド」に1回だけ効いて消える効果の基底（ループバック・マルチスレッド）。
    //
    // 効くのは、付いた後で実行し始めたコマンドだけ。付けたコマンド自身の実行中に付いた効果は、
    // そのコマンドの OnCommandStart を通っていないので準備ができておらず、効かないし消えもしない。
    // 次のコマンドの OnCommandStart で準備ができ、そのコマンドの OnCommandEnd で使い切って消える。
    // ターンをまたいでも残る（次のターンの最初のコマンドに効く）
    public abstract class NextCommandStatusEffectBase : StatusEffectBase
    {
        /// <summary>今実行しているコマンドに効くか</summary>
        protected bool IsArmed { get; private set; }

        protected NextCommandStatusEffectBase(int value) : base(value)
        {
        }

        public override void OnCommandStart()
        {
            IsArmed = true;
        }

        public override void OnCommandEnd()
        {
            if (!IsArmed) return;

            IsArmed = false;
            IsExpired = true;
        }
    }
}
