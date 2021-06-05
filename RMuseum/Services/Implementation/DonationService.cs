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
using Microsoft.Extensions.Configuration;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Globalization;
using RMuseum.Models.Accounting.ViewModels;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// donation service
    /// </summary>
    public class DonationService : IDonationService
    {

        /// <summary>
        /// new donation
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="donation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorDonationViewModel>> AddDonation(Guid editingUserId, GanjoorDonationViewModel donation)
        {
            try
            {
                var d = new GanjoorDonation()
                {
                    DateString = $"{donation.RecordDate.ToPersianYearMonthDay().Day.ToPersianNumbers()}م {PersianCulture.GetPersianMonthName(donation.RecordDate.ToPersianYearMonthDay().Month)} {donation.RecordDate.ToPersianYearMonthDay().Year.ToPersianNumbers()}",
                    RecordDate = donation.RecordDate,
                    Amount = donation.Amount,
                    Unit = donation.Unit,
                    AmountString = donation.Amount.ToString("N0", new CultureInfo("fa-IR")).ToPersianNumbers(),
                    DonorName = donation.DonorName,
                    Remaining = donation.Amount,
                    ExpenditureDesc = "",
                    ImportedRecord = false
                };

                if (!string.IsNullOrEmpty(donation.Unit))
                    d.AmountString = $"{d.AmountString} {d.Unit}";

                _context.GanjoorDonations.Add(d);
                await _context.SaveChangesAsync();

                if(d.Unit == "تومان")
                {
                    var expenses =
                    await _context.GanjoorExpenses
                        .Include(e => e.DonationExpenditures)
                        .Where(e => (e.Amount - e.DonationExpenditures.DefaultIfEmpty().Sum(x => x.Amount)) > 0)
                        .OrderBy(e => e.Id)
                        .ToListAsync();

                    foreach (var expense in expenses)
                    {
                        if (d.Remaining <= 0)
                            break;
                        var remaining = expense.Amount - expense.DonationExpenditures.DefaultIfEmpty().Sum(x => x.Amount);
                        if(remaining > d.Remaining)
                        {
                            remaining = d.Remaining;
                        }

                        DonationExpenditure n = new DonationExpenditure()
                        {
                            Amount = remaining,
                            GanjoorDonationId = d.Id
                        };

                        expense.DonationExpenditures.Add(n);
                        _context.GanjoorExpenses.Update(expense);
                        var part = expense.DonationExpenditures.Count == 0 && remaining == d.Amount ? "" : "بخشی از ";
                        if (!string.IsNullOrEmpty(d.ExpenditureDesc))
                            d.ExpenditureDesc += " ";
                        d.ExpenditureDesc += $"جهت تأمین {part}هزینهٔ {expense.Description} به مبلغ {expense.Amount.ToString("N0", new CultureInfo("fa-IR")).ToPersianNumbers()} تومان صرف شد.";
                        d.Remaining -= remaining;
                        _context.GanjoorDonations.Update(d);
                        await _context.SaveChangesAsync();
                    }
                }


                await RegenerateDonationsPage(editingUserId);//ignore possible errors here!

                donation.Id = d.Id;
                donation.DateString = d.DateString;
                donation.AmountString = d.AmountString;
                donation.Remaining = d.Remaining;
                donation.ExpenditureDesc = d.ExpenditureDesc;
                donation.ImportedRecord = d.ImportedRecord;



                return new RServiceResult<GanjoorDonationViewModel>(donation);
            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorDonationViewModel>(null, exp.ToString());
            }
        }

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
        /// regenerate donations page
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RegenerateDonationsPage(Guid editingUserId)
        {
            try
            {

                var dbPage = await _context.GanjoorPages.Where(p => p.UrlSlug == "donate").SingleAsync();

                var donations = await _context.GanjoorDonations.OrderByDescending(d => d.Id).ToArrayAsync();

                var remSum = await _context.GanjoorDonations.Where(d => d.Unit == "تومان").SumAsync(d => d.Remaining);


                string htmlText = "";

                htmlText += $"<p>{Environment.NewLine}";
                htmlText += $"در صورت تمایل به کمک مالی به گنجور از طریق کارت عابربانک؛ لطفاً کمکهای خود را به کارت شمارهٔ <span class=\"lft\">6219-8610-2780-4979</span> (بانک سامان) به نام حمیدرضا محمدی واریز نمایید. علاوه بر آن از طریق اینترنت‌بانک سامان می‌توانید کمکهای خود را به شماره حساب <span class=\"lft\">۸۲۸-۸۰۰-۸۷۳۳۳۰-۱</span> (شمارهٔ شبا: <span class=\"lft\">IR03-0560-0828-8000-0873-3300-01</span>) واریز نمایید. لطفاً از طریق تماس با نشانی ganjoor@ganjoor.net مشخصات خودتان و مبلغ واریزی را اطلاع دهید (نام کمک دهندگان و نوع استفاده‌ای که از کمک آنها شده به مرور در همین صفحه به اطلاع خواهد رسید).{Environment.NewLine}";
                htmlText += $"</p>{Environment.NewLine}";
                htmlText += $"<p>{Environment.NewLine}";
                htmlText += $"مبالغ واریزی جهت پرداخت هزینه‌های جاری (میزبانی وب و ...)، گسترش امکانات و همینطور پایگاه داده‌های سایت مورد استفاده قرار خواهد گرفت.{Environment.NewLine}";
                htmlText += $"</p>{Environment.NewLine}";
                htmlText += $"<p>{Environment.NewLine}";
                htmlText += $"باقیماندهٔ قابل هزینهٔ کمکهای دریافتی تا {donations[0].DateString} برابر {remSum.ToString("N0", new CultureInfo("fa-IR")).ToPersianNumbers()} تومان است.{Environment.NewLine}";
                htmlText += $"</p>{Environment.NewLine}";

                htmlText += $"<h3>کمکهای دریافت شده تا به حال</h3>{Environment.NewLine}";

                htmlText += $"<table>{Environment.NewLine}";

                htmlText += $"<tr class=\"h\">{Environment.NewLine}";
                htmlText += $"<td class=\"d1\">ردیف</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d2\">تاریخ</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d3\">مبلغ</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d4\">اهدا کننده</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d5\">محل هزینه</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d6\">مانده</td>{Environment.NewLine}";
                htmlText += $"</tr>{Environment.NewLine}";



                for (int i=0; i<donations.Length; i++)  
                {
                    var donation = donations[i];

                    string cssClass = i % 2 == 0 ? " class=\"e\"" : "";

                    htmlText += $"<tr{cssClass}>{Environment.NewLine}";
                    htmlText += $"<td class=\"drow\">{(donations.Length - i).ToPersianNumbers()}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"ddate\">{donation.DateString}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"damount\">{donation.AmountString}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"ddonator\">{donation.DonorName}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"dusage\">{(string.IsNullOrEmpty(donation.ExpenditureDesc) ? "هنوز هزینه نشده." : donation.ExpenditureDesc)}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"drem\">{(donation.Remaining == 0 || string.IsNullOrEmpty(donation.Unit) ? donation.Remaining.ToString("N0", new CultureInfo("fa-IR")).ToPersianNumbers() : $"{donation.Remaining.ToString("N0", new CultureInfo("fa-IR")).ToPersianNumbers()} {donation.Unit}")}</td>{Environment.NewLine}";
                    htmlText += $"</tr>{Environment.NewLine}";

                }

                htmlText += $"</table>{Environment.NewLine}";

                await _ganjoorService.ModifyPage(dbPage.Id, editingUserId,
                    new GanjoorModifyPageViewModel()
                    {
                        Title = dbPage.Title,
                        HtmlText = htmlText,
                        Note = "تولید خودکار صفحهٔ کمکهای مالی",
                        UrlSlug = dbPage.UrlSlug,
                    }
                    );

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
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
        /// Ganjoor Service
        /// </summary>
        private readonly IGanjoorService _ganjoorService;

        /// <summary>
        /// configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="ganjoorService"></param>
        /// <param name="configuration"></param>
        public DonationService(RMuseumDbContext context, IBackgroundTaskQueue backgroundTaskQueue, IGanjoorService ganjoorService, IConfiguration configuration)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;
            _ganjoorService = ganjoorService;
            _configuration = configuration;
        }
    }
}
