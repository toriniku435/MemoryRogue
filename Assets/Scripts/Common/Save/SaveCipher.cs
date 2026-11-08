using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Live2DCardRogue.Common.Save
{
    // セーブファイルの暗号化と改ざん検知。
    //
    //   ファイルの形: [MAGIC 4バイト "L2CR"][形式の版 1バイト][IV 16バイト][暗号文][HMAC 32バイト]
    //   - 暗号化: AES-256-CBC（PKCS7）。IV は書き込むたびに乱数で作るので、同じ中身でも毎回違うファイルになる
    //   - 改ざん検知: 先頭から暗号文の終わりまでに HMAC-SHA256 を付ける（Encrypt-then-MAC）。
    //     1バイトでも書き換えられた・壊れたファイルは、復号する前に弾く
    //
    // 鍵はゲームの中に持つしかない（オフラインのゲームなので）。手作業での書き換えを防ぎ、壊れたファイルを見つけるための仕組みで、
    // ゲーム本体を解析して鍵を取り出す相手までは防げない。鍵の元は数か所に分けて置き、そのままの文字列では持たない
    public sealed class SaveCipher
    {
        private static readonly byte[] Magic = { (byte)'L', (byte)'2', (byte)'C', (byte)'R' };
        private const byte FormatVersion = 1;
        private const int IvSize = 16;
        private const int MacSize = 32;
        private const int HeaderSize = 4 + 1 + IvSize;

        private readonly byte[] _encryptionKey;
        private readonly byte[] _macKey;

        /// <summary>ゲームが使う鍵</summary>
        public static SaveCipher Default { get; } = new SaveCipher(BuildSecret());

        /// <param name="secret">鍵の元。ここから暗号化用と改ざん検知用の2つの鍵を作る</param>
        public SaveCipher(byte[] secret)
        {
            _encryptionKey = Derive(secret, "encrypt");
            _macKey = Derive(secret, "mac");
        }

        public byte[] Encrypt(string plaintext)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            byte[] cipherText;
            using (var encryptor = aes.CreateEncryptor())
            {
                var plainBytes = Encoding.UTF8.GetBytes(plaintext ?? string.Empty);
                cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            using var stream = new MemoryStream();
            stream.Write(Magic, 0, Magic.Length);
            stream.WriteByte(FormatVersion);
            stream.Write(aes.IV, 0, IvSize);
            stream.Write(cipherText, 0, cipherText.Length);
            var body = stream.ToArray();

            var mac = ComputeMac(body, body.Length);
            var result = new byte[body.Length + MacSize];
            Buffer.BlockCopy(body, 0, result, 0, body.Length);
            Buffer.BlockCopy(mac, 0, result, body.Length, MacSize);
            return result;
        }

        /// <summary>改ざん・破損していなければ復号する。形が違う・署名が合わない・復号できないときは false</summary>
        public bool TryDecrypt(byte[] data, out string plaintext)
        {
            plaintext = null;
            if (data == null || data.Length < HeaderSize + MacSize + 16) return false;
            for (int i = 0; i < Magic.Length; i++)
            {
                if (data[i] != Magic[i]) return false;
            }
            if (data[Magic.Length] != FormatVersion) return false;

            // 先に署名を確かめる（書き換えられたものは復号しない）
            int bodyLength = data.Length - MacSize;
            var expected = ComputeMac(data, bodyLength);
            if (!FixedTimeEquals(expected, data, bodyLength)) return false;

            try
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Key = _encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                var iv = new byte[IvSize];
                Buffer.BlockCopy(data, Magic.Length + 1, iv, 0, IvSize);
                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(data, HeaderSize, bodyLength - HeaderSize);
                plaintext = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private byte[] ComputeMac(byte[] data, int length)
        {
            using var hmac = new HMACSHA256(_macKey);
            return hmac.ComputeHash(data, 0, length);
        }

        // 比べる時間から一致した長さを推測されないよう、途中で打ち切らずに最後まで比べる
        private static bool FixedTimeEquals(byte[] expected, byte[] data, int offset)
        {
            int diff = 0;
            for (int i = 0; i < MacSize; i++)
            {
                diff |= expected[i] ^ data[offset + i];
            }
            return diff == 0;
        }

        private static byte[] Derive(byte[] secret, string purpose)
        {
            using var sha = SHA256.Create();
            var purposeBytes = Encoding.UTF8.GetBytes("Live2DCardRogue.Save." + purpose);
            var input = new byte[secret.Length + purposeBytes.Length];
            Buffer.BlockCopy(secret, 0, input, 0, secret.Length);
            Buffer.BlockCopy(purposeBytes, 0, input, secret.Length, purposeBytes.Length);
            return sha.ComputeHash(input);
        }

        // 鍵の元。文字列のまま置くとバイナリから一目で見つかるので、ばらばらの値を組み合わせて作る。
        // これを変えると、それまでのセーブはすべて読めなくなる
        private static byte[] BuildSecret()
        {
            var a = new byte[] { 0x3A, 0x91, 0x5C, 0xE2, 0x07, 0xB4, 0x6F, 0x18 };
            var b = new byte[] { 0xD9, 0x24, 0x8E, 0x71, 0xC3, 0x5A, 0x0B, 0xF6 };
            var c = new byte[] { 0x62, 0xAF, 0x13, 0x9D, 0x48, 0xE7, 0x35, 0xCC };
            var secret = new byte[a.Length * 2];
            for (int i = 0; i < a.Length; i++)
            {
                secret[i] = (byte)(a[i] ^ c[i]);
                secret[a.Length + i] = (byte)(b[i] ^ c[a.Length - 1 - i]);
            }
            return secret;
        }
    }
}
