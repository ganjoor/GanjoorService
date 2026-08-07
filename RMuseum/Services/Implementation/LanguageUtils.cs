using DNTPersianUtils.Core;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        public const string PersianAlphabet = "آابپتثجچحخدذرزژسشصضطظعغفقکگلمنوهیئؤء";

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
        public static string GetFirstNotMatchingCharacter(string input, string specfics, string additional = "")
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
                       .Replace("ٔ", "")//ye
                       .Replace(":", "")
                       .Replace("ئ", "ی")
                       .Replace("؛", "")
                       .Replace(";", "")
                       .Replace("*", "")
                       .Replace(")", "")
                       .Replace("(", "")
                       .Replace("[", "")
                       .Replace("]", "")
                       .Replace("\"", "")
                       .Replace("'", "")
                       .Replace("«", "")
                       .Replace("»", "")
                       .Replace("ْ", "")//sokoon
                       ;
        }

        public static string CleanTextForTransileration(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("‌", " ")//replace zwnj with space
                       .Replace("ّ", "")//tashdid
                       .Replace("َ", "")//a
                       .Replace("ِ", "")//e
                       .Replace("ُ", "")//o
                       .Replace("ً", "")//an
                       .Replace("ٍ", "")//en
                       .Replace("ٌ", "")//on
                       .Replace("ٔ", "")//ye
                       .Replace("ْ", "")//sokoon
                       ;
        }

        /// <summary>
        /// simple, best-effort Perso-Arabic -> Tajik Cyrillic letter substitution.
        /// NOT a linguistically correct transliteration: Perso-Arabic script does not
        /// write short vowels, so a letter-by-letter map cannot recover them (that's
        /// why a dedicated tool like persian-tajik.ir exists, and why the bulk of
        /// TajikGanjoor's content comes from real TajikPoems/TajikCats/TajikPoets rows
        /// rather than on-the-fly conversion). This exists only as a fallback for
        /// spots - like a neighbouring poem's excerpt on the next/previous nav - that
        /// don't have a proper Tajik translation in the database yet, so at least
        /// something in Cyrillic is shown instead of raw Perso-Arabic script.
        /// </summary>
        private static readonly Dictionary<char, string> _persianToTajikMap = new Dictionary<char, string>()
        {
            ['آ'] = "о",
            ['ا'] = "о",
            ['ب'] = "б",
            ['پ'] = "п",
            ['ت'] = "т",
            ['ث'] = "с",
            ['ج'] = "ҷ",
            ['چ'] = "ч",
            ['ح'] = "ҳ",
            ['خ'] = "х",
            ['د'] = "д",
            ['ذ'] = "з",
            ['ر'] = "р",
            ['ز'] = "з",
            ['ژ'] = "ж",
            ['س'] = "с",
            ['ش'] = "ш",
            ['ص'] = "с",
            ['ض'] = "з",
            ['ط'] = "т",
            ['ظ'] = "з",
            ['ع'] = "ъ",
            ['غ'] = "ғ",
            ['ف'] = "ф",
            ['ق'] = "қ",
            ['ک'] = "к",
            ['ك'] = "к",
            ['گ'] = "г",
            ['ل'] = "л",
            ['م'] = "м",
            ['ن'] = "н",
            ['و'] = "в",
            ['ه'] = "ҳ",
            ['ی'] = "ӣ",
            ['ي'] = "ӣ",
            ['ئ'] = "ъ",
            ['ؤ'] = "в",
            ['ء'] = "ъ",
            ['ة'] = "ат",
            ['؟'] = "?",
            ['،'] = ",",
            ['؛'] = ";",
            ['۰'] = "0",
            ['۱'] = "1",
            ['۲'] = "2",
            ['۳'] = "3",
            ['۴'] = "4",
            ['۵'] = "5",
            ['۶'] = "6",
            ['۷'] = "7",
            ['۸'] = "8",
            ['۹'] = "9",
        };

        public static string TransliteratePersianToTajik(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = CleanTextForTransileration(text);
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                sb.Append(_persianToTajikMap.TryGetValue(c, out string mapped) ? mapped : c.ToString());
            }
            return sb.ToString();
        }


        public static string SameSoundLetters(string text)
        {
            return text.Replace("آ", "ا").Replace("ث", "س").Replace("ح", "ه").Replace("ذ", "ز").Replace("ص", "س").Replace("ض", "ز").Replace("ط", "ت").Replace("ظ", "ز").Replace("غ", "ق");
        }

        private static string PrepareTextForFindingRhyme(string text)
        {
            return MakeTextSearchable(text)
                    .Replace("لله", "للاه")
                    .Replace("آ", "ا")
                    .Replace("‍", "")
                    .Replace("‏", "")
                    .Replace("‌", "")
                    .Replace(" ", "")
                    .Trim();
        }


        /// <summary>
        /// find rhyme
        /// </summary>
        /// <param name="verses"></param>
        /// <param name="secondPhase"></param>
        /// <param name="bandCouplets"></param>
        /// <param name="tryWholeVerses"></param>
        /// <returns></returns>
        public static GanjooRhymeAnalysisResult FindRhyme(List<GanjoorVerse> verses, bool secondPhase = false, bool bandCouplets = false, bool tryWholeVerses = false)
        {
            try
            {
                List<string> verseTextList = verses.Count == 2 || tryWholeVerses ? verses.Select(v => v.Text).ToList()
                                                           : bandCouplets ?
                                                           verses.Where(v => v.VersePosition == VersePosition.CenteredVerse2).Select(v => v.Text).ToList()
                                                           : verses.Where(v => v.VersePosition == VersePosition.Left).Select(v => v.Text).ToList();
                if (verseTextList.Count > 1)
                {
                    string rhyme = PrepareTextForFindingRhyme(verseTextList[0]);
                    if (string.IsNullOrEmpty(rhyme))
                    {
                        return new GanjooRhymeAnalysisResult()
                        {
                            Rhyme = "",
                            FailVerse = verseTextList[0]
                        };
                    }
                    if (secondPhase)
                    {
                        if (rhyme.Length > 0 && rhyme[rhyme.Length - 1] == 'ی')
                            rhyme = rhyme.Remove(rhyme.Length - 1);
                    }

                    for (int j = 1; j < verseTextList.Count; j++)
                    {
                        string verseText = PrepareTextForFindingRhyme(verseTextList[j]);
                        if (secondPhase)
                        {
                            if (verseText.Length > 0 && verseText[verseText.Length - 1] == 'ی')
                            {
                                verseText = verseText.Remove(verseText.Length - 1);
                            }
                        }
                        string oldRhyme = rhyme;
                        rhyme = "";
                        int i = oldRhyme.Length - 1;
                        while (
                            (oldRhyme[i] == verseText[verseText.Length - oldRhyme.Length + i])
                            ||
                            (
                            (oldRhyme[i] == 'ذ')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'د')
                            )
                            ||
                            (
                            (oldRhyme[i] == 'د')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'ذ')
                            )
                            ||

                            (
                            (oldRhyme[i] == 'ی')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'ا')
                            )
                            ||
                            (
                            (oldRhyme[i] == 'ا')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'ی')
                            )

                            ||

                            (oldRhyme[i] == verseText[verseText.Length - oldRhyme.Length + i])
                            ||
                            (
                            (oldRhyme[i] == 'پ')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'ب')
                            )
                            ||
                            (
                            (oldRhyme[i] == 'ب')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'پ')
                            )

                            ||
                            (oldRhyme[i] == 'ة')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'ت')

                            ||
                            (oldRhyme[i] == 'ت')
                            &&
                            (verseText[verseText.Length - oldRhyme.Length + i] == 'ة')



                            )
                        {
                            rhyme = oldRhyme[i] + rhyme;
                            i--;
                            if (i == -1)
                                break;
                        }
                        if (rhyme.Length == 0)
                        {
                            if (verses.Count == 2 && verseTextList.Count == 2)
                            {
                                rhyme = PrepareTextForFindingRhyme(verseTextList[1]);
                                if(rhyme.Length > 50)
                                {
                                    rhyme = "";
                                }
                                return new GanjooRhymeAnalysisResult()
                                {
                                    Rhyme = rhyme, //rhyme.Length > 50 condition check removed to search using prosody meters
                                    FailVerse = "",
                                };
                            }
                            return new GanjooRhymeAnalysisResult()
                            {
                                Rhyme = "",
                                FailVerse = verseText
                            };
                        }

                    }

                    if (!secondPhase)
                    {
                        if (string.IsNullOrEmpty(rhyme))
                        {
                            return FindRhyme(verses, true);
                        }
                    }

                    ////rhyme.Length > 50 condition check removed to search using prosody meters
                    /*if (rhyme.Length > 50)
                    {
                        return new GanjooRhymeAnalysisResult()
                        {
                            Rhyme = "",
                            FailVerse = "",
                        };
                    }*/

                    if (rhyme.Length > 50)
                    {
                        rhyme = "";
                    }

                    return new GanjooRhymeAnalysisResult()
                    {
                        Rhyme = rhyme,
                        FailVerse = ""
                    };

                }
            }
            catch
            {

            }

            return new GanjooRhymeAnalysisResult() { Rhyme = "", FailVerse = "" };
        }


        /// <summary>
        /// format money
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>

        public static string FormatMoney(decimal amount)
        {
            return amount.ToString("N0", new CultureInfo("fa-IR")).ToPersianNumbers();
        }

        /// <summary>
        /// format datetime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string FormatDate(DateTime dateTime)
        {
            return $"{dateTime.ToPersianYearMonthDay().Day.ToPersianNumbers()}م {PersianCulture.GetPersianMonthName(dateTime.ToPersianYearMonthDay().Month)} {dateTime.ToPersianYearMonthDay().Year.ToPersianNumbers()}";
        }

        /// <summary>
        /// short date
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string FormatInShortDate(DateTime dateTime)
        {
            return dateTime.Year < 623 ? "" : $"{dateTime.ToPersianYearMonthDay().Year.ToPersianNumbers()}/{dateTime.ToPersianYearMonthDay().Month.ToString().PadLeft(2, '0').ToPersianNumbers()}/{dateTime.ToPersianYearMonthDay().Day.ToString().PadLeft(2, '0').ToPersianNumbers()}";
        }

    }
}
