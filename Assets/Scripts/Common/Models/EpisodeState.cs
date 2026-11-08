namespace Live2DCardRogue.Common.Models
{
    // エピソード選択でのエピソードの状態
    public enum EpisodeState
    {
        /// <summary>まだ選べない（ひとつ前のエピソードをクリアしていない）。ボタンは押せない見た目</summary>
        Locked,

        /// <summary>選べるが、中身（ステージ）がまだ無い</summary>
        NotReady,

        /// <summary>遊べる</summary>
        Playable,
    }
}
