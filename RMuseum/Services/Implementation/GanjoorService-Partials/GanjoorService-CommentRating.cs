using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <param name="value">+1: like, -1: dislike, 0: remove previous rating</param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RateCommentAsync(Guid userId, int commentId, short value)
        {
            if(value != 1 && value != -1 && value != 0)
            {
                return new RServiceResult<bool>(false, "value != 1 && value != -1 && value != 0");
            }
            try
            {
                var comment = await _context.GanjoorComments.Where(c => c.Id == commentId).SingleOrDefaultAsync();
                if (comment == null)
                {
                    return new RServiceResult<bool>(false, "comment == null");
                }

                var oldRating = await _context.GanjoorCommentReactions.Where(c => c.GanjoorCommentId == commentId && c.UserId == userId).SingleOrDefaultAsync();
                if(value == 0 && oldRating == null)
                {
                    return new RServiceResult<bool>(true);
                }
                if (oldRating != null)
                {
                    if (oldRating.Value == value)
                    {
                        return new RServiceResult<bool>(true);
                    }
                    if(value == 0)
                    {
                        _context.Remove(oldRating);
                    }
                    else
                    {
                        oldRating.Value = value;
                        oldRating.ReactionDate = DateTime.Now;
                        _context.Update(oldRating);
                    }
                    await _context.SaveChangesAsync();

                    return await ReCalculateCommentSortKeyAsync(comment);
                }
                else 
                {
                    var rating = new GanjoorCommentReaction()
                    {
                        GanjoorCommentId = commentId,
                        PoemId = comment.PoemId,
                        UserId = userId,
                        Value = value,
                        ReactionDate = DateTime.Now,
                    };
                    _context.Add(rating);
                    await _context.SaveChangesAsync();
                    return await ReCalculateCommentSortKeyAsync(comment);
                }

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task<RServiceResult<bool>> ReCalculateCommentSortKeyAsync(GanjoorComment comment)
        {
            try
            {
                var likes = await _context.GanjoorCommentReactions.AsNoTracking().Where(r => r.GanjoorCommentId == comment.Id && r.Value == 1).SumAsync(r => r.Value);
                var dislikes = await _context.GanjoorCommentReactions.AsNoTracking().Where(r => r.GanjoorCommentId == comment.Id && r.Value == -1).SumAsync(r => r.Value);
                comment.LikeCount = likes;
                comment.DislikeCount = dislikes;
                comment.SortKey = GanjoorCommentRankingScoreCalculator.ComputeRankingScore(likes, dislikes);
                _context.Update(comment);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}
