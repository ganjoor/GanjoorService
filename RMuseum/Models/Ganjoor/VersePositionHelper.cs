using System.Linq;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Verse Position strings
    /// </summary>
    public class VersePositionHelper
    {
        /// <summary>
        /// Verse Position
        /// </summary>
        public VersePosition VersePosition { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// default verse positions
        /// </summary>
        public static VersePositionHelper[] VersePositions
        {
            get
            {
                return new VersePositionHelper[] 
                { 
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.Right,
                        Text = "مصرع اول"
                    },
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.Left,
                        Text = "مصرع دوم"
                    },
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.CenteredVerse1,
                        Text =  "مصرع اول بند"
                    },
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.CenteredVerse2,
                        Text = "مصرع دوم بند"
                    },
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.Paragraph,
                        Text = "پاراگراف نثر"
                    },
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.Single,
                        Text = "نیمایی یا آزاد"
                    },
                    new VersePositionHelper()
                    {
                        VersePosition = VersePosition.Comment,
                        Text = "توضیحات تکمیلی"
                    }
                };
            }
        }

        /// <summary>
        /// get verse position text
        /// </summary>
        /// <param name="versePosition"></param>
        /// <returns></returns>
        public static string GetVersePositionString(VersePosition versePosition)
        {
            var pos = VersePositions.Where(v => v.VersePosition == versePosition).SingleOrDefault();
            if (pos == null)
                return "نامعتبر";
            return pos.Text;
        }
    }
}
