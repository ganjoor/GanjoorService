using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public class GanjoorService : IGanjoorService
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
                    case GanjoorPageType.PoetPage:
                        {
                            var poetRes = await GetPoetById((int)dbPage.PoetId);
                            if(!string.IsNullOrEmpty(poetRes.ExceptionString))
                            {
                                return new RServiceResult<GanjoorPageCompleteViewModel>(null, poetRes.ExceptionString);
                            }
                            page.PoetOrCat = poetRes.Result;
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
                var poem = await _context.GanjoorPoems.Where(p => p.Id == id).SingleOrDefaultAsync();
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
                        Category = cat,
                        Next = next,
                        Previous = previous,
                        Recitations = rc,
                        Images = imgs,
                        Verses = verses
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null, exp.ToString());
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

        #region Date import / modifications
        /// <summary>
        /// imports unimported poem data from a locally accessible ganjoor SqlLite database
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ImportLocalSQLiteDb()
        {
            try
            {
                SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();
                connectionStringBuilder.DataSource = Configuration.GetSection("LocalSqliteImport")["FilePath"];
                using (SqliteConnection sqliteConnection = new SqliteConnection(connectionStringBuilder.ToString()))
                {
                    await sqliteConnection.OpenAsync();
                    IDbConnection dapper = sqliteConnection;
                    using (var sqlConnection = _context.Database.GetDbConnection())
                    {
                        await sqlConnection.OpenAsync();
                        
                            foreach (var poet in await dapper.QueryAsync("SELECT * FROM poet ORDER BY id"))
                            {
                                int poetId = (int)poet.id;
                                if ((await _context.GanjoorPoets.Where(p => p.Id == poetId).FirstOrDefaultAsync()) != null)
                                {
                                    continue;
                                }

                                DbTransaction transaction = await sqlConnection.BeginTransactionAsync();
                                try
                                {
                                    using (var command = sqlConnection.CreateCommand())
                                    {
                                        command.Transaction = transaction;
                                        command.CommandText =
                                            $"INSERT INTO GanjoorPoets (Id, Name, Description) VALUES (${poet.id}, N'{poet.name}', N'{poet.description}')";
                                        await command.ExecuteNonQueryAsync();
                                        await _ImportSQLiteCatChildren(command, dapper, poetId, 0, "", "");
                                        await transaction.CommitAsync();
                                    }
                                }
                                catch (Exception exp2)
                                {
                                    await transaction.RollbackAsync();
                                    return new RServiceResult<bool>(false, exp2.ToString());
                                }
                            
                               
                            }
                    }
                        
                }
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            return new RServiceResult<bool>(true);
        }
        private async Task _ImportSQLiteCatChildren(DbCommand command, IDbConnection dapper, int poetId, int catId, string itemFullTitle, string itemFullSlug)
        {
           
            foreach(var cat in await dapper.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} AND parent_id = {catId} ORDER BY id"))
            {
               
                if (catId == 0)
                {
                    command.CommandText =
                                  $"INSERT INTO GanjoorCategories (Id, PoetId, Title, UrlSlug, FullUrl) VALUES (${cat.id}, {poetId}, N'{cat.text}', '{ _ExtractUrlSlug(cat.url)}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}')";
                }
                else
                {
                    command.CommandText =
                                  $"INSERT INTO GanjoorCategories (Id, PoetId, Title, ParentId, UrlSlug, FullUrl) VALUES (${cat.id}, {poetId}, N'{cat.text}', {catId}, '{ _ExtractUrlSlug(cat.url)}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}')";
                }
               
                await command.ExecuteNonQueryAsync();


                foreach (var poem in await dapper.QueryAsync($"SELECT * FROM poem WHERE cat_id = {cat.id} ORDER BY id"))
                {
                    command.CommandText =
                               $"INSERT INTO GanjoorPoems (Id, CatId, Title, UrlSlug, FullTitle, FullUrl) VALUES (${poem.id}, {cat.id}, N'{poem.title}', '{ _ExtractUrlSlug(poem.url)}', N'{$"{itemFullTitle}{cat.text} » {poem.title}"}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}/{_ExtractUrlSlug(poem.url)}')";
                    await command.ExecuteNonQueryAsync();

                    foreach (var verse in await dapper.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"))
                    {
                        command.CommandText =
                              $"INSERT INTO GanjoorVerses (PoemId, VOrder, VersePosition, Text) VALUES (${poem.id}, {verse.vorder}, {verse.position}, N'{verse.text}')";
                        await command.ExecuteNonQueryAsync();
                    }

                }


                await _ImportSQLiteCatChildren(command, dapper, poetId, (int)cat.id, $"{itemFullTitle}{cat.text} » ", $"{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}");
                

            }
        }
        private static string _ExtractUrlSlug(string slug)
        {
            if (!string.IsNullOrEmpty(slug))
            {
                //sample: https://ganjoor.net/hafez
                if (slug.LastIndexOf('/') != -1)
                {
                    // => result = hafez
                    slug = slug.Substring(slug.LastIndexOf('/') + 1);
                }
            }
            return slug;
        }

        /// <summary>
        /// updates poems text
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> UpdatePoemsText()
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                async token =>
                {
                    using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                    {
                        var poems = await context.GanjoorPoems.Where(p => p.PlainText == null).ToListAsync();
                        int n = 0;
                        foreach (GanjoorPoem poem in poems)
                        {
                            var verses = await context.GanjoorVerses.Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync();
                            string plainText = "";
                            string htmlText = "";
                            for (int vIndex = 0; vIndex < verses.Count; vIndex++)
                            {
                                GanjoorVerse v = verses[vIndex];

                                if (string.IsNullOrEmpty(plainText))
                                    plainText = v.Text;
                                else
                                {
                                    plainText += $" {v.Text}";
                                }

                                switch (v.VersePosition)
                                {
                                    case VersePosition.CenteredVerse1:
                                        if (((vIndex + 1) < verses.Count) && (verses[vIndex + 1].VersePosition == VersePosition.CenteredVerse2))
                                        {
                                            htmlText += $"<div class=\"b2\"><p>{v.Text.Replace("ـ", "").Trim()}</p>{Environment.NewLine}"; //div is not closed
                                        }
                                        else
                                        {
                                            htmlText += $"<div class=\"b2\"><p>{v.Text.Replace("ـ", "").Trim()}</p></div>{Environment.NewLine}";
                                        }
                                        break;
                                    case VersePosition.CenteredVerse2:
                                        htmlText += $"<p>{v.Text.Replace("ـ", "").Trim()}</p></div>{Environment.NewLine}";
                                        break;
                                    case VersePosition.Right:
                                        htmlText += $"<div class=\"b\"><div class=\"m1\"><p>{v.Text.Replace("ـ", "").Trim()}</p></div>{Environment.NewLine}";
                                        break;
                                    case VersePosition.Left:
                                        htmlText += $"<div class=\"m2\"><p>{v.Text.Replace("ـ", "").Trim()}</p></div></div>{Environment.NewLine}";
                                        break;
                                    case VersePosition.Paragraph:
                                    case VersePosition.Single:
                                        {
                                            string[] lines = v.Text.Replace("ـ", "").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                                            if (lines.Length != 0)
                                            {
                                                if (v.Text.Replace("ـ", "").Length / lines.Length < 150)
                                                {
                                                    htmlText += $"<div class=\"n\"><p>{v.Text.Replace("ـ", "").Replace("\r\n", " ")}</p></div>{Environment.NewLine}";
                                                }
                                                else
                                                {
                                                    foreach (string line in lines)
                                                        htmlText += $"<div class=\"n\"><p>{line}</p></div>{Environment.NewLine}";
                                                }
                                            }
                                        }
                                        break;
                                }
                            }

                            poem.PlainText = plainText;
                            poem.HtmlText = htmlText;

                            context.Update(poem);

                            n++;

                            if (n >= 1000)
                            {
                                await context.SaveChangesAsync();
                                n = 0;
                            }
                        }

                        if(n > 0)
                            await context.SaveChangesAsync();


                    }


                });


                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

        }


        /// <summary>
        /// import GanjoorPage entity data from MySql
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> ImportFromMySql()
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                async token =>
                {
                    
                    using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                    using (RMuseumDbContext contextReport = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                    {
                        LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(contextReport);
                        var job = (await jobProgressServiceEF.NewJob("GanjoorService:ImportFromMySql", "pre open connection")).Result;

                        try
                        {
                            using (MySqlConnection connection = new MySqlConnection
                                            (
                                            $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8;convert zero datetime=True"
                                            ))
                            {
                                connection.Open();
                                using (MySqlDataAdapter src = new MySqlDataAdapter(
                                    "SELECT ID, post_author, post_date, post_date_gmt, post_content, post_title, post_category, post_excerpt, post_status, comment_status, ping_status, post_password, post_name, to_ping, pinged, post_modified, post_modified_gmt, post_content_filtered, post_parent, guid, menu_order, post_type, post_mime_type, comment_count, " +
                                    "COALESCE((SELECT meta_value FROM ganja_postmeta WHERE post_id = ID AND meta_key='_wp_page_template'), '') AS template," +
                                    "(SELECT meta_value FROM ganja_postmeta WHERE post_id = ID AND meta_key='otherpoetid') AS other_poet_id " +
                                    "FROM ganja_posts",
                                    connection))
                                {
                                    using (DataTable srcData = new DataTable())
                                    {
                                        await src.FillAsync(srcData);

                                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 1")).Result;


                                        foreach (DataRow row in srcData.Rows)
                                        {
                                            GanjoorPageType pageType =
                                                row["post_type"].ToString() == "post" && row["comment_status"].ToString() != "closed" ?
                                                        GanjoorPageType.PoemPage
                                                        :
                                                        row["template"].ToString() == "comspage.php" ?
                                                        GanjoorPageType.AllComments
                                                        :
                                                        row["template"].ToString() == "relations.php" ?
                                                        GanjoorPageType.ProsodySimilars
                                                        :
                                                        row["template"].ToString() == "vazn.php" ?
                                                        GanjoorPageType.ProsodyAndStats
                                                        :
                                                        GanjoorPageType.None;

                                            int? poetId = row["post_author"].ToString() == "0" ? (int?)null : int.Parse(row["post_author"].ToString());
                                            if (poetId == 36)//رشحه
                                            {
                                                continue;
                                            }

                                            if (poetId != null)
                                            {
                                                if (!await context.GanjoorPoets.Where(poet => poet.Id == poetId).AnyAsync())
                                                {
                                                    continue;
                                                }
                                            }

                                            GanjoorPage page = new GanjoorPage()
                                            {
                                                Id = int.Parse(row["ID"].ToString()),
                                                GanjoorPageType = pageType,
                                                Published = true,
                                                PageOrder = -1,
                                                Title = row["post_title"].ToString(),
                                                UrlSlug = row["post_name"].ToString(),
                                                HtmlText = row["post_content"].ToString(),
                                                ParentId = row["post_parent"].ToString() == "0" ? (int?)null : int.Parse(row["post_parent"].ToString()),
                                                PoetId = row["post_author"].ToString() == "1" ? (int?)null : int.Parse(row["post_author"].ToString()),
                                                SecondPoetId = row["other_poet_id"] == DBNull.Value ? (int?)null : int.Parse(row["other_poet_id"].ToString()),
                                                PostDate = (DateTime)row["post_date"]
                                            };



                                            if (pageType == GanjoorPageType.PoemPage)
                                            {
                                                var poem = await context.GanjoorPoems.Where(p => p.Id == page.Id).FirstOrDefaultAsync();
                                                if (poem == null)
                                                    continue;
                                                page.PoemId = poem.Id;
                                            }
                                            if (poetId != null && pageType == GanjoorPageType.None)
                                            {
                                                GanjoorCat cat = await context.GanjoorCategories.Where(c => c.PoetId == poetId && c.ParentId == null && c.UrlSlug == page.UrlSlug).SingleOrDefaultAsync();
                                                if (cat != null)
                                                {
                                                    page.GanjoorPageType = GanjoorPageType.PoetPage;
                                                    page.CatId = cat.Id;
                                                }
                                                else
                                                {
                                                    cat = await context.GanjoorCategories.Where(c => c.PoetId == poetId && c.ParentId != null && c.UrlSlug == page.UrlSlug).SingleOrDefaultAsync();
                                                    if (cat != null)
                                                    {
                                                        page.GanjoorPageType = GanjoorPageType.CatPage;
                                                        page.CatId = cat.Id;
                                                    }
                                                }
                                            }

                                            context.GanjoorPages.Add(page);
                                        }
                                    }
                                }
                            }
                            await context.SaveChangesAsync();

                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 2")).Result;


                            var orphanPages = await context.GanjoorPages.Include(p => p.Poem).Where(p => p.FullUrl == null).ToListAsync();
                            double count = orphanPages.Count;
                            int i = 0;
                            foreach (var page in orphanPages)
                            {

                                job = (await jobProgressServiceEF.UpdateJob(job.Id, i++, "phase 2")).Result;

                                string fullUrl = page.UrlSlug;
                                string fullTitle = page.Title;

                                if (page.GanjoorPageType == GanjoorPageType.PoemPage)
                                {
                                    fullTitle = page.Poem.FullTitle;
                                    fullUrl = page.Poem.FullUrl;
                                }
                                else
                                {
                                    if (page.ParentId != null)
                                    {
                                        GanjoorPage parent = await context.GanjoorPages.Where(p => p.Id == page.ParentId).SingleAsync();
                                        while (parent != null)
                                        {
                                            fullUrl = parent.UrlSlug + "/" + fullUrl;
                                            fullTitle = parent.Title + " » " + fullTitle;
                                            parent = parent.ParentId == null ? null : await context.GanjoorPages.Where(p => p.Id == parent.ParentId).SingleAsync();
                                        }
                                    }
                                    else
                                    {
                                        GanjoorCat cat = await context.GanjoorCategories.Where(c => c.PoetId == page.PoetId && c.UrlSlug == page.UrlSlug).SingleOrDefaultAsync();
                                        if (cat != null)
                                        {
                                            fullUrl = cat.FullUrl;
                                            while (cat.ParentId != null)
                                            {
                                                cat = await context.GanjoorCategories.Where(c => c.Id == cat.ParentId).SingleOrDefaultAsync();
                                                if (cat != null)
                                                {
                                                    fullTitle = cat.Title + " » " + fullTitle;
                                                }
                                            }
                                        }
                                    }

                                }
                                page.FullUrl = fullUrl;
                                page.FullTitle = fullTitle;

                                context.Update(page);
                            }

                            await context.SaveChangesAsync();

                            await jobProgressServiceEF.UpdateJob(job.Id, 100, "Finised", true);
                        }
                        catch(Exception jobExp)
                        {
                            await jobProgressServiceEF.UpdateJob(job.Id, job.Progress, "", false, jobExp.ToString());
                        }
                    }
                });
    

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private List<GanjoorVerse> _extractVersesFromPoemHtmlText(string poemtext)
        {
            List<GanjoorVerse> verses = new List<GanjoorVerse>();

            //this spagetti code has been imported from my internal utilities:
            while (poemtext.IndexOf("<a href") != -1)
            {
                int ahrefStart = poemtext.IndexOf("<a href");
                string part1 = poemtext.Substring(0, ahrefStart);
                string part2 = poemtext.Substring(poemtext.IndexOf(">", ahrefStart) + 1, poemtext.IndexOf("</a>") - (poemtext.IndexOf(">", ahrefStart) + 1));
                poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("</a>") + 4, poemtext.Length - (poemtext.IndexOf("</a>") + 4));
            }
            while (poemtext.IndexOf("<acronym") != -1)
            {
                int acroStart = poemtext.IndexOf("<acronym");
                string part1 = poemtext.Substring(0, acroStart);
                string part2;
                try
                {
                    part2 = poemtext.Substring(poemtext.IndexOf(">", acroStart) + 1, poemtext.IndexOf("</acronym>") - (poemtext.IndexOf(">", acroStart) + 1));
                    poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("</acronym>") + 10, poemtext.Length - (poemtext.IndexOf("</acronym>") + 10));
                }
                catch
                {
                    part2 = poemtext.Substring(poemtext.IndexOf(">", acroStart) + 1, poemtext.IndexOf("<acronym>") - (poemtext.IndexOf(">", acroStart) + 1));
                    poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("<acronym>") + 10, poemtext.Length - (poemtext.IndexOf("<acronym>") + 10));
                }

            }

            while (poemtext.IndexOf("<sup>") != -1)
            {
                string part1 = poemtext.Substring(0, poemtext.IndexOf("<sup>"));
                try
                {
                    poemtext = part1 + poemtext.Substring(poemtext.IndexOf("</sup>") + 6, poemtext.Length - (poemtext.IndexOf("</sup>") + 6));
                    poemtext = poemtext.Replace("  ", " ");
                }
                catch
                {
                    throw new Exception($"poemtext.IndexOf(\"<sup>\": {poemtext}");
                }

            }


            poemtext = poemtext.Replace("Adaptation du milieu", "یییییییییییییییییییی");
            poemtext = poemtext.Replace("Empirique", "ببببببببب");


            poemtext = poemtext.Replace("<div class=\"b\" style=\"width:750px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:660px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:680px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:650px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:690px\">", "<div class=\"b\">").Replace("<p style=\"color:#911\">", "<p>").Replace("<p style=\"color:#191\">", "<p>").Replace("<div class=\"spacer\">", "").Replace("&nbsp;", "").Replace("<div class=\"spacer\" />", "").Replace("<div class=\"b\" style=\"width:700px\">", "<div class=\"b\">");
            poemtext = poemtext.Replace("<em>", "").Replace("</em>", "");
            poemtext = poemtext.Replace("<em>", "").Replace("</em>", "").Replace("<small>", "").Replace("</small>", "");
            poemtext = poemtext.Replace("<b>", "").Replace("</b>", "").Replace("<strong>", "").Replace("</strong>", "");
            poemtext = poemtext.Replace("<p><br style=\"clear:both;\"/></p>", "").Replace("<br style=\"clear:both;\"/>", "");
            if (poemtext.IndexOf("\r\n") == 0)
                poemtext = poemtext.Substring(2);
            poemtext = poemtext.Replace("\r", "").Replace("\n", "");
            poemtext = poemtext.Replace("</div>", "").Replace("</p>", "");
            poemtext = poemtext.Replace("<div class=\"b2\">", "a");
            poemtext = poemtext.Replace("<div class=\"b\">", "b");
            poemtext = poemtext.Replace("<div class=\"m1\">", "m");
            poemtext = poemtext.Replace("<div class=\"m2\">", "n");
            poemtext = poemtext.Replace("<div class=\"n\">", "");
            poemtext = poemtext.Replace("<p>", "p");
            poemtext = poemtext.Replace("bmp", "b");
            poemtext = poemtext.Replace("np", "n");
            poemtext = poemtext.Replace("ap", "a");
            poemtext = poemtext.Replace("\"", "").Replace("'", "");
            if (poemtext.IndexOfAny(new char[] { '<', '>' }) != -1)
                throw new Exception($"Invalid Characteres: {poemtext}");
            if (poemtext.IndexOf("mp") != -1)
                throw new Exception($"مصرع اول بدون مصرع دوم: {poemtext}");

            if (poemtext.Length > 0)
            {

                int idx = poemtext.IndexOfAny(new char[] { 'a', 'b', 'm', 'n', 'p' });
                bool preWasBand = false;
                while (idx != -1)
                {
                    GanjoorVerse verse = new GanjoorVerse();
                    verse.VOrder = verses.Count + 1;
                    switch (poemtext[idx])
                    {
                        case 'p':
                            if (preWasBand)
                                verse.VersePosition = VersePosition.CenteredVerse2;
                            else
                                verse.VersePosition = VersePosition.Paragraph;
                            preWasBand = false;
                            break;
                        case 'b':
                            verse.VersePosition = VersePosition.Right;
                            preWasBand = false;
                            break;
                        case 'n':
                            verse.VersePosition = VersePosition.Left;
                            preWasBand = false;
                            break;
                        case 'a':
                            verse.VersePosition = VersePosition.CenteredVerse1;
                            preWasBand = true;
                            break;
                    }
                    int nextIdx = poemtext.IndexOfAny(new char[] { 'a', 'b', 'm', 'n', 'p' }, idx + 1);
                    if (nextIdx == -1)
                    {
                        verse.Text = poemtext.Substring(idx + 1).Replace("یییییییییییییییییییی", "Adaptation du milieu").Replace("ببببببببب", "Empirique");
                    }
                    else
                    {
                        verse.Text = poemtext.Substring(idx + 1, nextIdx - idx - 1).Replace("یییییییییییییییییییی", "Adaptation du milieu").Replace("ببببببببب", "Empirique");
                    }

                    verses.Add(verse);

                    idx = nextIdx;
                }
            }

            return verses;
        }
        #endregion

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
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        public GanjoorService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;
            Configuration = configuration;
        }
    }
}
