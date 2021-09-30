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
            (int Verse1, int? Verse2)? verse12 = await _GetVerse12IdFromCoupletIndex(poemId, coupletIndex);
            if (verse12 == null)
                return new RServiceResult<GanjoorUserBookmark>(null, "verse not found");
            var bookmark = await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.VerseId == verse12.Value.Verse1).SingleOrDefaultAsync();
            if (bookmark != null)
            {
                var res = await DeleteGanjoorBookmark(bookmark.Id, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return new RServiceResult<GanjoorUserBookmark>(null, res.ExceptionString);
                bookmark = null;

            }
            else
            {
                var res = await BookmarkVerse(poemId, coupletIndex, verse12.Value.Verse1, verse12.Value.Verse2, userId);
                if(!string.IsNullOrEmpty(res.ExceptionString))
                    return res;
                bookmark = res.Result;
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
            (int Verse1, int? Verse2)? verse12 = await _GetVerse12IdFromCoupletIndex(poemId, coupletIndex);
            if (verse12 == null)
                return new RServiceResult<GanjoorUserBookmark>(null, "verse not found");
            var bookmark = await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.VerseId == verse12.Value.Verse1).SingleOrDefaultAsync();
            if (bookmark == null)
            { 
                var res = await BookmarkVerse(poemId, coupletIndex, verse12.Value.Verse1, verse12.Value.Verse2, userId);
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
        /// <param name="verseId"></param>
        /// <param name="verse2Id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> BookmarkVerse(int poemId, int coupletIndex, int verseId, int? verse2Id, Guid userId)
        {
            if ((await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.VerseId == verseId).SingleOrDefaultAsync()) != null)
            {
                return new RServiceResult<GanjoorUserBookmark>(null, "Verse is already bookmarkeded.");
            }

            GanjoorUserBookmark bookmark =
                new GanjoorUserBookmark()
                {
                    UserId = userId,
                    PoemId = poemId,
                    VerseId = verseId,
                    Verse2Id = verse2Id,
                    DateTime = DateTime.Now,
                    CoupletIndex = coupletIndex
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
            int? verseId = await _GetVerseIdFromCoupletIndex(poemId, coupletIndex);
            if (verseId == null)
                return new RServiceResult<bool>(false, "verse not found");
            return new RServiceResult<bool>(await _context.GanjoorUserBookmarks.Where(b => b.PoemId == poemId && b.VerseId == verseId && b.UserId == userId).AnyAsync());
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
        /// get user bookmarks (artifacts and items)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] Bookmarks)>> GetUserBookmarks(PagingParameterModel paging, Guid userId)
        {
            var source =
                 _context.GanjoorUserBookmarks
                 .Include(b => b.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet)
                 .Include(b => b.Verse)
                 .Include(b => b.Verse2)
                 .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.DateTime)
                .AsQueryable();

            (PaginationMetadata PagingMeta, GanjoorUserBookmark[] Bookmarks) bookmarksPage =
                await QueryablePaginator<GanjoorUserBookmark>.Paginate(source, paging);


            List<GanjoorUserBookmarkViewModel> result = new List<GanjoorUserBookmarkViewModel>();
            foreach (var bookmark in bookmarksPage.Bookmarks)
            {
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
                        VerseText = bookmark.Verse.Text,
                        Verse2Text = bookmark.Verse2 == null ? "" : bookmark.Verse2.Text,
                        DateTime = bookmark.DateTime
                    }
                    );
            }


            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] Bookmarks)>
                ((bookmarksPage.PagingMeta, result.ToArray()));
        }
    }
}