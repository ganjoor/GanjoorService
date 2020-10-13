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
        /// Contains Only Specific Characters
        /// </summary>
        /// <param name="input"></param>
        /// <param name="specfics"></param>
        /// <param name="additional"></param>
        /// <returns></returns>
        public static bool ContainsOnlySpecificCharacters(string input, string specfics, string additional= "")
        {
            string all = specfics + additional;
            foreach (char c in input)
                if (all.IndexOf(c) == -1)
                    return false;
            return true;
        }
    }
}
