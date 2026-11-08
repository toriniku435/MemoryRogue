using Live2DCardRogue.MapScene.Presenters;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // マップの経路の座標: ページ、原点からの差で置くこと、プレイヤーが立つ部屋
    public sealed class MapRouteLayoutTests
    {
        private const float CellWidth = 100f;

        [Test]
        public void PageOf_GroupsTenRoomsPerPage()
        {
            Assert.AreEqual(0, MapRouteLayout.PageOf(0));
            Assert.AreEqual(0, MapRouteLayout.PageOf(9));
            Assert.AreEqual(1, MapRouteLayout.PageOf(10));
            Assert.AreEqual(0, MapRouteLayout.PageOf(-3));
        }

        [Test]
        public void FirstPage_IsCenteredOnTheScreen()
        {
            var layout = new MapRouteLayout(CellWidth, stageCount: 10);
            layout.SetOrigin(0);

            float first = layout.CellPosition(0).x;
            float last = layout.CellPosition(9).x;
            Assert.AreEqual(0f, first + last, 0.001f);
        }

        // 部屋が1ページに満たないストーリーは、その数で真ん中に寄せる
        [Test]
        public void ShortRoute_IsCenteredByItsOwnCount()
        {
            var layout = new MapRouteLayout(CellWidth, stageCount: 3);
            layout.SetOrigin(0);

            Assert.AreEqual(0f, layout.CellPosition(0).x + layout.CellPosition(2).x, 0.001f);
        }

        // 何十万階層進んでも、原点のページからの差で置くので座標は小さいまま
        [Test]
        public void FarRooms_StayNearTheOriginPage()
        {
            var layout = new MapRouteLayout(CellWidth, stageCount: null);
            layout.SetOrigin(500_003);

            Assert.AreEqual(0f, layout.CellPosition(500_000).x + layout.CellPosition(500_009).x, 0.001f);
            Assert.AreEqual(0f, layout.PageOffset(MapRouteLayout.PageOf(500_003)), 0.001f);
        }

        [Test]
        public void PageOffset_MovesOnePageWidthPerPage()
        {
            var layout = new MapRouteLayout(CellWidth, stageCount: null);
            layout.SetOrigin(0);

            float onePage = layout.CellPosition(10).x - layout.CellPosition(0).x;
            Assert.AreEqual(onePage, layout.PageOffset(1), 0.001f);
        }

        [Test]
        public void PlayerCellIndex_StaysOnTheLastRoomAfterClearing()
        {
            var finite = new MapRouteLayout(CellWidth, stageCount: 10);
            Assert.AreEqual(9, finite.PlayerCellIndex(10));
            Assert.AreEqual(0, finite.PlayerCellIndex(-1));

            var endless = new MapRouteLayout(CellWidth, stageCount: null);
            Assert.AreEqual(25, endless.PlayerCellIndex(25));
        }

        [Test]
        public void HasRooms_IsFalseOnlyForAnEmptyRoute()
        {
            Assert.IsFalse(new MapRouteLayout(CellWidth, 0).HasRooms);
            Assert.IsTrue(new MapRouteLayout(CellWidth, 1).HasRooms);
            Assert.IsTrue(new MapRouteLayout(CellWidth, null).HasRooms);
        }
    }
}
