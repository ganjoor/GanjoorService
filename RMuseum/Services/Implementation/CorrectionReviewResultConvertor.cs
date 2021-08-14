using RMuseum.Models.Ganjoor;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// تبدیلگر CorrectionReviewResult
    /// </summary>
    public static class CorrectionReviewResultConvertor
    {
        /// <summary>
        /// تبدیل به رشته
        /// </summary>
        /// <param name="reviewResult"></param>
        /// <returns></returns>
        public static string GetString(CorrectionReviewResult reviewResult)
        {
            switch(reviewResult)
            {
                case CorrectionReviewResult.Approved:
                    return "تأیید می‌شود";
                case CorrectionReviewResult.NotChanged:
                    return "تغییری نکرده";
                case CorrectionReviewResult.RejectedBecauseWrong:
                    return "درست نیست";
                case CorrectionReviewResult.RejectedBecauseVariant:
                    return "مربوط به نسخهٔ دیگری است";
                case CorrectionReviewResult.RejectedBecauseUnnecessaryChange:
                    return "تغییر سلیقه‌ای یا بی دلیل است";
                case CorrectionReviewResult.Rejected:
                    return "به دلیل دیگری رد می‌شود";
            }
            return "بررسی نشده";
        }
    }
}
