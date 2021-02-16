using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RMuseum.Models.Artifact;
using System.Text.RegularExpressions;
using RSecurityBackend.Services.Implementation;
using DNTPersianUtils.Core;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        /// <summary>
        /// Get List of poets
        /// </summary>
        /// <param name="websitePoets"></param>
        /// <param name="includeBio"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetViewModel[]>> GetPoets(bool websitePoets, bool includeBio = true)
        {
            try
            {
                var res =
                     await
                     (from poet in _context.GanjoorPoets
                      join cat in _context.GanjoorCategories.Where(c => c.ParentId == null)
                      on poet.Id equals cat.PoetId
                      where !websitePoets || poet.Id < 200
                      orderby poet.Name descending
                      select new GanjoorPoetViewModel()
                      {
                          Id = poet.Id,
                          Name = poet.Name,
                          Description = includeBio ? poet.Description : null,
                          FullUrl = cat.FullUrl,
                          RootCatId = cat.Id
                      }
                      )
                     .ToListAsync();

                StringComparer fa = StringComparer.Create(new CultureInfo("fa-IR"), true);
                res.Sort((a, b) => fa.Compare(a.Name, b.Name));

                return new RServiceResult<GanjoorPoetViewModel[]>
                    (
                        res.ToArray()
                    ); ;
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get poet by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetById(int id)
        {
            try
            {
                var poet = await _context.GanjoorPoets.Where(p => p.Id == id).FirstOrDefaultAsync();
                if (poet == null)
                    return new RServiceResult<GanjoorPoetCompleteViewModel>(null);
                var cat = await _context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == id).FirstOrDefaultAsync();
                return await GetCatById(cat.Id);
            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get poet by url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetByUrl(string url)
        {
            try
            {
                // /hafez/ => /hafez :
                if(url.LastIndexOf('/') == url.Length - 1)
                {
                    url = url.Substring(0, url.Length - 1);
                }
                var cat = await _context.GanjoorCategories.Where(c => c.FullUrl == url && c.ParentId == null).SingleOrDefaultAsync();
                if (cat == null)
                    return new RServiceResult<GanjoorPoetCompleteViewModel>(null);
                return await GetCatById(cat.Id);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get cat by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="poems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatByUrl(string url, bool poems = true)
        {
            try
            {
                // /hafez/ => /hafez :
                if (url.LastIndexOf('/') == url.Length - 1)
                {
                    url = url.Substring(0, url.Length - 1);
                }
                var cat = await _context.GanjoorCategories.Where(c => c.FullUrl == url).SingleOrDefaultAsync();
                if (cat == null)
                    return new RServiceResult<GanjoorPoetCompleteViewModel>(null);
                return await GetCatById(cat.Id);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get cat by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="poems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatById(int id, bool poems = true)
        {
            try
            {
               
                var cat = await _context.GanjoorCategories.Include(c => c.Poet).Include(c => c.Parent).Where(c => c.Id == id).FirstOrDefaultAsync();
                if (cat == null)
                    return new RServiceResult<GanjoorPoetCompleteViewModel>(null);

                List<GanjoorCatViewModel> ancetors = new List<GanjoorCatViewModel>();

                var parent = cat.Parent;
                while (parent != null)
                {
                    ancetors.Insert(0, new GanjoorCatViewModel()
                    {
                        Id = parent.Id,
                        Title = parent.Title,
                        UrlSlug = parent.UrlSlug,
                        FullUrl = parent.FullUrl
                    });

                    parent = await _context.GanjoorCategories.Where(c => c.Id == parent.ParentId).FirstOrDefaultAsync();
                }


                int nextCatId =
                    await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id > id).AnyAsync() ?
                    await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id > id).MinAsync(c => c.Id)
                    :
                    0;
                var nextCat = nextCatId == 0 ? null : await _context
                                            .GanjoorCategories
                                            .Where(c => c.Id == nextCatId)
                                            .Select
                                            (
                                                c =>
                                                    new GanjoorCatViewModel() 
                                                    {
                                                        Id = c.Id,
                                                        Title = c.Title,
                                                        UrlSlug = c.UrlSlug,
                                                        FullUrl = c.FullUrl
                                                        //other fields null
                                                    }
                                            ).SingleOrDefaultAsync();

                int preCatId =
                     await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id < id).AnyAsync() ?
                    await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id < id).MaxAsync(c => c.Id)
                    :
                    0;
                var preCat = preCatId == 0 ? null : await _context
                                            .GanjoorCategories
                                            .Where(c => c.Id == preCatId)
                                            .Select
                                            (
                                                c =>
                                                    new GanjoorCatViewModel()
                                                    {
                                                        Id = c.Id,
                                                        Title = c.Title,
                                                        UrlSlug = c.UrlSlug,
                                                        FullUrl = c.FullUrl
                                                        //other fields null
                                                    }
                                            ).SingleOrDefaultAsync();

                GanjoorCatViewModel catViewModel = new GanjoorCatViewModel()
                {
                    Id = cat.Id,
                    Title = cat.Title,
                    UrlSlug = cat.UrlSlug,
                    FullUrl = cat.FullUrl,
                    Next = nextCat,
                    Previous = preCat,
                    Ancestors = ancetors,
                    Children = await _context.GanjoorCategories.Where(c => c.ParentId == cat.Id).OrderBy(cat => cat.Id).Select
                     (
                     c => new GanjoorCatViewModel()
                     {
                         Id = c.Id,
                         Title = c.Title,
                         UrlSlug = c.UrlSlug,
                         FullUrl = c.FullUrl
                     }
                     ).ToListAsync(),
                    Poems = poems ? await _context.GanjoorPoems.Where(p => p.CatId == cat.Id).OrderBy(p => p.Id).Select
                     (
                         p => new GanjoorPoemSummaryViewModel()
                         {
                             Id = p.Id,
                             Title = p.Title,
                             UrlSlug = p.UrlSlug,
                             Excerpt = _context.GanjoorVerses.Where(v => v.PoemId == p.Id && v.VOrder == 1).FirstOrDefault().Text
                         }
                     ).ToListAsync()
                     :
                     null
                };

                return new RServiceResult<GanjoorPoetCompleteViewModel>
                   (
                   new GanjoorPoetCompleteViewModel()
                   {
                       Poet = await _context.GanjoorPoets.Where(p => p.Id == cat.PoetId)
                                            .Select(p => new GanjoorPoetViewModel()
                                            {
                                                Id = p.Id,
                                                Name = p.Name,
                                                Description = p.Description,
                                                FullUrl = _context.GanjoorCategories.Where(c => c.PoetId == p.Id && c.ParentId == null).Single().FullUrl,
                                                RootCatId = _context.GanjoorCategories.Where(c => c.PoetId == p.Id && c.ParentId == null).Single().Id
                                            }) .FirstOrDefaultAsync(),
                       Cat = catViewModel
                   }
                   );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get page url by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> GetPageUrlById(int id)
        {
            try
            {
                var dbPage = await _context.GanjoorPages.Where(p => p.Id == id).SingleOrDefaultAsync();
                if (dbPage == null)
                    return new RServiceResult<string>(null); //not found
                return new RServiceResult<string>(dbPage.FullUrl);
            }
            catch(Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageCompleteViewModel>> GetPageByUrl(string url, bool catPoems = true)
        {
            try
            {
                if(url.IndexOf('?') != -1)
                {
                    url = url.Substring(0, url.IndexOf('?'));
                }

                // /hafez/ => /hafez :
                if (url.LastIndexOf('/') == url.Length - 1)
                {
                    url = url.Substring(0, url.Length - 1);
                }

                var dbPage = await _context.GanjoorPages.Where(p => p.FullUrl == url).SingleOrDefaultAsync();
                if (dbPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null); //not found
                var secondPoet = dbPage.SecondPoetId == null ? null :
                     await
                     (from poet in _context.GanjoorPoets
                      join cat in _context.GanjoorCategories.Where(c => c.ParentId == null)
                      on poet.Id equals cat.PoetId
                      where poet.Id == (int)dbPage.SecondPoetId
                      orderby poet.Name descending
                      select new GanjoorPoetViewModel()
                      {
                          Id = poet.Id,
                          Name = poet.Name,
                          FullUrl = cat.FullUrl,
                          RootCatId = cat.Id
                      }
                      )
                     .SingleAsync();
                GanjoorPageCompleteViewModel page = new GanjoorPageCompleteViewModel()
                {
                    Id = dbPage.Id,
                    GanjoorPageType = dbPage.GanjoorPageType,
                    Title = dbPage.Title,
                    FullTitle = dbPage.FullTitle,
                    UrlSlug = dbPage.UrlSlug,
                    FullUrl = dbPage.FullUrl,
                    HtmlText = dbPage.HtmlText,
                    SecondPoet = secondPoet

                };
                switch (page.GanjoorPageType)
                {
                    case GanjoorPageType.PoemPage:
                        {
                            var poemRes = await GetPoemById((int)dbPage.PoemId);
                            if(!string.IsNullOrEmpty(poemRes.ExceptionString))
                            {
                                return new RServiceResult<GanjoorPageCompleteViewModel>(null, poemRes.ExceptionString);
                            }
                            page.Poem = poemRes.Result;
                        }
                        break;
                    
                    case GanjoorPageType.CatPage:
                        {
                            var catRes = await GetCatById((int)dbPage.CatId);
                            if(!string.IsNullOrEmpty(catRes.ExceptionString))
                            {
                                return new RServiceResult<GanjoorPageCompleteViewModel>(null, catRes.ExceptionString);
                            }
                            page.PoetOrCat = catRes.Result;
                        }
                        break;
                    default:
                        {
                            if (dbPage.PoetId != null)
                            {
                                var poetRes = await GetPoetById((int)dbPage.PoetId);
                                if (!string.IsNullOrEmpty(poetRes.ExceptionString))
                                {
                                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, poetRes.ExceptionString);
                                }
                                page.PoetOrCat = poetRes.Result;
                            }
                        }
                        break;
                }
                return new RServiceResult<GanjoorPageCompleteViewModel>(page);

            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorPageCompleteViewModel>(null, exp.ToString());
            }
        }


       
        /// <summary>
        /// get poem recitations  (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemRecitations(int id)
        {
            try
            {
                var source =
                     from audio in _context.Recitations
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     where
                     audio.ReviewStatus == AudioReviewStatus.Approved
                     &&
                     poem.Id == id
                     orderby audio.AudioOrder
                     select new PublicRecitationViewModel()
                     {
                         Id = audio.Id,
                         PoemId = audio.GanjoorPostId,
                         PoemFullTitle = poem.FullTitle,
                         PoemFullUrl = poem.FullUrl,
                         AudioTitle = audio.AudioTitle,
                         AudioArtist = audio.AudioArtist,
                         AudioArtistUrl = audio.AudioArtistUrl,
                         AudioSrc = audio.AudioSrc,
                         AudioSrcUrl = audio.AudioSrcUrl,
                         LegacyAudioGuid = audio.LegacyAudioGuid,
                         Mp3FileCheckSum = audio.Mp3FileCheckSum,
                         Mp3SizeInBytes = audio.Mp3SizeInBytes,
                         PublishDate = audio.ReviewDate,
                         FileLastUpdated = audio.FileLastUpdated,
                         Mp3Url = $"https://ganjgah.ir/api/audio/file/{audio.Id}.mp3",
                         XmlText = $"https://ganjgah.ir/api/audio/xml/{audio.Id}",
                         PlainText = "", //poem.PlainText 
                         HtmlText = "",//poem.HtmlText
                     };
                return new RServiceResult<PublicRecitationViewModel[]>(await source.ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PublicRecitationViewModel[]>(null, exp.ToString());
            }
        }

        private string _Linkify(string SearchText)
        {
            // this will find links like:
            // http://www.mysite.com
            // as well as any links with other characters directly in front of it like:
            // href="http://www.mysite.com"
            // you can then use your own logic to determine which links to linkify
            Regex regx = new Regex(@"\b(((\S+)?)(@|mailto\:|(news|(ht|f)tp(s?))\://)\S+)\b", RegexOptions.IgnoreCase);
            SearchText = SearchText.Replace("&nbsp;", " ");
            MatchCollection matches = regx.Matches(SearchText);

            foreach (Match match in matches)
            {
                if (match.Value.StartsWith("http"))
                { // if it starts with anything else then dont linkify -- may already be linked!
                    SearchText = SearchText.Replace(match.Value, "<a href='" + match.Value + "'>" + match.Value + "</a>");
                }
            }

            return SearchText;
        }

        /// <summary>
        /// get poem comments
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCommentSummaryViewModel[]>> GetPoemComments(int poemId, Guid userId)
        {
            try
            {
                var source =
                      from comment in _context.GanjoorComments.Include(c => c.User)
                      where
                      (comment.Status == PublishStatus.Published || (userId != Guid.Empty && comment.Status == PublishStatus.Awaiting && comment.UserId == userId))
                      &&
                      comment.PoemId == poemId
                      orderby comment.CommentDate
                      select new GanjoorCommentSummaryViewModel()
                      {
                          Id = comment.Id,
                          AuthorName = comment.User == null ? comment.AuthorName : $"{comment.User.FirstName} {comment.User.SureName}".Trim(),
                          AuthorUrl = comment.AuthorUrl,
                          CommentDate = comment.CommentDate,
                          HtmlComment = comment.HtmlComment,
                          PublishStatus = comment.Status == PublishStatus.Awaiting ? "در انتظار تأیید" : "",
                          InReplyToId = comment.InReplyToId,
                          UserId = comment.UserId
                      };

                GanjoorCommentSummaryViewModel[] allComments = await source.ToArrayAsync();

                foreach(GanjoorCommentSummaryViewModel comment in allComments)
                {
                    comment.AuthorName = comment.AuthorName.ToPersianNumbers().ApplyCorrectYeKe();
                    comment.HtmlComment = comment.HtmlComment.ToPersianNumbers().ApplyCorrectYeKe();
                    comment.HtmlComment = _Linkify(comment.HtmlComment);
                    comment.HtmlComment = $"<p>{comment.HtmlComment.Replace("\r\n", "\n").Replace("\n\n", "\n").Replace("\n", "<br />")}</p>";
                }

                GanjoorCommentSummaryViewModel[] rootComments = allComments.Where(c => c.InReplyToId == null).ToArray();

                foreach(GanjoorCommentSummaryViewModel comment in rootComments)
                {
                    _FindReplies(comment, allComments);
                }

                return new RServiceResult<GanjoorCommentSummaryViewModel[]>(rootComments);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorCommentSummaryViewModel[]>(null, exp.ToString());
            }
        }

        private void _FindReplies(GanjoorCommentSummaryViewModel comment, GanjoorCommentSummaryViewModel[] allComments)
        {
            comment.Replies = allComments.Where(c => c.InReplyToId == comment.Id).ToArray();
            foreach(GanjoorCommentSummaryViewModel reply in comment.Replies)
            {
                _FindReplies(reply, allComments);
            }
        }

        /// <summary>
        /// new comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ip"></param>
        /// <param name="poemId"></param>
        /// <param name="content"></param>
        /// <param name="inReplyTo"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCommentSummaryViewModel>> NewComment(Guid userId, string ip, int poemId, string content, int? inReplyTo)
        {
            try
            {
                if(string.IsNullOrEmpty(content))
                {
                    return new RServiceResult<GanjoorCommentSummaryViewModel>(null, "متن حاشیه خالی است.");
                }

                GanjoorComment comment = new GanjoorComment()
                {
                    UserId = userId,
                    AuthorIpAddress = ip,
                    CommentDate = DateTime.Now,
                    HtmlComment = content,
                    InReplyToId = inReplyTo,
                    PoemId = poemId,
                    Status = PublishStatus.Published,
                };
                _context.GanjoorComments.Add(comment);
                await _context.SaveChangesAsync();

                var userRes = await _appUserService.GetUserInformation(userId);

                comment.HtmlComment = comment.HtmlComment.ToPersianNumbers().ApplyCorrectYeKe();
                comment.HtmlComment = _Linkify(comment.HtmlComment);
                comment.HtmlComment = $"<p>{comment.HtmlComment.Replace("\r\n", "\n").Replace("\n\n", "\n").Replace("\n", "<br />")}</p>";


                return new RServiceResult<GanjoorCommentSummaryViewModel>
                    (
                    new GanjoorCommentSummaryViewModel()
                    {
                        Id = comment.Id,
                        AuthorName = $"{userRes.Result.FirstName} {userRes.Result.SureName}".Trim(),
                        AuthorUrl = comment.AuthorUrl,
                        CommentDate = comment.CommentDate,
                        HtmlComment = comment.HtmlComment,
                        PublishStatus = comment.Status == PublishStatus.Awaiting ? "در انتظار تأیید" : "",
                        InReplyToId = comment.InReplyToId,
                        UserId = comment.UserId
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorCommentSummaryViewModel>(null, exp.ToString());
            }
        }


        /// <summary>
        /// get recent comments
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items)>> GetRecentComments(PagingParameterModel paging)
        {
            try
            {
                var source =
                     from comment in _context.GanjoorComments.Include(c => c.Poem).Include(c => c.User).Include(c => c.InReplyTo).ThenInclude(r => r.User)
                     where
                    comment.Status == PublishStatus.Published
                     orderby comment.CommentDate descending
                     select new GanjoorCommentFullViewModel()
                     {
                         Id = comment.Id,
                         AuthorName = comment.User == null ? comment.AuthorName : $"{comment.User.FirstName} {comment.User.SureName}".Trim(),
                         AuthorUrl = comment.AuthorUrl,
                         CommentDate = comment.CommentDate,
                         HtmlComment = comment.HtmlComment,
                         InReplayTo = comment.InReplyTo == null ? null :
                            new GanjoorCommentSummaryViewModel()
                            {
                                Id = comment.InReplyTo.Id,
                                AuthorName = comment.InReplyTo.User == null ? comment.InReplyTo.AuthorName : $"{comment.InReplyTo.User.FirstName} {comment.InReplyTo.User.SureName}".Trim(),
                                AuthorUrl = comment.InReplyTo.AuthorUrl,
                                CommentDate = comment.InReplyTo.CommentDate,
                                HtmlComment = comment.InReplyTo.HtmlComment,
                                PublishStatus = ""
                            },
                         Poem = new GanjoorPoemSummaryViewModel()
                         {
                             Id = comment.Poem.Id,
                             Title = comment.Poem.FullTitle,
                             UrlSlug = comment.Poem.FullUrl,
                             Excerpt = ""
                         }
                     };

                (PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items) paginatedResult =
                    await QueryablePaginator<GanjoorCommentFullViewModel>.Paginate(source, paging);


                foreach (GanjoorCommentFullViewModel comment in paginatedResult.Items)
                {
                    comment.AuthorName = comment.AuthorName.ToPersianNumbers().ApplyCorrectYeKe();
                    comment.HtmlComment = comment.HtmlComment.ToPersianNumbers().ApplyCorrectYeKe();
                    comment.HtmlComment = _Linkify(comment.HtmlComment);
                    comment.HtmlComment = $"<p>{comment.HtmlComment.Replace("\r\n", "\n").Replace("\n\n", "\n").Replace("\n", "<br />")}</p>";
                }

                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }


        /// <summary>
        /// get poem images by id (some fields are intentionally field with blank or null),
        /// EntityImageId : the most important data field, image url is https://ganjgah.ir/api/images/thumb/{EntityImageId}.jpg or https://ganjgah.ir/api/images/norm/{EntityImageId}.jpg
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemRelatedImage[]>> GetPoemImages(int id)
        {
            try
            {
                var museumSrc =
                     from link in _context.GanjoorLinks.Include(l => l.Artifact).Include(l => l.Item).ThenInclude(i => i.Images)
                     join poem in _context.GanjoorPoems
                     on link.GanjoorPostId equals poem.Id
                     where
                     link.DisplayOnPage == true
                     &&
                     link.ReviewResult == Models.GanjoorIntegration.ReviewResult.Approved
                     &&
                     poem.Id == id
                     orderby link.ReviewDate
                     select new PoemRelatedImage()
                     {
                         PoemRelatedImageType = PoemRelatedImageType.MuseumLink,
                         ThumbnailImageUrl = $"https://ganjgah.ir/api/images/thumb/{link.Item.Images.First().Id}.jpg",
                         TargetPageUrl = $"https://museum.ganjoor.net/items/{link.Artifact.FriendlyUrl}/{link.Item.FriendlyUrl}",
                         AltText = $"{link.Artifact.Name} » {link.Item.Name}",
                     };
                List<PoemRelatedImage> museumImages = await museumSrc.ToListAsync();

                var externalSrc =
                     from link in _context.PinterestLinks
                     join poem in _context.GanjoorPoems
                     on link.GanjoorPostId equals poem.Id
                     where
                     link.ReviewResult == Models.GanjoorIntegration.ReviewResult.Approved
                     &&
                     poem.Id == id
                     orderby link.ReviewDate
                     select new PoemRelatedImage()
                     {
                         PoemRelatedImageType = PoemRelatedImageType.ExternalLink,
                         ThumbnailImageUrl = $"https://ganjgah.ir/api/images/thumb/{link.Item.Images.First().Id}.jpg",
                         TargetPageUrl = link.PinterestUrl,
                         AltText = link.AltText,
                     };

                museumImages.AddRange(await externalSrc.ToListAsync());

                for(int i=0; i<museumImages.Count; i++)
                {
                    museumImages[i].ImageOrder = 0;
                }


                return new RServiceResult<PoemRelatedImage[]>(museumImages.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemRelatedImage[]>(null, exp.ToString());
            }
        }

        private int _GetRandomPoemId()
        {
            //this is magic number based method!
            int startPoemId = 2130;
            int endPoemId = 2624 + 1; //one is added for مژده ای دل که مسیحا نفسی می‌آید
            Random r = new Random(DateTime.Now.Millisecond);
            int poemId = r.Next(startPoemId, endPoemId);
            if (poemId == endPoemId)
            {
                poemId = 33179;//مژده ای دل که مسیحا نفسی می‌آید
            }
            return poemId;
        }

        /// <summary>
        /// Get Poem By Url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemByUrl(string url, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true)
        {
            try
            {
                // /hafez/ => /hafez :
                if (url.LastIndexOf('/') == url.Length - 1)
                {
                    url = url.Substring(0, url.Length - 1);
                }
                var poem = await _context.GanjoorPoems.Where(p => p.FullUrl == url).SingleOrDefaultAsync();
                if (poem == null)
                {
                    return new RServiceResult<GanjoorPoemCompleteViewModel>(null); //not found
                }
                return await GetPoemById(poem.Id, catInfo, catPoems, rhymes, recitations, images, songs, comments, verseDetails, navigation);
            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get Poem By Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemById(int id, bool catInfo = true, bool catPoems = false , bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true)
        {
            try
            {
                var poem = await _context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == id).SingleOrDefaultAsync();
                if(poem == null)
                {
                    return new RServiceResult<GanjoorPoemCompleteViewModel>(null); //not found
                }
                GanjoorPoetCompleteViewModel cat = null;
                if(catInfo)
                {
                    var catRes = await GetCatById(poem.CatId, catPoems);
                    if(!string.IsNullOrEmpty(catRes.ExceptionString))
                    {
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, catRes.ExceptionString);
                    }
                    cat = catRes.Result;
                }

                GanjoorPoemSummaryViewModel next = null;
                if(navigation)
                {
                    int nextId =
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id > poem.Id)
                                                       .AnyAsync()
                                                       ?
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id > poem.Id)
                                                       .MinAsync(p => p.Id)
                                                       :
                                                       0;
                    if(nextId != 0)
                    {
                        next = await _context.GanjoorPoems.Where(p => p.Id == nextId).Select
                            (
                            p =>
                            new GanjoorPoemSummaryViewModel()
                            {
                                Id = p.Id,
                                Title = p.Title,
                                UrlSlug = p.UrlSlug,
                                Excerpt = _context.GanjoorVerses.Where(v => v.PoemId == p.Id && v.VOrder == 1).FirstOrDefault().Text
                            }
                            ).SingleAsync();
                    }
                                                       
                }

                GanjoorPoemSummaryViewModel previous = null;
                if (navigation)
                {
                    int preId =
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id < poem.Id)
                                                       .AnyAsync()
                                                       ?
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id < poem.Id)
                                                       .MaxAsync(p => p.Id)
                                                       :
                                                       0;
                    if (preId != 0)
                    {
                        previous = await _context.GanjoorPoems.Where(p => p.Id == preId).Select
                            (
                            p =>
                            new GanjoorPoemSummaryViewModel()
                            {
                                Id = p.Id,
                                Title = p.Title,
                                UrlSlug = p.UrlSlug,
                                Excerpt = _context.GanjoorVerses.Where(v => v.PoemId == p.Id && v.VOrder == 1).FirstOrDefault().Text
                            }
                            ).SingleAsync();
                    }

                }

                PublicRecitationViewModel[] rc = null;
                if(recitations)
                {
                    var rcRes = await GetPoemRecitations(id);
                    if (!string.IsNullOrEmpty(rcRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, rcRes.ExceptionString);
                    rc = rcRes.Result;
                }

                PoemRelatedImage[] imgs = null;
                if(images)
                {
                    var imgsRes = await GetPoemImages(id);
                    if (!string.IsNullOrEmpty(imgsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, imgsRes.ExceptionString);
                    imgs = imgsRes.Result;
                }

                GanjoorVerseViewModel[] verses = null;
                if(verseDetails)
                {
                    verses = await _context.GanjoorVerses
                                                    .Where(v => v.PoemId == id)
                                                    .OrderBy(v => v.VOrder)
                                                    .Select
                                                    (
                                                        v => new GanjoorVerseViewModel()
                                                        {
                                                            Id = v.Id,
                                                            VOrder = v.VOrder,
                                                            VersePosition = v.VersePosition,
                                                            Text = v.Text
                                                        }
                                                    ).ToArrayAsync();
                };


                PoemMusicTrackViewModel[] tracks = null;
                if(songs)
                {
                    var songsRes = await GetPoemSongs(id, true, PoemMusicTrackType.All);
                    if(!string.IsNullOrEmpty(songsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, songsRes.ExceptionString);
                    tracks = songsRes.Result;
                }

                GanjoorCommentSummaryViewModel[] poemComments = null;

                if(comments)
                {
                    var commentsRes = await GetPoemComments(id, Guid.Empty);
                    if (!string.IsNullOrEmpty(commentsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, commentsRes.ExceptionString);
                    poemComments = commentsRes.Result;
                }



                return new RServiceResult<GanjoorPoemCompleteViewModel>
                    (
                    new GanjoorPoemCompleteViewModel()
                    {
                        Id = poem.Id,
                        Title = poem.Title,
                        FullTitle = poem.FullTitle,
                        FullUrl = poem.FullUrl,
                        UrlSlug = poem.UrlSlug,
                        HtmlText = poem.HtmlText,
                        PlainText = poem.PlainText,
                        GanjoorMetre = poem.GanjoorMetre,
                        RhymeLetters = poem.RhymeLetters,
                        SourceName = poem.SourceName,
                        SourceUrlSlug = poem.SourceUrlSlug,
                        Category = cat,
                        Next = next,
                        Previous = previous,
                        Recitations = rc,
                        Images = imgs,
                        Verses = verses,
                        Songs = tracks,
                        Comments = poemComments
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null, exp.ToString());
            }
        }


        /// <summary>
        /// get poem related songs
        /// </summary>
        /// <param name="id"></param>
        /// <param name="approved"></param>
        /// <param name="trackType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel[]>> GetPoemSongs(int id, bool approved, PoemMusicTrackType trackType)
        {
            try
            {
                return new RServiceResult<PoemMusicTrackViewModel[]>
                    (
                    await _context.GanjoorPoemMusicTracks
                                                    .Where
                                                    (
                                                        t => t.PoemId == id
                                                        &&
                                                        t.Approved == approved
                                                        &&
                                                        t.Rejected == false
                                                        &&
                                                        (trackType == PoemMusicTrackType.All || t.TrackType == trackType)
                                                    )
                                                    .OrderBy(t => t.Id)
                                                    .Select
                                                    (
                                                     t => new PoemMusicTrackViewModel()
                                                     {
                                                         Id = t.Id,
                                                         PoemId = t.PoemId,
                                                         TrackType = t.TrackType,
                                                         ArtistName = t.ArtistName,
                                                         ArtistUrl = t.ArtistUrl,
                                                         AlbumName = t.AlbumName,
                                                         AlbumUrl = t.AlbumUrl,
                                                         TrackName = t.TrackName,
                                                         TrackUrl = t.TrackUrl,
                                                         Description = t.Description,
                                                         BrokenLink = t.BrokenLink,
                                                         GolhaTrackId = t.GolhaTrackId == null ? 0 : (int)t.GolhaTrackId,
                                                         Approved = t.Approved,
                                                         Rejected = t.Rejected,
                                                         RejectionCause = t.RejectionCause

                                                     }
                                                    ).ToArrayAsync()
                    );
            }
            catch(Exception exp)
            {
                return new RServiceResult<PoemMusicTrackViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// suggest song
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> SuggestSong(Guid userId, PoemMusicTrackViewModel song)
        {
            try
            {
                song.Approved = false;
                song.Rejected = false;
                song.RejectionCause = "";
                song.BrokenLink = false;
                if(song.TrackType == PoemMusicTrackType.Golha)
                {
                    var golhaTrack = await _context.GolhaTracks.Include(g => g.GolhaProgram).ThenInclude(p => p.GolhaCollection).Where(g => g.Id == song.GolhaTrackId).FirstOrDefaultAsync();
                    if (golhaTrack == null)
                    {
                        return new RServiceResult<PoemMusicTrackViewModel>(null, "مشخصات قطعهٔ گلها درست نیست.");
                    }
                    var alreadySuggestedSong = await _context.GanjoorPoemMusicTracks.Where(t => t.PoemId == song.PoemId && t.TrackType == song.TrackType && t.GolhaTrackId == song.GolhaTrackId).FirstOrDefaultAsync();
                    if (alreadySuggestedSong != null)
                    {
                        return new RServiceResult<PoemMusicTrackViewModel>(null, "این آهنگ پیشتر برای این شعر پیشنهاد داده شده است.");
                    }

                    song.ArtistName = "";
                    song.ArtistUrl = "";
                    song.AlbumName = $"{golhaTrack.GolhaProgram.GolhaCollection.Name} » شمارهٔ {golhaTrack.GolhaProgram.Title.ToPersianNumbers().ApplyCorrectYeKe()}";
                    song.AlbumUrl = "";
                    song.TrackName = $"{golhaTrack.Timing.ToPersianNumbers().ApplyCorrectYeKe()} {golhaTrack.Title}";
                    song.TrackUrl = golhaTrack.GolhaProgram.Url;
                }
                else
                {
                    var alreadySuggestedSong = await _context.GanjoorPoemMusicTracks.Where(t => t.PoemId == song.PoemId && t.TrackType == song.TrackType && t.TrackUrl == song.TrackUrl).FirstOrDefaultAsync();
                    if (alreadySuggestedSong != null)
                    {
                        return new RServiceResult<PoemMusicTrackViewModel>(null, "این آهنگ پیشتر برای این شعر پیشنهاد داده شده است.");
                    }

                }
                var sug =
                    new PoemMusicTrack()
                    {
                        TrackType = song.TrackType,
                        PoemId = song.PoemId,
                        ArtistName = song.ArtistName,
                        ArtistUrl = song.ArtistUrl,
                        AlbumName = song.AlbumName,
                        AlbumUrl = song.AlbumUrl,
                        TrackName = song.TrackName,
                        TrackUrl = song.TrackUrl,
                        SuggestedById = userId,
                        Description = song.Description,
                        GolhaTrackId = song.TrackType == PoemMusicTrackType.Golha ? song.GolhaTrackId : (int?)null,
                        Approved = false,
                        Rejected = false,
                        RejectionCause = ""
                    };

                GanjoorSinger singer = await _context.GanjoorSingers.Where(s => s.Url == song.ArtistUrl).FirstOrDefaultAsync();
                if (singer != null)
                {
                    sug.SingerId = singer.Id;
                }

                _context.GanjoorPoemMusicTracks.Add
                    (
                    sug
                    );

                await _context.SaveChangesAsync();
                song.Id = sug.Id;
                return new RServiceResult<PoemMusicTrackViewModel>(song);
            }
            catch(Exception exp)
            {
                return new RServiceResult<PoemMusicTrackViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get unreviewed count
        /// </summary>
        /// <param name="suggestedById"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreviewedSongsCount(Guid suggestedById)
        {
            try
            {
                return new RServiceResult<int>(await _context.GanjoorPoemMusicTracks
                   .Where(p => p.Approved == false && p.Rejected == false && (suggestedById == Guid.Empty || p.SuggestedById == suggestedById))
                   .CountAsync());
            }
            catch(Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }


        /// <summary>
        /// next unreviewed track
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="suggestedById"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> GetNextUnreviewedSong(int skip, Guid suggestedById)
        {
            try
            {
                var song = await _context.GanjoorPoemMusicTracks
                    .Where(p => p.Approved == false && p.Rejected == false && (suggestedById == Guid.Empty || p.SuggestedById == suggestedById))
                    .OrderBy(p => p.Id).Skip(skip).FirstOrDefaultAsync();
                if(song != null)
                {
                    return new RServiceResult<PoemMusicTrackViewModel>
                        (
                        new PoemMusicTrackViewModel()
                        {
                            Id = song.Id,
                            TrackType = song.TrackType,
                            PoemId = song.PoemId,
                            ArtistName = song.ArtistName,
                            ArtistUrl = song.ArtistUrl,
                            AlbumName = song.AlbumName,
                            AlbumUrl = song.AlbumUrl,
                            TrackName = song.TrackName,
                            TrackUrl = song.TrackUrl,
                            Description = song.Description,
                            GolhaTrackId = song.TrackType == PoemMusicTrackType.Golha ? (int)song.GolhaTrackId : 0,
                            BrokenLink = song.BrokenLink,
                            Approved = song.Approved,
                            Rejected = song.Rejected,
                            RejectionCause = song.RejectionCause
                        }
                        );
                }
                return new RServiceResult<PoemMusicTrackViewModel>(null); //not found
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemMusicTrackViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// review song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> ReviewSong(PoemMusicTrackViewModel song)
        {
            try
            {
                if (song.Approved && song.Rejected)
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "song.Approved && song.Rejected");

                var track = await _context.GanjoorPoemMusicTracks.Where(t => t.Id == song.Id).SingleOrDefaultAsync();

                track.TrackType = song.TrackType;
                track.ArtistName = song.ArtistName;
                track.ArtistUrl = song.ArtistUrl;
                track.AlbumName = song.AlbumName;
                track.AlbumUrl = song.AlbumUrl;
                track.TrackName = song.TrackName;
                track.TrackUrl = song.TrackUrl;
                if(!track.Approved && song.Approved)
                {
                    track.ApprovalDate = DateTime.Now;
                }
                track.Approved = song.Approved;
                track.Rejected = song.Rejected;
                track.RejectionCause = song.RejectionCause;
                track.BrokenLink = song.BrokenLink;

                GanjoorSinger singer = await _context.GanjoorSingers.Where(s => s.Url == track.ArtistUrl).FirstOrDefaultAsync();
                if (singer != null)
                {
                    track.SingerId = singer.Id;
                }

                _context.GanjoorPoemMusicTracks.Update(track);

                await _context.SaveChangesAsync();

                return new RServiceResult<PoemMusicTrackViewModel>(song);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemMusicTrackViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// direct insert song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> DirectInsertSong(PoemMusicTrackViewModel song)
        {
            try
            {

                var poem = await _context.GanjoorPoems.Where(p => p.Id == song.PoemId).SingleOrDefaultAsync();
                if (poem == null)
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "poem == null");

                if
                    (
                    string.IsNullOrEmpty(song.ArtistName)
                    ||
                    string.IsNullOrEmpty(song.ArtistUrl)
                    ||
                    string.IsNullOrEmpty(song.AlbumName)
                    ||
                    string.IsNullOrEmpty(song.AlbumUrl)
                    ||
                    string.IsNullOrEmpty(song.TrackName)
                    ||
                    string.IsNullOrEmpty(song.TrackUrl)
                    ||
                    song.TrackType != PoemMusicTrackType.BeepTunesOrKhosousi
                    )
                {
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "data validation err");
                }

                var duplicated = await _context.GanjoorPoemMusicTracks.Where(m => m.PoemId == song.PoemId && m.TrackUrl == song.TrackUrl).FirstOrDefaultAsync();
                if(duplicated != null)
                {
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "duplicated song url for this poem");
                }
                

                PoemMusicTrack track = new PoemMusicTrack();

                track.PoemId = song.PoemId;
                track.TrackType = song.TrackType;
                track.ArtistName = song.ArtistName;
                track.ArtistUrl = song.ArtistUrl;
                track.AlbumName = song.AlbumName;
                track.AlbumUrl = song.AlbumUrl;
                track.TrackName = song.TrackName;
                track.TrackUrl = song.TrackUrl;
                track.ApprovalDate = DateTime.Now;
                track.Approved = true;
                track.Rejected = false;
                track.BrokenLink = song.BrokenLink;

                GanjoorSinger singer = await _context.GanjoorSingers.Where(s => s.Url == track.ArtistUrl).FirstOrDefaultAsync();
                if (singer != null)
                {
                    track.SingerId = singer.Id;
                }

                _context.GanjoorPoemMusicTracks.Add(track);

                await _context.SaveChangesAsync();

                return new RServiceResult<PoemMusicTrackViewModel>(song);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemMusicTrackViewModel>(null, exp.ToString());
            }
        }



        /// <summary>
        /// get a random poem from hafez
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> Faal()
        {
            try
            {
                int poemId = _GetRandomPoemId();
                var poem = await _context.GanjoorPoems.Where(p => p.Id == poemId).SingleOrDefaultAsync();
                PublicRecitationViewModel[] recitations = poem == null ? new PublicRecitationViewModel[] { } : (await GetPoemRecitations(poemId)).Result;
                int loopPreventer = 0;
                while (poem == null || recitations.Length == 0)
                {
                    poem = await _context.GanjoorPoems.Where(p => p.Id == poemId).SingleOrDefaultAsync();
                    recitations = poem == null ? new PublicRecitationViewModel[] { } : (await GetPoemRecitations(poemId)).Result;
                    loopPreventer++;
                    if (loopPreventer > 5)
                        break;
                }

                return await GetPoemById(poemId, false, false, false, true, false, false, false, false, false);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null, exp.ToString());
            }
        }

        

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="appUserService"></param>
        public GanjoorService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue, IAppUserService appUserService)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;
            _appUserService = appUserService;
            Configuration = configuration;
        }
    }
}
