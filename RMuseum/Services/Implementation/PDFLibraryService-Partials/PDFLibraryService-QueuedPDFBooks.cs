using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
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
               .OrderBy(t => t.DownloadOrder)
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
                var processed = await _context.QueuedPDFBooks.Where(t => t.ResultId != 0).ToListAsync();
                if(processed.Count > 0)
                {
                    _context.RemoveRange(processed);
                    await _context.SaveChangesAsync();
                }
                var qSoha = await _context.QueuedPDFBooks.Where(t => t.OriginalSourceUrl.Contains("https://sohalibrary.com")).OrderBy(t => t.DownloadOrder).ToListAsync();
                var qElit = await _context.QueuedPDFBooks.Where(t => !t.OriginalSourceUrl.Contains("https://sohalibrary.com")).OrderBy(t => t.DownloadOrder).ToListAsync();

                int downloadOrder = 0;
                int e = 0;
                for (var i = 0; i < qSoha.Count; i++)
                {
                    qSoha[i].DownloadOrder = downloadOrder;
                    qSoha[i].Processed = false;
                    downloadOrder++;
                    if (i % step == 0)
                    {
                        if (e < qElit.Count)
                        {
                            qElit[e].DownloadOrder = downloadOrder;
                            qElit[e].Processed = false;
                            downloadOrder++;
                            e++;
                        }
                    }
                }

                for (var i = e; e < qElit.Count; e++)
                {
                    qElit[e].DownloadOrder = downloadOrder;
                    qElit[e].Processed = false;
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
        /// <param name="count"></param>
        public void StartProcessingQueuedPDFBooks(int count)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                      (
                          async token =>
                          {
                              using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                              {
                                  var jobs = await context.ImportJobs.ToListAsync();
                                  context.RemoveRange(jobs);
                                  await context.SaveChangesAsync();
                                  var q = await context.QueuedPDFBooks.Where(i => i.Processed == false).OrderBy(i => i.DownloadOrder).Take(count).ToListAsync();
                                  foreach (var item in q)
                                  {
                                      var res = await ImportfFromKnownSourceAsync(context, item.OriginalSourceUrl);
                                      item.Processed = true;
                                      item.ProcessResult = res.ExceptionString;
                                      item.ResultId = res.Result;
                                      context.Update(item);
                                      await context.SaveChangesAsync();
                                  }
                              }
                          }
                      );
        }
    }
}
