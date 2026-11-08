using UnityEngine;

namespace Live2DCardRogue.MapScene.Presenters
{
    // マップの経路の座標の計算（部屋の位置・ページ・ページ送りのずらし量・プレイヤーの立ち位置）。
    // 部屋は10個で1ページ。経路はページが進むほど右へ伸び、今のページが画面の真ん中に来るよう経路ごと横へずらす。
    //
    // 部屋の位置は「原点の部屋」からの差で出す。何十万階層進んでも座標が小さいままなので、
    // float の精度が落ちて部屋や移動がずれることがない
    public sealed class MapRouteLayout
    {
        /// <summary>1ページの部屋の数</summary>
        public const int PageSize = 10;

        private const float CellSpacing = 40f;
        // 部屋を上下に交互にずらして、経路をジグザグにする量
        private const float ZigzagOffsetY = 55f;
        // プレイヤーは部屋の上に立つ
        private const float PlayerOffsetY = 105f;

        private readonly int? _stageCount;
        // 隣の部屋までの距離（部屋の幅 + 間隔）と、原点のページの最初の部屋の x（そのページが画面の真ん中に来るように）
        private readonly float _cellStep;
        private readonly float _startX;
        private int _originIndex;

        /// <param name="stageCount">部屋の数。終わりが無ければ null</param>
        public MapRouteLayout(float cellWidth, int? stageCount)
        {
            _stageCount = stageCount;
            _cellStep = cellWidth + CellSpacing;
            // 部屋が1ページに満たないストーリーは、その数で真ん中に寄せる
            int firstPageCount = stageCount.HasValue ? Mathf.Min(PageSize, stageCount.Value) : PageSize;
            _startX = -(firstPageCount * cellWidth + (firstPageCount - 1) * CellSpacing) / 2f + cellWidth / 2f;
        }

        public bool HasRooms => !_stageCount.HasValue || _stageCount.Value > 0;

        /// <summary>原点にする部屋を、その部屋のページの最初の部屋に決める（開いたときに見せるページ）</summary>
        public void SetOrigin(int cellIndex)
        {
            _originIndex = PageOf(cellIndex) * PageSize;
        }

        /// <summary>経路の座標での部屋の位置（上下に交互にずらしてジグザグにする）</summary>
        public Vector2 CellPosition(int index)
        {
            long offset = (long)index - _originIndex;
            return new Vector2(_startX + offset * _cellStep, index % 2 == 0 ? ZigzagOffsetY : -ZigzagOffsetY);
        }

        /// <summary>プレイヤーの立ち位置（プレイヤーの親は部屋の親と同じ座標系に置いてある）</summary>
        public Vector2 PlayerPosition(int cellIndex)
        {
            return CellPosition(cellIndex) + Vector2.up * PlayerOffsetY;
        }

        public static int PageOf(int index)
        {
            return Mathf.Max(0, index) / PageSize;
        }

        /// <summary>そのページを画面の真ん中に置くための、経路のずらし量</summary>
        public float PageOffset(int page)
        {
            return ((long)page * PageSize - _originIndex) * _cellStep;
        }

        /// <summary>プレイヤーが立つ部屋。全部踏破した後は最後の部屋に立たせておく（終わりの無いモードはそのまま）</summary>
        public int PlayerCellIndex(int stageIndex)
        {
            int index = Mathf.Max(0, stageIndex);
            return _stageCount.HasValue ? Mathf.Min(index, _stageCount.Value - 1) : index;
        }
    }
}
