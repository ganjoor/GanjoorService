namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// a single user's rating value for a single comment
    /// (used to merge "my rating" state into an already fetched, anonymously cacheable comment list)
    /// </summary>
    public class GanjoorCommentUserRatingViewModel
    {
        /// <summary>
        /// GanjoorComment Id
        /// </summary>
        public int CommentId { get; set; }

        /// <summary>
        /// +1 = Like, -1 = Dislike
        /// </summary>
        public short Value { get; set; }
    }

    /// <summary>
    /// result of rating/unrating a comment, returned so the client can update counts in place
    /// without re-fetching the whole comment list
    /// </summary>
    public class GanjoorCommentRatingResultViewModel
    {
        /// <summary>
        /// comment id
        /// </summary>
        public int CommentId { get; set; }

        /// <summary>
        /// updated total likes
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// updated total dislikes
        /// </summary>
        public int DislikeCount { get; set; }

        /// <summary>
        /// the rating value the requesting user now has for this comment (0 if cleared)
        /// </summary>
        public short CurrentUserRatingValue { get; set; }
    }
}
