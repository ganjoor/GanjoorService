namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// correction review result
    /// </summary>
    public enum CorrectionReviewResult
    {
        NotReviewed = 0,
        Approved = 1,
        NotChanged = 2,
        RejectedBecauseWrong = 3,
        RejectedBecauseVariant = 4,
        RejectedBecauseUnnecessaryChange = 5,
        Rejected = 6
    }
}
