namespace Live2DCardRogue.Common.Message
{
    // トースト表示の入口。View(MessageToast)のシングルトン生成と寿命はView側が持つので、
    // ここはViewを知らない呼び出し側（UseCaseなど）から文字列1本で呼べるようにするだけ
    public static class MessageUseCase
    {
        public static void ShowMessage(string message)
        {
            MessageToast.Show(message);
        }
    }
}
