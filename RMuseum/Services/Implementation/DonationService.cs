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
            var d = new GanjoorDonation()
            {
                DateString = LanguageUtils.FormatDate(donation.RecordDate),
                RecordDate = donation.RecordDate,
                Amount = donation.Amount,
                Unit = donation.Unit,
                AmountString = LanguageUtils.FormatMoney(donation.Amount),
                DonorName = donation.DonorName,
                Remaining = donation.Amount,
                ExpenditureDesc = "",
                ImportedRecord = false
            };

            if (!string.IsNullOrEmpty(donation.Unit))
                d.AmountString = $"{d.AmountString} {d.Unit}";

            _context.GanjoorDonations.Add(d);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(d.Unit) && d.Amount > 0)
            {
                var expenses =
                await _context.GanjoorExpenses
                    .Include(e => e.DonationExpenditures)
                    .Where(e => e.Unit == d.Unit && (e.Amount - e.DonationExpenditures.Sum(x => x.Amount)) > 0)
                    .OrderBy(e => e.Id)
                    .ToListAsync();

                foreach (var expense in expenses)
                {
                    if (d.Remaining <= 0)
                        break;
                    var remaining = expense.Amount - expense.DonationExpenditures.Sum(x => x.Amount);
                    if (remaining > d.Remaining)
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
                    var amount = d.Remaining == d.Amount && remaining == d.Remaining ? "" : d.Remaining == remaining ? "" : $"مبلغ {LanguageUtils.FormatMoney(remaining)} {d.Unit} آن ";
                    var part = expense.DonationExpenditures.Count == 0 && remaining == expense.Amount ? "" : "بخشی از ";
                    if (!string.IsNullOrEmpty(d.ExpenditureDesc))
                        d.ExpenditureDesc += " ";
                    d.ExpenditureDesc += $"{amount}جهت تأمین {part}هزینهٔ {expense.Description} به مبلغ {LanguageUtils.FormatMoney(expense.Amount)} {d.Unit} صرف شد ({LanguageUtils.FormatDate(expense.ExpenseDate)}).";
                    d.Remaining -= remaining;
                    _context.GanjoorDonations.Update(d);
                    await _context.SaveChangesAsync();
                }
            }


            await RegenerateDonationsPage(editingUserId, $"ثبت کمک مالی از {d.DonorName} به مبلغ {d.AmountString}");//ignore possible errors here!

            donation.Id = d.Id;
            donation.DateString = d.DateString;
            donation.AmountString = d.AmountString;
            donation.Remaining = d.Remaining;
            donation.ExpenditureDesc = d.ExpenditureDesc;
            donation.ImportedRecord = d.ImportedRecord;



            return new RServiceResult<GanjoorDonationViewModel>(donation);
        }

        /// <summary>
        /// update donation
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <param name="updateModel"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateDonation(Guid editingUserId, int id, UpdateDateDescriptionViewModel updateModel)
        {
            var donation = await _context.GanjoorDonations.Where(d => d.Id == id).SingleAsync();
            donation.RecordDate = updateModel.Date;
            donation.DateString = LanguageUtils.FormatDate(donation.RecordDate);
            donation.DonorName = updateModel.Description;
            _context.GanjoorDonations.Update(donation);
            await _context.SaveChangesAsync();
            await RegenerateDonationsPage(editingUserId, $"ویرایش کمک مالی از {donation.DonorName} به مبلغ {donation.AmountString}");//ignore possible errors here!
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete donation
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteDonation(Guid editingUserId, int id)
        {
            var donation = await _context.GanjoorDonations.Where(d => d.Id == id).SingleAsync();
            if (donation.ImportedRecord)
            {
                var sumExpenditures = await _context.DonationExpenditure.AsNoTracking().Where(x => x.GanjoorDonationId == id).SumAsync(x => x.Amount);
                if (sumExpenditures != (donation.Amount - donation.Remaining))
                {
                    return new RServiceResult<bool>(false, "حذف این ردیف از طریق API امکان ندارد.");
                }

            }
            string note = $"حذف کمک مالی از {donation.DonorName} به مبلغ {donation.AmountString}";
            _context.GanjoorDonations.Remove(donation);
            await _context.SaveChangesAsync();

            await RegenerateDonationsPage(editingUserId, note);//ignore possible errors here!

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// new expense
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="expense"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorExpense>> AddExpense(Guid editingUserId, GanjoorExpense expense)
        {
            expense.DonationExpenditures = new List<DonationExpenditure>();//fix swagger posting a list which causes a donation to be added
            _context.GanjoorExpenses.Add(expense);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(expense.Unit) && expense.Amount > 0)
            {
                var expenseRemaining = expense.Amount;

                var donations = await _context.GanjoorDonations.Where(d => d.Unit == expense.Unit && d.Remaining > 0).OrderBy(d => d.Id).ToListAsync();
                foreach (var d in donations)
                {
                    if (expenseRemaining <= 0)
                        break;

                    var remaining = expenseRemaining;
                    if (remaining > d.Remaining)
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

                    var amount = d.Remaining == d.Amount && remaining == d.Remaining ? "" : d.Remaining == remaining ? "باقیماندهٔ آن " : $"مبلغ {LanguageUtils.FormatMoney(remaining)} {d.Unit} آن ";
                    var part = expense.DonationExpenditures.Count == 1 && remaining == expenseRemaining ? "" : "بخشی از ";
                    if (!string.IsNullOrEmpty(d.ExpenditureDesc))
                        d.ExpenditureDesc += " ";
                    d.ExpenditureDesc += $"{amount}جهت تأمین {part}هزینهٔ {expense.Description} به مبلغ {LanguageUtils.FormatMoney(expense.Amount)} {d.Unit} صرف شد ({LanguageUtils.FormatDate(expense.ExpenseDate)}).";
                    d.Remaining -= remaining;
                    _context.GanjoorDonations.Update(d);


                    await _context.SaveChangesAsync();

                    expenseRemaining -= remaining;
                }
            }

            await RegenerateDonationsPage(editingUserId, $"ثبت هزینهٔ {expense.Description} به مبلغ {LanguageUtils.FormatMoney(expense.Amount)} {expense.Unit}");//ignore possible errors here!

            return new RServiceResult<GanjoorExpense>(expense);
        }

        /// <summary>
        /// update expense
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <param name="updateModel"></param>
        /// <returns></returns>

        public async Task<RServiceResult<bool>> UpdateExpense(Guid editingUserId, int id, UpdateDateDescriptionViewModel updateModel)
        {
            var expense = await _context.GanjoorExpenses.Include(e => e.DonationExpenditures).Where(d => d.Id == id).SingleAsync();
            List<GanjoorDonation> donations = new List<GanjoorDonation>();
            foreach (var expenditure in expense.DonationExpenditures)
            {
                if (!donations.Where(d => d.Id == expenditure.GanjoorDonationId).Any())
                {
                    donations.Add(await _context.GanjoorDonations.Where(d => d.Id == expenditure.GanjoorDonationId).SingleAsync());
                }
            }
            expense.ExpenseDate = updateModel.Date;
            expense.Description = updateModel.Description;
            _context.GanjoorExpenses.Update(expense);
            await _context.SaveChangesAsync();

            await RegenerateDonationsExpenditureDesc(donations);

            await RegenerateDonationsPage(editingUserId, $"ویرایش هزینهٔ {expense.Description} به مبلغ {LanguageUtils.FormatMoney(expense.Amount)} {expense.Unit}");//ignore possible errors here!

            return new RServiceResult<bool>(true);

        }

        private async Task RegenerateDonationsExpenditureDesc(List<GanjoorDonation> donations)
        {
            if (donations.Count > 0)
            {
                foreach (var donation in donations)
                {
                    donation.Remaining = donation.Amount;
                    donation.ExpenditureDesc = "";

                    var donationExpenses = await _context.GanjoorExpenses.AsNoTracking()
                                                                            .Include(e => e.DonationExpenditures)
                                                                            .Where(e => e.DonationExpenditures
                                                                            .Any(x => x.GanjoorDonationId == donation.Id))
                                                                            .ToListAsync();

                    foreach (var donationExpense in donationExpenses)
                        foreach (var donationExpenditure in donationExpense.DonationExpenditures)
                            if (donationExpenditure.GanjoorDonationId == donation.Id)
                            {
                                var amount = donation.Remaining == donation.Amount && donationExpenditure.Amount == donation.Remaining ? "" : donation.Remaining == donationExpenditure.Amount ? "باقیماندهٔ آن " : $"مبلغ {LanguageUtils.FormatMoney(donationExpenditure.Amount)} {donation.Unit} آن ";
                                var part = donationExpense.DonationExpenditures.Count == 1 && donationExpenditure.Amount == donationExpense.Amount ? "" : "بخشی از ";
                                if (!string.IsNullOrEmpty(donation.ExpenditureDesc))
                                    donation.ExpenditureDesc += " ";
                                donation.ExpenditureDesc += $"{amount}جهت تأمین {part}هزینهٔ {donationExpense.Description} به مبلغ {LanguageUtils.FormatMoney(donationExpense.Amount)} {donation.Unit} صرف شد ({LanguageUtils.FormatDate(donationExpense.ExpenseDate)}).";
                                donation.Remaining -= donationExpenditure.Amount;
                            }

                    _context.GanjoorDonations.Update(donation);
                }

                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// delete expense
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteExpense(Guid editingUserId, int id)
        {
            var expense = await _context.GanjoorExpenses.Include(e => e.DonationExpenditures).Where(e => e.Id == id).SingleAsync();
            string note = $"حذف هزینهٔ {expense.Description} به مبلغ {LanguageUtils.FormatMoney(expense.Amount)} {expense.Unit}";

            List<GanjoorDonation> donations = new List<GanjoorDonation>();
            foreach (var expenditure in expense.DonationExpenditures)
            {
                if (!donations.Where(d => d.Id == expenditure.GanjoorDonationId).Any())
                {
                    donations.Add(await _context.GanjoorDonations.Where(d => d.Id == expenditure.GanjoorDonationId).SingleAsync());
                }

                _context.DonationExpenditure.Remove(expenditure);
            }

            await _context.SaveChangesAsync();

            _context.GanjoorExpenses.Remove(expense);
            await _context.SaveChangesAsync();

            await RegenerateDonationsExpenditureDesc(donations);

            await RegenerateDonationsPage(editingUserId, note);//ignore possible errors here!

            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// returns all donations
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorDonationViewModel[]>> GetDonations()
        {
            return new RServiceResult<GanjoorDonationViewModel[]>
                (
                await _context.GanjoorDonations
                              .AsNoTracking()
                              .OrderByDescending(d => d.Id)
                              .Select
                              (
                                d =>
                                new GanjoorDonationViewModel()
                                {
                                    Id = d.Id,
                                    Amount = d.Amount,
                                    AmountString = d.AmountString,
                                    DateString = d.DateString,
                                    DonorName = d.DonorName,
                                    ExpenditureDesc = d.ExpenditureDesc,
                                    ImportedRecord = d.ImportedRecord,
                                    RecordDate = d.RecordDate,
                                    Remaining = d.Remaining,
                                    Unit = d.Unit
                                }
                             ).ToArrayAsync()

                );
        }

        /// <summary>
        /// get donation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorDonationViewModel>> GetDonation(int id)
        {
            return new RServiceResult<GanjoorDonationViewModel>
                (
                await _context.GanjoorDonations
                              .AsNoTracking()
                              .Where(d => d.Id == id)
                              .Select
                              (
                                d =>
                                new GanjoorDonationViewModel()
                                {
                                    Id = d.Id,
                                    Amount = d.Amount,
                                    AmountString = d.AmountString,
                                    DateString = d.DateString,
                                    DonorName = d.DonorName,
                                    ExpenditureDesc = d.ExpenditureDesc,
                                    ImportedRecord = d.ImportedRecord,
                                    RecordDate = d.RecordDate,
                                    Remaining = d.Remaining,
                                    Unit = d.Unit
                                }
                             ).SingleOrDefaultAsync()

                );
        }

        /// <summary>
        /// returns all expenses
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorExpense[]>> GetExpenses()
        {
            return new RServiceResult<GanjoorExpense[]>
                (
                await _context.GanjoorExpenses.Include(x => x.DonationExpenditures)
                              .AsNoTracking()
                              .OrderByDescending(x => x.Id)
                              .ToArrayAsync()

                );
        }

        /// <summary>
        /// get expense by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorExpense>> GetExpense(int id)
        {
            return new RServiceResult<GanjoorExpense>
                (
                await _context.GanjoorExpenses.Include(x => x.DonationExpenditures)
                              .AsNoTracking()
                              .Where(e => e.Id == id)
                              .SingleOrDefaultAsync()

                );
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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// regenerate donations page
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RegenerateDonationsPage(Guid editingUserId, string note)
        {

            var dbPage = await _context.GanjoorPages.Where(p => p.FullUrl == "/donate").SingleAsync();

            var donations = await _context.GanjoorDonations.OrderByDescending(d => d.Id).ToArrayAsync();

            var remSum = await _context.GanjoorDonations.Where(d => d.Unit == "تومان").SumAsync(d => d.Remaining);


            string htmlText = "";

            DateTime dateLastDonation = donations.Length > 0 ? donations[0].RecordDate : DateTime.MinValue;
            DateTime dateLastExpense = DateTime.MinValue;
            var lastExpense = await _context.GanjoorExpenses.OrderByDescending(e => e.ExpenseDate).FirstOrDefaultAsync();
            if (lastExpense != null)
            {
                dateLastExpense = lastExpense.ExpenseDate;
            }



            if (ShowAccountInfo)
            {
                htmlText += $"<div class=\"notice\">{Environment.NewLine}";
                htmlText += $"<p>{Environment.NewLine}";
                htmlText += $"در صورت تمایل به کمک مالی به گنجور از طریق کارت عابربانک؛ لطفاً کمکهای خود را به کارت شمارهٔ <span class=\"lft\">6219-8610-2780-4979</span> (بانک سامان) به نام حمیدرضا محمدی واریز نمایید. علاوه بر آن از طریق اینترنت‌بانک سامان می‌توانید کمکهای خود را به شماره حساب <span class=\"lft\">۸۲۸-۸۰۰-۸۷۳۳۳۰-۱</span> (شمارهٔ شبا: <span class=\"lft\">IR03-0560-0828-8000-0873-3300-01</span>) واریز نمایید. لطفاً از طریق تماس با نشانی ganjoor@ganjoor.net مشخصات خودتان و مبلغ واریزی را اطلاع دهید (نام کمک دهندگان و نوع استفاده‌ای که از کمک آنها شده به مرور در همین صفحه به اطلاع خواهد رسید).{Environment.NewLine}";
                htmlText += $"</p>{Environment.NewLine}";
                htmlText += $"<p>{Environment.NewLine}";
                htmlText += $"مبالغ واریزی جهت پرداخت هزینه‌های جاری (میزبانی وب و ...)، گسترش امکانات و همینطور پایگاه داده‌های سایت مورد استفاده قرار خواهد گرفت.{Environment.NewLine}";
                htmlText += $"</p>{Environment.NewLine}";
                htmlText += $"</div>{Environment.NewLine}";
            }
            else
            {
                htmlText += $"<div class=\"notice\"><p>با سپاس از بزرگواری همهٔ دوستان، در حال حاضر هزینه‌های جاری گنجور تا چند ماه آینده تأمین شده است و جهت جلوگیری از ایجاد بدهی انباشته برای دریافتی‌های فاقد محل هزینه کمک جدید نمی‌پذیریم. خواهشمندیم در صورت امکان کمکهای خود را به ماههای آینده محول فرمایید. اطلاعات واریز در مقطعی که نیاز باشد مجدداً در دسترس قرار خواهد گرفت.</p></div>{Environment.NewLine}";
            }


            htmlText += $"<p>{Environment.NewLine}";

            DateTime last = dateLastDonation > dateLastExpense ? dateLastDonation : dateLastExpense;

            var dateString = LanguageUtils.FormatDate(last);
            htmlText += $"باقیماندهٔ قابل هزینهٔ کمکهای دریافتی تا {dateString} برابر {LanguageUtils.FormatMoney(remSum)} تومان است.{Environment.NewLine}";
            htmlText += $"</p>{Environment.NewLine}";

            var expenses =
                await _context.GanjoorExpenses
                    .Include(e => e.DonationExpenditures)
                    .Where(e => !string.IsNullOrEmpty(e.Unit) && (e.Amount - e.DonationExpenditures.Sum(x => x.Amount)) > 0)
                    .OrderBy(e => e.Id)
                    .ToListAsync();
            if (expenses.Count > 0)
            {
                htmlText += $"<h3>هزینه‌های پوشش داده نشده</h3>{Environment.NewLine}";

                htmlText += $"<p>کمکهای آتی دریافتی صرف پوشش هزینه‌های زیر خواهد شد:</p>{Environment.NewLine}";

                htmlText += $"<table>{Environment.NewLine}";

                htmlText += $"<tr class=\"h\">{Environment.NewLine}";
                htmlText += $"<td class=\"d1\">ردیف</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d2\">تاریخ</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d3\">مبلغ</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d4\">عنوان</td>{Environment.NewLine}";
                htmlText += $"<td class=\"d6\">مانده</td>{Environment.NewLine}";
                htmlText += $"</tr>{Environment.NewLine}";

                for (int i = 0; i < expenses.Count; i++)
                {
                    var expense = expenses[i];

                    string cssClass = i % 2 == 0 ? " class=\"e\"" : "";


                    var expenseRem = expense.Amount;
                    if (expense.DonationExpenditures.Count > 0)
                        expenseRem -= expense.DonationExpenditures.Sum(x => x.Amount);

                    htmlText += $"<tr{cssClass}>{Environment.NewLine}";
                    htmlText += $"<td class=\"drow\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"ddate\">{LanguageUtils.FormatDate(expense.ExpenseDate)}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"damount\">{LanguageUtils.FormatMoney(expense.Amount)} {expense.Unit}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"ddonator\">{expense.Description}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"drem\">{LanguageUtils.FormatMoney(expenseRem)} {expense.Unit}</td>{Environment.NewLine}";
                    htmlText += $"</tr>{Environment.NewLine}";
                }


                htmlText += $"</table>{Environment.NewLine}";
            }

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



            for (int i = 0; i < donations.Length; i++)
            {
                var donation = donations[i];

                string cssClass = i % 2 == 0 ? " class=\"e\"" : "";

                htmlText += $"<tr{cssClass}>{Environment.NewLine}";
                htmlText += $"<td class=\"drow\">{(donations.Length - i).ToPersianNumbers()}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"ddate\">{donation.DateString}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"damount\">{donation.AmountString}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"ddonator\">{donation.DonorName}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"dusage\">{(string.IsNullOrEmpty(donation.ExpenditureDesc) ? "هنوز هزینه نشده." : donation.ExpenditureDesc)}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"drem\">{(donation.Remaining == 0 || string.IsNullOrEmpty(donation.Unit) ? LanguageUtils.FormatMoney(donation.Remaining) : $"{LanguageUtils.FormatMoney(donation.Remaining)} {donation.Unit}")}</td>{Environment.NewLine}";
                htmlText += $"</tr>{Environment.NewLine}";

            }

            htmlText += $"</table>{Environment.NewLine}";

            await _ganjoorService.UpdatePageAsync(dbPage.Id, editingUserId,
                new GanjoorModifyPageViewModel()
                {
                    Title = dbPage.Title,
                    HtmlText = htmlText,
                    Note = note,
                    UrlSlug = dbPage.UrlSlug,
                    NoIndex = dbPage.NoIndex,
                }
                );

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Show Donating Information (temporary switch off/on)
        /// </summary>
        public bool ShowAccountInfo
        {
            get
            {
                try
                {
                    return bool.Parse(_configuration.GetSection("Donations")["ShowAccountInfo"]);
                }
                catch
                {
                    return true;
                }
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
