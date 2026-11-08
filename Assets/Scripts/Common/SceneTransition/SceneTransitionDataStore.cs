namespace Live2DCardRogue.Common.SceneTransition
{
    // シーンをまたいで渡すデータ（SceneTransitionData）の受け渡し口。
    // 渡す側が Set し、受け取る側のInitializer（SceneInitializerBase）が TryConsume で取り出す。
    //
    // 取り出しは1度きりで、受け取れた時点で中身を捨てる。
    // 「前のシーンで置かれたデータが、次の遷移にも残っている」状態を作らないための割り切り。
    //
    // 型が一致しない場合は捨てずに残す。空振りした側が、別の型で読むはずのデータを消してしまわないように。
    // なお遷移のたびに Set で上書きされるので、読まれなかったデータが溜まることはない
    public static class SceneTransitionDataStore
    {
        private static SceneTransitionData _parameter;

        public static void Set(SceneTransitionData parameter)
        {
            _parameter = parameter;
        }

        public static bool TryConsume<T>(out T parameter) where T : SceneTransitionData
        {
            parameter = _parameter as T;
            if (parameter == null) return false;

            _parameter = null;
            return true;
        }

        public static void Clear()
        {
            _parameter = null;
        }
    }
}
