using ganjoor;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RMuseum.Models.PDFLibrary;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace RMuseum.Services.Implementation
{
    public partial class PDFLibraryService
    {
        /// <summary>
        /// queued downloding pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, QueuedPDFBook[] Books)>> GetQueuedPDFBooksAsync(PagingParameterModel paging)
        {
            try
            {
                var source =
                _context.QueuedPDFBooks.AsNoTracking()
               .OrderBy(t => t.Id)
               .AsQueryable();
                (PaginationMetadata PagingMeta, QueuedPDFBook[] Books) paginatedResult =
                    await QueryablePaginator<QueuedPDFBook>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, QueuedPDFBook[] Books)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, QueuedPDFBook[] Books)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// delete queued books
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteQueuedPDFBookAsync(Guid id)
        {
            try
            {
                var qb = await _context.QueuedPDFBooks.Where(t => t.Id == id).SingleAsync();
                _context.Remove(qb);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// mix queued pdf books 
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> MixQueuedPDFBooksAsync(int step)
        {
            try
            {
                var qSoha = await _context.QueuedPDFBooks.Where(t => t.OriginalSourceUrl.Contains("https://sohalibrary.com")).OrderBy(t => t.DownloadOrder).ToListAsync();
                var qElit = await _context.QueuedPDFBooks.Where(t => !t.OriginalSourceUrl.Contains("https://sohalibrary.com")).OrderBy(t => t.DownloadOrder).ToListAsync();

                int downloadOrder = 0;
                int e = 0;
                for (var i = 0; i < qSoha.Count; i++)
                {
                    qSoha[i].DownloadOrder = downloadOrder;
                    downloadOrder++;
                    if (i % step == 0)
                    {
                        if (e < qElit.Count)
                        {
                            qElit[e].DownloadOrder = downloadOrder;
                            downloadOrder++;
                            e++;
                        }
                    }
                }

                for (var i = e; e < qElit.Count; e++)
                {
                    qElit[e].DownloadOrder = downloadOrder;
                    downloadOrder++;
                }

                _context.UpdateRange(qSoha);
                _context.UpdateRange(qElit);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// start processing queue pdf books
        /// </summary>
        public void StartProcessingQueuedPDFBooks()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                      (
                          async token =>
                          {
                              using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                              {
                                  var q = await context.QueuedPDFBooks.AsNoTracking().OrderBy(i => i.DownloadOrder).ToListAsync();
                                  foreach (var item in q)
                                  {
                                      await StartImportingKnownSourceAsync(item.OriginalSourceUrl);
                                  }
                              }
                          }
                      );
        }
    }
}
