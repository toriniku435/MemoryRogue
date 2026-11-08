namespace Live2DCardRogue.Window.DeckEdit.Views
{
    // デッキ編集ウインドウの表示のしかた。リストが見づらいときに、片方を隠してもう片方を広げる。
    // 並びは切り替えボタンの並び（DeckEditWindowView._layoutButtons）と同じ
    public enum DeckEditLayout
    {
        // 所持リスト・デッキリスト・拡大表示（今まで通り）
        Both,
        // デッキリストを広げて、拡大表示と並べる
        DeckOnly,
        // 所持リストを広げて、拡大表示と並べる
        OwnedOnly,
    }
}
