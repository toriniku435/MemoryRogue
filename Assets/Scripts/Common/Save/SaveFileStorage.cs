using System.IO;

namespace Live2DCardRogue.Common.Save
{
    // セーブファイルの読み書き（1つのフォルダの中のファイル）。中身の形も暗号化も知らない（暗号化は SaveCipher）。
    //
    // 書き込みは「一時ファイルに書いてから置き換える」。書いている途中で落ちても、前のファイルは壊れずに残る。
    // PlayerPrefs を使わないのは、Windows ではレジストリに入って構造のあるデータに向かず、
    // バックアップや削除、将来のクラウド同期（ファイル単位）もしにくいため
    public sealed class SaveFileStorage
    {
        private const string TempSuffix = ".tmp";

        public string Directory { get; }

        public SaveFileStorage(string directory)
        {
            Directory = directory;
        }

        public bool Exists(string fileName)
        {
            return File.Exists(PathOf(fileName));
        }

        /// <summary>読めなければ false（無い・読み取りに失敗した）</summary>
        public bool TryRead(string fileName, out string text)
        {
            text = null;
            var path = PathOf(fileName);
            if (!File.Exists(path)) return false;
            try
            {
                text = File.ReadAllText(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>読めなければ false（無い・読み取りに失敗した）</summary>
        public bool TryReadBytes(string fileName, out byte[] data)
        {
            data = null;
            var path = PathOf(fileName);
            if (!File.Exists(path)) return false;
            try
            {
                data = File.ReadAllBytes(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public void WriteAtomic(string fileName, string text)
        {
            WriteBytesAtomic(fileName, System.Text.Encoding.UTF8.GetBytes(text));
        }

        public void WriteBytesAtomic(string fileName, byte[] data)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var path = PathOf(fileName);
            var temp = path + TempSuffix;
            File.WriteAllBytes(temp, data);
            if (!File.Exists(path))
            {
                File.Move(temp, path);
            }
            else if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WebGLPlayer)
            {
                // ブラウザのファイルシステムは File.Replace を確実には扱えない。ここはメモリ上で、
                // 途中で落ちても IndexedDB には前のファイルが残っている（書き戻すのは Flush の時だけ）ので、消してから移す
                File.Delete(path);
                File.Move(temp, path);
            }
            else
            {
                File.Replace(temp, path, null);
            }
            WebGLFileSync.Flush();
        }

        public void Delete(string fileName)
        {
            var path = PathOf(fileName);
            if (!File.Exists(path)) return;
            File.Delete(path);
            WebGLFileSync.Flush();
        }

        private string PathOf(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }
    }
}
