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
        Rejected = 6,
        NotSuggestedByUser = 7,
        RejectedParaphraseBecauseInformal = 8,
        RejectedParaphraseBecauseArtificial = 9,
        RejectedParaphraseBecauseUnfathomable = 10,
        RejectedParaphraseBecauseContainsOwnIdeas = 11,
        RejectedParaphraseBecauseHasServeralTypoErrors = 12,
        RejectedParaphraseBecauseNotBetter = 13,
        RejectedParaphraseBecauseItIsNotAParaphrase = 14,
        RejectedParaphraseBecauseItIsWordMeaningOrIncomplete = 15,
    }
}
