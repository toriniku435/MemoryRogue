namespace Live2DCardRogue.TitleScene
{
    // タイトルから選べるモード。ボタン1つにつき1つ対応する。
    // この並び順が画面の並び順なのではなく、どのボタンがどのモードかはInspector側で決める
    public enum GameMode
    {
        Campaign,
        // ノーマル / ハード / マスター / エンドレス（以前はエンドレスのボタン。値はそのまま）
        Challenge,
        Collection,
        Options,
    }
}
