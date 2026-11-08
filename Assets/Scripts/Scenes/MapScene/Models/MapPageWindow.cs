namespace Live2DCardRogue.MapScene.Models
{
    // マップで部屋を出しておく範囲（何番目から何番目の手前まで）。
    // 今のページと前後のページの分だけを出し、それ以外はプールへ戻すので、何階層進んでも作る数は増えない
    public readonly struct MapPageWindow
    {
        /// <summary>出しておく最初の部屋（0始まり）</summary>
        public int First { get; }

        /// <summary>出しておく最後の部屋の次</summary>
        public int End { get; }

        private MapPageWindow(int first, int end)
        {
            First = first;
            End = end < first ? first : end;
        }

        /// <param name="page">今のページ（0始まり）</param>
        /// <param name="pagesAround">前後に何ページ出しておくか</param>
        /// <param name="pageSize">1ページの部屋の数</param>
        /// <param name="stageCount">部屋の数。終わりが無ければ null</param>
        public static MapPageWindow Of(int page, int pagesAround, int pageSize, int? stageCount)
        {
            // とても深い階層でもあふれないよう long で計算し、int に収める
            long first = ((long)page - pagesAround) * pageSize;
            if (first < 0) first = 0;
            long end = ((long)page + pagesAround + 1) * pageSize;
            if (stageCount.HasValue && end > stageCount.Value) end = stageCount.Value;
            if (end > int.MaxValue) end = int.MaxValue;
            if (first > end) first = end;
            return new MapPageWindow((int)first, (int)end);
        }

        public bool Contains(int index)
        {
            return index >= First && index < End;
        }
    }
}
