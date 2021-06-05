using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Accounting;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using RSecurityBackend.Services.Implementation;
using RSecurityBackend.Services;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// donation service
    /// </summary>
    public class DonationService : IDonationService
    {

        /// <summary>
        /// donation page row
        /// </summary>
        private class DonationPageRow
        {
            /// <summary>
            /// date
            /// </summary>
            public string Date { get; set; }

            /// <summary>
            /// amount
            /// </summary>
            public string Amount { get; set; }

            /// <summary>
            /// donor
            /// </summary>
            public string Donor { get; set; }

            /// <summary>
            /// usage
            /// </summary>
            public string Usage { get; set; }

            /// <summary>
            /// remaining
            /// </summary>
            public string Remaining { get; set; }
        }

        private async Task DoInitializeRecords(RMuseumDbContext context)
        {
            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
            var job = (await jobProgressServiceEF.NewJob("DonationService::DoInitializeRecords", "Processing")).Result;

            try
            {
                string htmlText = await context.GanjoorPages.Where(p => p.UrlSlug == "donate").Select(p => p.HtmlText).AsNoTracking().SingleAsync();

                List<DonationPageRow> rows = new List<DonationPageRow>();

                int nStartIndex = htmlText.IndexOf("<td class=\"ddate\">");
                int rowNumber = 0;
                while (nStartIndex != -1)
                {
                    rowNumber++;
                    DonationPageRow row = new DonationPageRow();

                    nStartIndex += "<td class=\"ddate\">".Length;
                    row.Date = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex) - nStartIndex);

                    nStartIndex = htmlText.IndexOf("<td class=\"damount\">", nStartIndex);
                    if (nStartIndex == -1)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{rowNumber} : damount");
                        return;
                    }
                    nStartIndex += "<td class=\"damount\">".Length;
                    row.Amount = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex) - nStartIndex);

                    nStartIndex = htmlText.IndexOf("<td class=\"ddonator\">", nStartIndex);
                    if (nStartIndex == -1)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{rowNumber} : ddonator");
                        return;
                    }
                    nStartIndex += "<td class=\"ddonator\">".Length;
                    row.Donor = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex) - nStartIndex);

                    nStartIndex = htmlText.IndexOf("<td class=\"dusage\">", nStartIndex);
                    if (nStartIndex == -1)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{rowNumber} : dusage");
                        return;
                    }
                    nStartIndex += "<td class=\"dusage\">".Length;
                    row.Usage = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex) - nStartIndex);

                    nStartIndex = htmlText.IndexOf("<td class=\"drem\">", nStartIndex);
                    if (nStartIndex == -1)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"{rowNumber} : drem");
                        return;
                    }

                    nStartIndex += "<td class=\"drem\">".Length;
                    row.Remaining = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex) - nStartIndex);


                    rows.Add(row);

                    nStartIndex = htmlText.IndexOf("<td class=\"ddate\">", nStartIndex);
                }

                DateTime recordDate = DateTime.Now.AddDays(-2);

                for (int i = rows.Count - 1; i >= 0; i--)
                {
                    GanjoorDonation donation = new GanjoorDonation()
                    {
                        ImportedRecord = true,
                        DateString = rows[i].Date,
                        RecordDate = recordDate,
                        AmountString = rows[i].Amount,
                        DonorName = rows[i].Donor, //needs to be cleaned from href values
                        ExpenditureDesc = rows[i].Usage,
                        Remaining = 0
                    };

                    if (
                        rows[i].Remaining != "۲۰ دلار" //one record
                        &&
                        rows[i].Remaining.ToEnglishNumbers() != "0"
                        )
                    {
                        donation.Remaining = decimal.Parse(rows[i].Remaining.Replace("٬", "").Replace("تومان", "").Trim().ToEnglishNumbers());
                    }

                    if (donation.AmountString.Contains("تومان"))
                    {
                        donation.Amount = decimal.Parse(donation.AmountString.Replace("٬", "").Replace("تومان", "").Trim().ToEnglishNumbers());
                        donation.Unit = "تومان";
                    }

                    if (donation.AmountString.Contains("دلار"))
                    {
                        donation.Amount = decimal.Parse(donation.AmountString.Replace("٬", "").Replace("دلار", "").Trim().ToEnglishNumbers());
                        donation.Unit = "دلار";
                    }

                    if (donation.ExpenditureDesc == "هنوز هزینه نشده.")
                    {
                        donation.ExpenditureDesc = "";
                    }

                    context.GanjoorDonations.Add(donation);

                    await context.SaveChangesAsync(); //in order to make Id columns filled in desired order


                }

                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
            }
            catch (Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
            }
        }

        /// <summary>
        /// parse html of https://ganjoor.net/donate/ and fill the records
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> InitializeRecords()
        {
            if (await _context.GanjoorDonations.AnyAsync())
                return new RServiceResult<bool>(true);

            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                     (
                     async token =>
                     {
                         using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                          {
                             await DoInitializeRecords(context);
                         }

                     }
                     );
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Database Context
        /// </summary>
        private readonly RMuseumDbContext _context;

        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="backgroundTaskQueue"></param>
        public DonationService(RMuseumDbContext context, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;


        }
    }
}
