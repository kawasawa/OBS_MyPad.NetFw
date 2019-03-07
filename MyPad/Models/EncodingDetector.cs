using System;
using System.Text;
using Ude;

namespace MyPad.Models
{
    public static class EncodingDetector
    {
        public static Encoding Detect(byte[] bytes, int verifiedLength)
        {
            const int MIN_LENGTH = 256;

            // 空の場合は null
            if (bytes.Length == 0)
                return null;

            // BOM による判定
            var bom = DetectByBom(bytes);
            if (bom != null)
                return bom;

            // バイト配列の整形
            if (bytes.Length < MIN_LENGTH)
            {
                // バイト長が短すぎる場合は複製して補う
                var count = MIN_LENGTH / bytes.Length + 1;
                var buffer = new byte[count * bytes.Length];
                for (var i = 0; i < count; i++)
                    Array.Copy(bytes, 0, buffer, bytes.Length * i, bytes.Length);
                bytes = buffer;
            }
            else if (MIN_LENGTH <= verifiedLength && verifiedLength < bytes.Length)
            {
                // バイト長が指定されている場合は切り出す
                var buffer = new byte[verifiedLength];
                Array.Copy(bytes, 0, buffer, 0, verifiedLength);
                bytes = buffer;
            }

            // Mozilla Universal Charset Detector による判定
            var ude = new CharsetDetector();
            ude.Feed(bytes, 0, bytes.Length);
            ude.DataEnd();
            if (string.IsNullOrEmpty(ude.Charset) == false)
                return Encoding.GetEncoding(ude.Charset);

            // いずれによっても判定できない場合は null
            return null;
        }

        private static Encoding DetectByBom(byte[] bytes)
        {
            if (bytes.Length <= 1)
                return null;

            if (3 <= bytes.Length)
                if (bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
                    return new UTF8Encoding(true);             // UTF8

            Encoding result = null;
            if (2 <= bytes.Length)
            {
                if (bytes[0] == 0xfe && bytes[1] == 0xff)
                    result = new UnicodeEncoding(true, true);  // UTF16-BE
                if (bytes[0] == 0xff && bytes[1] == 0xfe)
                    result = new UnicodeEncoding(false, true); // UTF16-LE (UTF32-LE の可能性もある)
            }
            if (4 <= bytes.Length)
            {
                if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xfe && bytes[3] == 0xff)
                    result = new UTF32Encoding(true, true);    // UTF32-BE
                if (bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0x00 && bytes[3] == 0x00)
                    result = new UTF32Encoding(false, true);   // UTF32-LE
            }
            return result;
        }
    }
}
