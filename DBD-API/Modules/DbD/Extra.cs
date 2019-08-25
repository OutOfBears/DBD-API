using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Joveler.Compression.ZLib;

namespace DBD_API.Modules.DbD
{
    public static class ArrayExt
    {
        public static T[] Subset<T>(this T[] array, int start, int count)
        {
            T[] result = new T[count];
            Array.Copy(array, start, result, 0, count);
            return result;
        }
    }

    public static class Extra
    {
        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        public static string RawDecrypt(string text, string key)
        {
            byte[] cipher = Convert.FromBase64String(text);
            byte[] btkey = Encoding.ASCII.GetBytes(key);

            //init AES 128
            RijndaelManaged aes128 = new RijndaelManaged();
            aes128.Mode = CipherMode.ECB;
            aes128.Padding = PaddingMode.Zeros;

            //decrypt
            ICryptoTransform decryptor = aes128.CreateDecryptor(btkey, null);
            MemoryStream ms = new MemoryStream(cipher);
            CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

            byte[] plain = new byte[cipher.Length];
            int decryptcount = cs.Read(plain, 0, plain.Length);

            ms.Close();
            cs.Close();

            //return plaintext in String
            return Encoding.UTF8.GetString(plain, 0, decryptcount);
        }

        public static string DecryptCdn(string content, string key)
        {
            content = content.Substring(8).Trim();

            var decrypted = RawDecrypt(content, key);
            var transformed = "";
            foreach (var t in decrypted)
                transformed += (char)(t + 1);

            if (!transformed.StartsWith("DbdDAQEB")) return transformed;

            transformed = transformed.Replace("\x01", "");
            var b64Decoded = Convert.FromBase64String(transformed.Substring(8));
            var decoded = b64Decoded.Subset(4, b64Decoded.Length - 4);
            var stream = new ZLibStream(new MemoryStream(decoded), ZLibMode.Decompress);

            return Encoding.Unicode.GetString(ReadToEnd(stream));

        }
    }
}
