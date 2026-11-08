namespace Live2DCardRogue.Common.SceneTransition
{
    // シーン名。遷移元と遷移先で同じ文字列を書かずに済むよう、ここに1箇所だけ置く。
    // 値はシーンファイル名そのままで、Build Settingsの登録名と一致している必要がある
    public static class SceneName
    {
        public const string Title = "TitleScene";
        public const string Map = "MapScene";
        public const string Battle = "BattleScene";
        public const string Ending = "EndingScene";
        public const string Result = "ResultScene";
    }
}
