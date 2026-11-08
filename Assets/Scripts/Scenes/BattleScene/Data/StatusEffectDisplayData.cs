namespace Live2DCardRogue.BattleScene.Data
{
    // 状態効果1件の表示内容。PresenterがLocalizationHelperで解決してViewに渡す
    public sealed class StatusEffectDisplayData
    {
        public string Name { get; }
        public string Description { get; }

        public StatusEffectDisplayData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
