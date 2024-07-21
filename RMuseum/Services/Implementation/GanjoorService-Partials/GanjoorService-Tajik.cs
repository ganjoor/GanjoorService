using Microsoft.EntityFrameworkCore;
using System;
using RMuseum.Models.Ganjoor;
using System.Threading.Tasks;
using RSecurityBackend.Models.Generic;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Linq;
using Org.BouncyCastle.Asn1.Ocsp;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// tajik poets
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorTajikPoet[]>> GetTajikPoetsAsync()
        {
            try
            {
                return new RServiceResult<GanjoorTajikPoet[]>(await _context.TajikPoets.AsNoTracking().ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorTajikPoet[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// tajik page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageCompleteViewModel>> GetTajikPageByUrlAsync(string url, bool catPoems = false)
        {
            try
            {
                var res = await GetPageByUrl(url, catPoems);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return res;
                GanjoorPageCompleteViewModel page = res.Result;

                int poetId = page.Poem != null ? page.Poem.Category.Poet.Id : page.PoetOrCat.Poet.Id;

                var tajikPage = await _context.TajikPages.AsNoTracking().Where(p => p.Id == page.Id).SingleOrDefaultAsync();
                if (tajikPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, "Ин сафҳа ҳануз ба тоҷикии баргардонида нашуда.");

                page.HtmlText = tajikPage.TajikHtmlText;

                var tajikPoet = await _context.TajikPoets.AsNoTracking().Where(p => p.Id == poetId).SingleOrDefaultAsync();
                if (tajikPoet == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, "Ин суханвар ҳануз ба тоҷикӣ дар дастрас нест.");
                if(page.PoetOrCat != null && page.PoetOrCat.Poet != null)
                {
                    page.PoetOrCat.Poet.Nickname = tajikPoet.TajikNickname;
                    page.PoetOrCat.Poet.Description = tajikPoet.TajikDescription;
                }

                if(page.Poem != null && page.Poem.Category != null && page.Poem.Category.Poet != null)
                {
                    page.Poem.Category.Poet.Nickname = tajikPoet.TajikNickname;
                    page.Poem.Category.Poet.Description = tajikPoet.TajikDescription;
                }

                int catId = page.Poem != null ? page.Poem.Category.Cat.Id : page.PoetOrCat.Cat.Id;

                var tajikCat = await _context.TajikCats.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                if (page.Poem != null && page.Poem.Category != null && page.Poem.Category.Cat != null)
                {
                    page.Poem.Category.Cat.Title = tajikCat.TajikTitle;
                    page.Poem.Category.Cat.Description = tajikCat.TajikDescription;
                    foreach (var parent in page.Poem.Category.Cat.Ancestors)
                    {
                        var tajikParent = await _context.TajikCats.AsNoTracking().Where(c => c.Id == parent.Id).SingleAsync();
                        parent.Title = tajikParent.TajikTitle;
                        parent.Description = tajikParent.TajikDescription;
                    }
                    foreach (var child in page.Poem.Category.Cat.Children)
                    {
                        var tajikChild = await _context.TajikCats.AsNoTracking().Where(c => c.Id == child.Id).SingleAsync();
                        child.Title = tajikChild.TajikTitle;
                        child.Description = tajikChild.TajikDescription;
                    }
                }

                if (page.PoetOrCat != null && page.PoetOrCat.Cat != null)
                {
                    page.PoetOrCat.Cat.Title = tajikCat.TajikTitle;
                    page.PoetOrCat.Cat.Description = tajikCat.TajikDescription;
                    foreach (var parent in page.PoetOrCat.Cat.Ancestors)
                    {
                        var tajikParent = await _context.TajikCats.AsNoTracking().Where(c => c.Id == parent.Id).SingleAsync();
                        parent.Title = tajikParent.TajikTitle;
                        parent.Description = tajikParent.TajikDescription;
                    }
                    foreach (var child in page.PoetOrCat.Cat.Children)
                    {
                        var tajikChild = await _context.TajikCats.AsNoTracking().Where(c => c.Id == child.Id).SingleAsync();
                        child.Title = tajikChild.TajikTitle;
                        child.Description = tajikChild.TajikDescription;
                    }
                    if (page.PoetOrCat.Cat.Next != null)
                    {
                        var nextCat = await _context.TajikCats.AsNoTracking().Where(c => c.Id == page.PoetOrCat.Cat.Next.Id).SingleAsync();
                        page.PoetOrCat.Cat.Next.Title = nextCat.TajikTitle;
                    }
                    if (page.PoetOrCat.Cat.Previous != null)
                    {
                        var preCat = await _context.TajikCats.AsNoTracking().Where(c => c.Id == page.PoetOrCat.Cat.Previous.Id).SingleAsync();
                        page.PoetOrCat.Cat.Previous.Title = preCat.TajikTitle;
                    }

                    if (page.PoetOrCat.Cat.Poems != null)
                    {
                        var tajikPoems = await _context.TajikPoems.AsNoTracking().Where(p => p.CatId == catId).ToListAsync();
                        foreach (var poem in page.PoetOrCat.Cat.Poems)
                        {
                            var tajikPoem = tajikPoems.Where(p => p.Id == poem.Id).SingleOrDefault();
                            if (tajikPoem != null)
                            {
                                poem.Title = tajikPoem.TajikTitle;
                            }
                        }
                    }
                }

                if (page.Poem != null)
                {
                    var tajikPoem = await _context.TajikPoems.AsNoTracking().Where(p => p.Id == page.Poem.Id).SingleAsync();
                    page.Poem.Title = tajikPoem.TajikTitle;
                    page.Poem.PlainText = tajikPoem.TajikPlainText;


                    var tajikVerses = await _context.TajikVerses.AsNoTracking().Where(v => v.PoemId ==  page.Poem.Id).OrderBy(v => v.VOrder).ToListAsync();
                    foreach( var verse in page.Poem.Verses)
                    {
                        verse.Text = tajikVerses.Where(v => v.VOrder == verse.VOrder).SingleOrDefault().TajikText;
                    }

                    if(page.Poem.Next != null)
                    {
                        var nextPoem = await _context.TajikPoems.AsNoTracking().Where(p => p.Id == page.Poem.Next.Id).SingleAsync();
                        page.Poem.Next.Title = nextPoem.TajikTitle;

                    }
                    if (page.Poem.Previous != null)
                    {
                        var prePoem = await _context.TajikPoems.AsNoTracking().Where(p => p.Id == page.Poem.Previous.Id).SingleAsync();
                        page.Poem.Next.Title = prePoem.TajikTitle;
                    }
                }
                return new RServiceResult<GanjoorPageCompleteViewModel>(page);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPageCompleteViewModel>(null, exp.ToString());
            }
        }
    }
}