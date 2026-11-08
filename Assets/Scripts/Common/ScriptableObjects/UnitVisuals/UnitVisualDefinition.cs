using Live2DCardRogue.Common.Views;
using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // ユニットの見た目の定義。EnemyData（敵）と BattleSceneInitializer（プレイヤー）が持つ。
    // 表示の方式（コマ送り・Live2D など）ごとにサブクラスを作り、Create で IUnitVisual を組み立てて返す。
    // 呼ぶ側はこの型と IUnitVisual しか知らないので、アセットを差し替えるだけで方式ごと入れ替わる
    public abstract class UnitVisualDefinition : ScriptableObject
    {
        /// <summary>小さなアイコン（対象の候補一覧・ログ）に使う1枚絵</summary>
        public abstract Sprite Icon { get; }

        /// <summary>parent の中いっぱいに見た目を作る。parent の向き（左右反転）や揺れはそのまま引き継がれる</summary>
        public abstract IUnitVisual Create(RectTransform parent);
    }
}
