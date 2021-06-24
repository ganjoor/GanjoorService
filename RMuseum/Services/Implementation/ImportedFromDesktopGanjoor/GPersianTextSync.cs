using System.Collections.Generic;

namespace RMuseum.Services.Implementation.ImportedFromDesktopGanjoor
{
    /// <summary>
    /// Persian specific text utility class
    /// </summary>
    public class GPersianTextSync
    {
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

        public static string UniquelyFarglisize(string inputStr)
        {
            string outStr = "";
            string s;
            foreach (char c in inputStr)
                if (_UniqueAlphaNames.TryGetValue(c, out s))
                    outStr += s;
            return outStr;
        }


        private static Dictionary<char, string> _UniqueAlphaNames = new Dictionary<char, string>()
        {
            {'ا', "a"},
            {'آ', "a"},
            {'ب', "b"},
            {'پ', "p"},
            {'ت', "t"},
            {'ث', "th"},
            {'ج', "j"},
            {'چ', "ch"},
            {'ح', "hh"},
            {'خ', "kh"},
            {'د', "d"},
            {'ذ', "the"},
            {'ر', "r"},
            {'ز', "z"},
            {'س', "si"},
            {'ش', "sh"},
            {'ط', "tt"},
            {'ظ', "zz"},
            {'ص', "ss"},
            {'ض', "zzz"},
            {'ع', "e"},
            {'غ', "gh2"},
            {'ف', "f"},
            {'ق', "gh"},
            {'ک', "k"},
            {'گ', "g"},
            {'ل', "l"},
            {'م', "m"},
            {'ن', "n"},
            {'ه', "h"},
            {'و', "v"},
            {'ی', "y"},
        };
    }
}
