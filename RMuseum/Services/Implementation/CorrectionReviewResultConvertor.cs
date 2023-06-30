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
                    return "تأیید می‌شود.";
                case CorrectionReviewResult.NotChanged:
                    return "تغییری نکرده.";
                case CorrectionReviewResult.RejectedBecauseWrong:
                    return "درست نیست.";
                case CorrectionReviewResult.RejectedBecauseVariant:
                    return "مربوط به نسخهٔ دیگری است.";
                case CorrectionReviewResult.RejectedBecauseUnnecessaryChange:
                    return "تغییر سلیقه‌ای یا بی دلیل است.";
                case CorrectionReviewResult.Rejected:
                    return "به دلیل دیگری رد می‌شود.";
                case CorrectionReviewResult.NotSuggestedByUser:
                    return "ایراد نرم‌افزار گنجور.";
                case CorrectionReviewResult.RejectedParaphraseBecauseInformal:
                    return "محاوره‌ای است.";
                case CorrectionReviewResult.RejectedParaphraseBecauseArtificial:
                    return "در آن از واژه‌های نامأنوس استفاده شده است.";
                case CorrectionReviewResult.RejectedParaphraseBecauseUnfathomable:
                    return "نامفهوم است.";
                case CorrectionReviewResult.RejectedParaphraseBecauseContainsOwnIdeas:
                    return "شامل تفسیرهای شخصی است.";
                case CorrectionReviewResult.RejectedParaphraseBecauseHasServeralTypoErrors:
                    return "غلطهای تایپی و املایی زیاد دارد.";
                case CorrectionReviewResult.RejectedParaphraseBecauseNotBetter:
                    return "از متن قبلی بهتر نیست.";
                case CorrectionReviewResult.RejectedParaphraseBecauseItIsNotAParaphrase:
                    return "از کادر معنی برای پیشنهاد تصحیح یا ارائهٔ توضیح تصحیحی استفاده شده است.";
                case CorrectionReviewResult.RejectedParaphraseBecauseItIsWordMeaningOrIncomplete:
                    return "معنی یا خلاصهٔ ارائه شده جزئی و ناقص یا در حد معنی کلمه است.";
            }
            return "هنوز بررسی نشده";
        }
    }
}
