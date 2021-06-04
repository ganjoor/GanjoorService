using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Accounting;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DNTPersianUtils.Core;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// donation service
    /// </summary>
    public class DonationService
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

        /// <summary>
        /// parse html of https://ganjoor.net/donate/ and fill the records
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> InitializeRecords()
        {
            if (await _context.GanjoorDonations.AnyAsync())
                return new RServiceResult<bool>(true);

            string htmlText = await _context.GanjoorPages.Where(p => p.UrlSlug == "donate").Select(p => p.HtmlText).AsNoTracking().SingleAsync();

            List<DonationPageRow> rows = new List<DonationPageRow>();

            int nStartIndex = htmlText.IndexOf("<td class=\"ddate\">");
            int rowNumber = 0;
            while(nStartIndex != -1)
            {
                rowNumber++;
                DonationPageRow row = new DonationPageRow();

                nStartIndex += "<td class=\"ddate\">".Length;
                row.Date = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex));

                nStartIndex = htmlText.IndexOf("<td class=\"damount\">", nStartIndex);
                if (nStartIndex == -1)
                    return new RServiceResult<bool>(false, $"{rowNumber} : damount");
                nStartIndex += "<td class=\"damount\">".Length;
                row.Amount = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex));

                nStartIndex = htmlText.IndexOf("<td class=\"ddonator\">", nStartIndex);
                if (nStartIndex == -1)
                    return new RServiceResult<bool>(false, $"{rowNumber} : ddonator");
                nStartIndex += "<td class=\"ddonator\">".Length;
                row.Donor = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex));

                nStartIndex = htmlText.IndexOf("<td class=\"dusage\">", nStartIndex);
                if (nStartIndex == -1)
                    return new RServiceResult<bool>(false, $"{rowNumber} : dusage");
                nStartIndex += "<td class=\"dusage\">".Length;
                row.Usage = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex));

                nStartIndex = htmlText.IndexOf("<td class=\"drem\">", nStartIndex);
                if (nStartIndex == -1)
                    return new RServiceResult<bool>(false, $"{rowNumber} : drem");
                nStartIndex += "<td class=\"drem\">".Length;
                row.Remaining = htmlText.Substring(nStartIndex, htmlText.IndexOf("</td>", nStartIndex));


                rows.Add(row);

                nStartIndex = htmlText.IndexOf("<td class=\"ddate\">", nStartIndex);
            }

            DateTime recordDate = DateTime.Now.AddDays(-2);

            for(int i = rows.Count - 1; i>=0; i--)
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

                if(
                    rows[i].Remaining != "۲۰ دلار" //one record
                    &&
                    rows[i].Remaining.ToEnglishNumbers() != "0"
                    )
                {
                    donation.Remaining = decimal.Parse(rows[i].Remaining.Replace("٬", "").Replace("تومان", "").Trim());
                }

                if(donation.DonorName.IndexOf("href") != -1)
                {
                    nStartIndex = donation.DonorName.IndexOf("href") + "href".Length + 1;
                    donation.DonorLink = donation.DonorName.Substring(nStartIndex, donation.DonorName.IndexOf("</a>") - nStartIndex);
                    donation.DonorName = donation.DonorName.Substring(0, donation.DonorName.IndexOf("<a")) + donation.DonorName.Replace("</a>", "").Substring(donation.DonorName.IndexOf(">") + 1);
                }

                if(donation.AmountString.Contains("تومان"))
                {
                    donation.Amount = decimal.Parse(donation.AmountString.Replace("٬", "").Replace("تومان", "").Trim());
                    donation.Unit = "تومان";
                }

                if (donation.AmountString.Contains("دلار"))
                {
                    donation.Amount = decimal.Parse(donation.AmountString.Replace("٬", "").Replace("دلار", "").Trim());
                    donation.Unit = "دلار";
                }

                if(donation.ExpenditureDesc == "هنوز هزینه نشده.")
                {
                    donation.ExpenditureDesc = "";
                }

                _context.GanjoorDonations.Add(donation);

                await _context.SaveChangesAsync(); //in order to make Id columns filled in desired order


            }

            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Database Context
        /// </summary>
        private readonly RMuseumDbContext _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public DonationService(RMuseumDbContext context)
        {
            _context = context;
            
        }
    }
}
