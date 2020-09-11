using System.Collections.Generic;

namespace RMuseum.Services.Implementation.ImportedFromDesktopGanjoor
{
    /// <summary>
    /// Persian specific text utility class
    /// </summary>
    public class GPersianTextSync
    {
        /// <summary>
        /// convert arabic characters to persian equivalents
        /// </summary>
        /// <param name="inputStr"></param>
        /// <returns></returns>
        public static string Sync(string inputStr)
        {
            return
                inputStr
                    .Replace('ك', 'ک')
                    .Replace('ي', 'ی')
                    .Replace("ۀ", "هٔ")
                    .Replace("ه‌ی", "هٔ")
                    .Replace("0", "۰")
                    .Replace("1", "۱")
                    .Replace("2", "۲")
                    .Replace("3", "۳")
                    .Replace("4", "۴")
                    .Replace("5", "۵")
                    .Replace("6", "۶")
                    .Replace("7", "۷")
                    .Replace("8", "۸")
                    .Replace("9", "۹");
        }

        /// <summary>
        /// list of persian letters
        /// </summary>
        public static string PersianLetters
        {
            get
            {
                return "اآئأإءبپتثجچحخدذرزژسشصضطظعغفقکگلمنوهی";
            }
        }

        /// <summary>
        /// convert persian text to simple pinglish
        /// </summary>
        /// <param name="inputStr"></param>
        /// <returns></returns>
        public static string Farglisize(string inputStr)
        {
            string outStr = "";
            string s;
            foreach (char c in inputStr)
                if (_PinglishDic.TryGetValue(c, out s))
                    outStr += s;
            return outStr;
        }

        /// <summary>
        /// equaivalnet for persian characters
        /// </summary>
        private static Dictionary<char, string> _PinglishDic = new Dictionary<char, string>()
        {
            {'ا', "a"},
            {'آ', "a"},
            {'ب', "b"},
            {'پ', "p"},
            {'ت', "t"},
            {'ث', "s"},
            {'ج', "j"},
            {'چ', "ch"},
            {'ح', "h"},
            {'خ', "kh"},
            {'د', "d"},
            {'ذ', "z"},
            {'ر', "r"},
            {'ز', "z"},
            {'ژ', "zh"},
            {'س', "s"},
            {'ش', "sh"},
            {'ص', "s"},
            {'ض', "z"},
            {'ط', "t"},
            {'ظ', "z"},
            {'ع', "E"},
            {'غ', "gh"},
            {'ف', "f"},
            {'ق', "gh"},
            {'ک', "k"},
            {'گ', "g"},
            {'ل', "l"},
            {'م', "m"},
            {'ن', "n"},
            {'ه', "h"},
            {'و', "v"},
            {'ی', "i"},
            {'ئ', "E"},
            {'أ', "A"},
            {'إ', "E"},
            {' ', "-"},
        };
    }
}
