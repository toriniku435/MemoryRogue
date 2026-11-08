#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Live2DCardRogue.Common.Save
{
    // WebGL の persistentDataPath はメモリ上のファイルシステムで、ブラウザの IndexedDB へは明示的に書き戻さないと
    // ページを閉じたときに消えることがある。書き戻しの本体は Assets/Plugins/WebGL/FileSystemSync.jslib。
    // ほかのプラットフォームでは何もしない
    public static class WebGLFileSync
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SyncFileSystemToIndexedDb();
#endif

        public static void Flush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SyncFileSystemToIndexedDb();
#endif
        }
    }
}
