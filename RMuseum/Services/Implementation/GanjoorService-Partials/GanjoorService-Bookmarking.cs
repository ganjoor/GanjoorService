using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RSecurityBackend.Services.Implementation;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// Switch Bookmark for couplet
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> SwitchCoupletBookmark(Guid userId, int poemId, int coupletIndex)
        {
            var bookmark = await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.CoupletIndex == coupletIndex).SingleOrDefaultAsync();
            if (bookmark != null)
            {
                var res = await DeleteGanjoorBookmark(bookmark.Id, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return new RServiceResult<GanjoorUserBookmark>(null, res.ExceptionString);
                bookmark = null;
            }
            else
            {
                if(coupletIndex < 0)//bookmark a comment
                {
                    var res = await BookmarkComment(poemId, coupletIndex, userId);
                    if (!string.IsNullOrEmpty(res.ExceptionString))
                        return res;
                    bookmark = res.Result;
                }
                else
                {
                    var res = await BookmarkVerse(poemId, coupletIndex, userId);
                    if (!string.IsNullOrEmpty(res.ExceptionString))
                        return res;
                    bookmark = res.Result;
                }
            }
            return new RServiceResult<GanjoorUserBookmark>(bookmark);
        }

        /// <summary>
        /// Bookmark couplet if it is not
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> BookmarkCoupletIfNotBookmarked(Guid userId, int poemId, int coupletIndex)
        {
            var bookmark = await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.CoupletIndex == coupletIndex).SingleOrDefaultAsync();
            if (bookmark == null)
            { 
                var res = await BookmarkVerse(poemId, coupletIndex, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return res;
                bookmark = res.Result;
            }
            return new RServiceResult<GanjoorUserBookmark>(bookmark);
        }


        /// <summary>
        /// Bookmark Verse
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> BookmarkVerse(int poemId, int coupletIndex, Guid userId)
        {
            if ((await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.CoupletIndex == coupletIndex).SingleOrDefaultAsync()) != null)
            {
                return new RServiceResult<GanjoorUserBookmark>(null, "The couplet is already bookmarkeded.");
            }

            GanjoorUserBookmark bookmark =
                new GanjoorUserBookmark()
                {
                    UserId = userId,
                    PoemId = poemId,
                    DateTime = DateTime.Now,
                    CoupletIndex = coupletIndex
                };

            _context.GanjoorUserBookmarks.Add(bookmark);
            await _context.SaveChangesAsync();

            return new RServiceResult<GanjoorUserBookmark>(bookmark);
        }

        /// <summary>
        /// bookmark comment
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="negativeCommentId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> BookmarkComment(int poemId, int negativeCommentId, Guid userId)
        {
            if ((await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.CoupletIndex == negativeCommentId).SingleOrDefaultAsync()) != null)
            {
                return new RServiceResult<GanjoorUserBookmark>(null, "The comment is already bookmarkeded.");
            }

            int commentId = -negativeCommentId;
            var comment = await _context.GanjoorComments.AsNoTracking().Where(c => c.Id == commentId).SingleOrDefaultAsync();
            if(comment == null)
            {
                return new RServiceResult<GanjoorUserBookmark>(null, "The comment is deleted.");
            }

            GanjoorUserBookmark bookmark =
                new GanjoorUserBookmark()
                {
                    UserId = userId,
                    PoemId = poemId,
                    DateTime = DateTime.Now,
                    CoupletIndex = negativeCommentId,
                    PrivateNote = comment.HtmlComment,
                };

            _context.GanjoorUserBookmarks.Add(bookmark);
            await _context.SaveChangesAsync();

            return new RServiceResult<GanjoorUserBookmark>(bookmark);
        }

        /// <summary>
        /// get user ganjoor bookmarks (only Id, CoupletIndex and DateTime are valid)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmarkViewModel[]>> GetPoemUserBookmarks(Guid userId, int poemId)
        {
            GanjoorUserBookmarkViewModel[] bookmarks = 
                await _context.GanjoorUserBookmarks
                .Where(b => b.PoemId == poemId && b.UserId == userId)
                .Select(b => 
                    new GanjoorUserBookmarkViewModel()
                    {
                        Id = b.Id,
                        CoupletIndex = b.CoupletIndex,
                        DateTime = b.DateTime,
                        PrivateNote = b.PrivateNote
                    }
                )
                .ToArrayAsync();
            return new RServiceResult<GanjoorUserBookmarkViewModel[]>(bookmarks);
        }

        /// <summary>
        /// get verse bookmarks
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> IsCoupletBookmarked(Guid userId, int poemId, int coupletIndex)
        {
            return new RServiceResult<bool>(await _context.GanjoorUserBookmarks.Where(b => b.PoemId == poemId && b.CoupletIndex == coupletIndex && b.UserId == userId).AnyAsync());
        }


        /// <summary>
        /// delete user bookmark         
        /// /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="userId">to make sure a user can not delete another user's bookmarks</param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteGanjoorBookmark(Guid bookmarkId, Guid userId)
        {
            GanjoorUserBookmark bookmark = await _context.GanjoorUserBookmarks.Where(b => b.Id == bookmarkId && b.UserId == userId).SingleOrDefaultAsync();
            if (bookmark == null)
            {
                return new RServiceResult<bool>(false, "bookmark not found");
            }
            _context.GanjoorUserBookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// modify bookmark private note
        /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="userId">to make sure a user can not modify another user's bookmarks</param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifyBookmarkPrivateNoteAsync(Guid bookmarkId, Guid userId, string note)
        {
            try
            {
                GanjoorUserBookmark bookmark = await _context.GanjoorUserBookmarks.Where(b => b.Id == bookmarkId && b.UserId == userId).SingleOrDefaultAsync();
                if (bookmark == null)
                {
                    return new RServiceResult<bool>(false, "bookmark not found");
                }
                bookmark.PrivateNote = note;
                _context.Update(bookmark);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            
        }

        /// <summary>
        /// get user bookmarks (artifacts and items)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] Bookmarks)>> GetUserBookmarks(PagingParameterModel paging, Guid userId, string q)
        {
            var source =
                 _context.GanjoorUserBookmarks
                 .Include(b => b.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet)
                 .Where(b => b.UserId == userId && (string.IsNullOrEmpty(q) || (!string.IsNullOrEmpty(q) && b.PrivateNote.Contains(q))))
                .OrderByDescending(b => b.DateTime)
                .AsQueryable();

            (PaginationMetadata PagingMeta, GanjoorUserBookmark[] Bookmarks) bookmarksPage =
                await QueryablePaginator<GanjoorUserBookmark>.Paginate(source, paging);


            List<GanjoorUserBookmarkViewModel> result = new List<GanjoorUserBookmarkViewModel>();
            foreach (var bookmark in bookmarksPage.Bookmarks)
            {
                var verses = await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == bookmark.PoemId && v.CoupletIndex == bookmark.CoupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                result.Add
                    (
                    new GanjoorUserBookmarkViewModel()
                    {
                        Id = bookmark.Id,
                        PoetName = bookmark.Poem.Cat.Poet.Nickname,
                        PoetImageUrl = $"{WebServiceUrl.Url}{$"/api/ganjoor/poet/image/{bookmark.Poem.FullUrl.Substring(1, bookmark.Poem.FullUrl.IndexOf('/', 1) - 1)}.gif"}",
                        PoemFullTitle = bookmark.Poem.FullTitle,
                        PoemFullUrl = bookmark.Poem.FullUrl,
                        CoupletIndex = bookmark.CoupletIndex,
                        VerseText = verses.Count == 0 ? "" : verses[0].Text,
                        Verse2Text = verses.Count < 2 ? "" : verses[1].Text,
                        DateTime = bookmark.DateTime,
                        PrivateNote = bookmark.PrivateNote
                    }
                    );
            }


            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] Bookmarks)>
                ((bookmarksPage.PagingMeta, result.ToArray()));
        }
    }
}