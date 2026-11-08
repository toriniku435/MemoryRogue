namespace Live2DCardRogue.Common.Localization
{
    /// <summary>選べる言語1つ（Locale のコードと、その言語自身での名前）</summary>
    public readonly struct LocaleOption
    {
        public string Code { get; }
        public string Name { get; }

        public LocaleOption(string code, string name)
        {
            Code = code;
            Name = name;
        }
    }
}
