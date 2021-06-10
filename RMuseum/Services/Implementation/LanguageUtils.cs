namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// Language Utilitis
    /// </summary>
    public static class LanguageUtils
    {
        /// <summary>
        /// Persian Alphabet
        /// </summary>
        public const string PersianAlphabet = "آابپتثجچحخدذرزژسشصضطظعغفقکگلمنوهیئؤ";

        /// <summary>
        /// English Alphabet
        /// </summary>
        public const string EnglishLowerCaseAlphabet = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Check to see if input Contains Only Specific Characters
        /// </summary>
        /// <param name="input"></param>
        /// <param name="specfics"></param>
        /// <param name="additional"></param>
        /// <returns></returns>
        public static string GetFirstNotMatchingCharacter(string input, string specfics, string additional= "")
        {
            string all = specfics + additional;
            foreach (char c in input)
                if (all.IndexOf(c) == -1)
                    return (c + "");
            return "";
        }

        /// <summary>
        /// make text searchable
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MakeTextSearchable(string text)
        {
            return text.Replace("‌", " ")//replace zwnj with space
                       .Replace("ّ", "")//tashdid
                       .Replace("َ", "")//a
                       .Replace("ِ", "")//e
                       .Replace("ُ", "")//o
                       .Replace("ً", "")//an
                       .Replace("ٍ", "")//en
                       .Replace("ٌ", "")//on
                       .Replace(".", "")//dot
                       .Replace("،", "")//virgool
                       .Replace("!", "")
                       .Replace("؟", "")
                       .Replace("ٔ", "")
                       .Replace(":", "")
                       ;
        }
    }
}
