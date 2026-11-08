using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.WindowSystem;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 共通メニューの中身: 項目は渡した順に並び、押せるかは渡した条件に従う（無ければ常に押せる）
    public sealed class MenuWindowDataTests
    {
        [Test]
        public void Items_KeepTheGivenOrder()
        {
            var data = new MenuWindowInitData(UiTextKey.Menu, null,
                new MenuItemData(UiTextKey.Options, () => { }),
                new MenuItemData(UiTextKey.GiveUp, () => { }, MenuItemStyle.Danger));

            Assert.AreEqual(UiTextKey.Menu, data.Title);
            Assert.AreEqual(2, data.Items.Count);
            Assert.AreEqual(UiTextKey.Options, data.Items[0].Label);
            Assert.AreEqual(MenuItemStyle.Normal, data.Items[0].Style);
            Assert.AreEqual(UiTextKey.GiveUp, data.Items[1].Label);
            Assert.AreEqual(MenuItemStyle.Danger, data.Items[1].Style);
        }

        [Test]
        public void CanPress_IsTrueWithoutCondition()
        {
            Assert.IsTrue(new MenuItemData(UiTextKey.Options, () => { }).CanPress());
        }

        [Test]
        public void CanPress_FollowsTheCondition()
        {
            bool decided = false;
            var item = new MenuItemData(UiTextKey.GiveUp, () => { }, canPress: () => !decided);

            Assert.IsTrue(item.CanPress());
            decided = true;
            Assert.IsFalse(item.CanPress());
        }

        [Test]
        public void Confirm_IsNullUnlessGiven()
        {
            Assert.IsNull(new MenuItemData(UiTextKey.Options, () => { }).Confirm);

            var confirm = new MenuConfirmData(UiTextKey.SuspendConfirm, UiTextKey.SuspendConfirmDetail, UiTextKey.SuspendOk, UiTextKey.Back);
            Assert.AreSame(confirm, new MenuItemData(UiTextKey.Suspend, () => { }, confirm: confirm).Confirm);
        }
    }
}
