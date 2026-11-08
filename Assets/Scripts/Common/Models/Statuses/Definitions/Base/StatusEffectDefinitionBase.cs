using System;

namespace Live2DCardRogue.Common.Statuses
{
    /// <summary>
    /// アセット(EnemyDataなど)上で状態効果をオーサリングするための定義。
    ///
    /// 状態効果そのものを[SerializeReference]で持たせない理由:
    /// 実行時の状態効果はValueやIsExpiredを戦闘中に書き換えるため、アセットの実体をそのまま付与すると
    /// 変化がScriptableObjectに残ってしまう。定義と実体を分けておけば、定義側は読み取り専用のままでいられる。
    ///
    /// ローカライズの見出し・説明は生成された IStatusEffect.Id で引くので、
    /// 定義側はIdを持たない（＝同期させるものが無い）
    /// </summary>
    [Serializable]
    public abstract class StatusEffectDefinitionBase
    {
        /// <summary>この定義から実行時の状態効果を1つ生成する</summary>
        public abstract IStatusEffect Create();
    }
}
