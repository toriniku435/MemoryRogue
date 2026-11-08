using System;
using System.IO;
using UnityEngine;

namespace Live2DCardRogue.Common.Save
{
    // セーブファイルを暗号化して書き、復号して読む。暗号化する前の平文の旧形式（.json）も読み、次に書いたときに消す。
    // 保存に失敗してもゲームは止めない（次の節目でまた保存する）
    public sealed class SaveFileCodec
    {
        private readonly SaveFileStorage _storage;
        private readonly SaveCipher _cipher;

        public SaveFileCodec(SaveFileStorage storage, SaveCipher cipher)
        {
            _storage = storage;
            _cipher = cipher;
        }

        public string Directory => _storage.Directory;

        /// <param name="legacyFileName">消す旧形式のファイル（無ければ null）</param>
        public void Write(string fileName, string legacyFileName, string json)
        {
            try
            {
                _storage.WriteBytesAtomic(fileName, _cipher.Encrypt(json));
                if (legacyFileName != null) _storage.Delete(legacyFileName);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"セーブに失敗しました（{fileName}）: {e.Message}");
            }
        }

        // 新しい形式（暗号化）を読む。無ければ旧形式（平文）を読む。
        // 書き換えられた・壊れた新しい形式のファイルは読まない（旧形式にも戻らない）
        public bool TryReadJson(string fileName, string legacyFileName, out string json)
        {
            json = null;
            if (_storage.TryReadBytes(fileName, out var data))
            {
                if (_cipher.TryDecrypt(data, out json)) return true;
                Debug.LogError($"セーブデータ {fileName} が書き換えられているか壊れているため、読み込みません");
                return false;
            }
            return legacyFileName != null && _storage.TryRead(legacyFileName, out json);
        }

        public void Delete(string fileName)
        {
            _storage.Delete(fileName);
        }
    }
}
