namespace SecondMonitor.DataModel.Extensions
{
    using System;
    using System.Text.RegularExpressions;

    public static partial class StringExtensions
    {
        private static readonly Regex invalidCharsRegex = MyRegex();

        public static string FromArray(this byte[] buffer)
        {
            return FromArray(buffer, 0);
        }

        public static string FromArray(this byte[] buffer, int startIndex)
        {
            if (buffer == null || buffer[startIndex] == (char)0)
            {
                return string.Empty;
            }

            return System.Text.Encoding.UTF8.GetString(buffer, startIndex, buffer.Length - startIndex).Split(new[] { (char)0 }, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        public static string FromArray(char[] buffer)
        {
            if (buffer[0] == (char)0)
            {
                return string.Empty;
            }

            return new string(buffer);
        }

        public static string CleanInvalidXmlChars(this string text)
        {
            // From xml spec valid chars:
            // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
            // any Unicode character, excluding the surrogate blocks, FFFE, and FFFF.
            return invalidCharsRegex.Replace(text, string.Empty);
        }

        [GeneratedRegex(@"[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]")]
        private static partial Regex MyRegex();
    }
}