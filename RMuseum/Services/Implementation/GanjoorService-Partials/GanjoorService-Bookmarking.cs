using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RMuseum.Models.Artifact;
using RSecurityBackend.Services.Implementation;
using RMuseum.Models.Bookmark;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// Bookmark Verse
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="vOrder"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> SwitchBookmarkVerse(int poemId, int vOrder, Guid userId, RBookmarkType type)
        {
            var verse = await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder == vOrder).SingleAsync();
            var bookmark = await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.VerseId == verse.Id && b.RBookmarkType == type).SingleOrDefaultAsync();
            if (bookmark != null)
            {
                var res = await DeleteGanjoorBookmark(bookmark.Id, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return new RServiceResult<GanjoorUserBookmark>(null, res.ExceptionString);
                bookmark = null;

            }
            else
            {
                var res = await BookmarkVerse(poemId, vOrder, userId, type);
                if(!string.IsNullOrEmpty(res.ExceptionString))
                    return res;
                bookmark = res.Result;
            }
            return new RServiceResult<GanjoorUserBookmark>(bookmark);
        }


        /// <summary>
        /// Bookmark Verse
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="vOrder"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark>> BookmarkVerse(int poemId, int vOrder, Guid userId, RBookmarkType type)
        {
            var verse = await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder == vOrder).SingleAsync();
            if ((await _context.GanjoorUserBookmarks.Where(b => b.UserId == userId && b.PoemId == poemId && b.VerseId == verse.Id && b.RBookmarkType == type).SingleOrDefaultAsync()) != null)
            {
                return new RServiceResult<GanjoorUserBookmark>(null, "Artifact is already bookmarked/faved.");
            }

            GanjoorUserBookmark bookmark =
                new GanjoorUserBookmark()
                {
                    UserId = userId,
                    PoemId = poemId,
                    VerseId = verse.Id,
                    RBookmarkType = type,
                    DateTime = DateTime.Now,
                    Note = ""
                };

            _context.GanjoorUserBookmarks.Add(bookmark);
            await _context.SaveChangesAsync();

            return new RServiceResult<GanjoorUserBookmark>(bookmark);
        }

        /// <summary>
        /// get user ganjoor bookmarks
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark[]>> GetPoemGanjoorUserBookmarks(int poemId, Guid userId, RBookmarkType type)
        { 
            GanjoorUserBookmark[] bookmarks = await _context.GanjoorUserBookmarks.Where(b => b.PoemId == poemId && b.UserId == userId && b.RBookmarkType == type).ToArrayAsync();
            return new RServiceResult<GanjoorUserBookmark[]>(bookmarks);
        }

        /// <summary>
        /// get verse bookmarks
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="vOrder"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorUserBookmark[]>> GetVerseGanjoorUserBookmarks(int poemId, int vOrder, Guid userId)
        {
            var verse = await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder == vOrder).SingleAsync();
            GanjoorUserBookmark[] bookmarks = await _context.GanjoorUserBookmarks.Where(b => b.PoemId == poemId && b.VerseId == verse.Id && b.UserId == userId).ToArrayAsync();
            return new RServiceResult<GanjoorUserBookmark[]>(bookmarks);
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
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmark[] Bookmarks)>> GetUserBookmarks(PagingParameterModel paging, Guid userId, RBookmarkType type)
        {
            var source =
                 _context.GanjoorUserBookmarks
                 .Include(b => b.Poem)
                 .Include(b => b.Verse)
                 .Where(b => b.UserId == userId && b.RBookmarkType == type)
                .OrderByDescending(b => b.DateTime)
                .AsQueryable();

            (PaginationMetadata PagingMeta, GanjoorUserBookmark[] Bookmarks) paginatedResult =
                await QueryablePaginator<GanjoorUserBookmark>.Paginate(source, paging);



            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmark[] Bookmarks)>(paginatedResult);
        }
    }
}