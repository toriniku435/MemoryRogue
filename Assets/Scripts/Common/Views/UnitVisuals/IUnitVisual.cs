using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // ユニット（プレイヤー・敵）の見た目の差し替え口。
    // 何もしていない間はアイドルを流し続け、行動・被弾は1回再生してアイドルへ戻る、を実装側が受け持つ。
    // 呼ぶ側は「何が起きたか」だけを伝え、コマ送りか Live2D か 3D かは知らない。
    // 実装は UnitVisualDefinition（ScriptableObject）の Create が作る
    public interface IUnitVisual
    {
        /// <summary>行動（カードを使った）。倒れている間は無視してよい</summary>
        void PlayAction();

        /// <summary>被弾（HP が減った）</summary>
        void PlayHit();

        /// <summary>倒れた・起き上がった。倒れている間はアイドルを止める</summary>
        void SetDefeated(bool isDefeated);

        /// <summary>全体に掛ける色（被弾の点滅や、倒れたときの薄さ）。白で元の色</summary>
        void SetColor(Color color);
    }
}
