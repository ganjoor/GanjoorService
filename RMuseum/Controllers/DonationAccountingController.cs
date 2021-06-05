using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Accounting;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/donations")]
    public class DonationAccountingController : Controller
    {
        /// <summary>
        /// add donation + regenerate donations page
        /// </summary>
        /// <param name="donation"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Donations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorDonationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddDonation([FromBody] GanjoorDonationViewModel donation)
        {
            try
            {
                Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

                var res = await _donationService.AddDonation(userId, donation);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok(res.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// one time import
        /// </summary>
        /// <returns></returns>
        [HttpPost("onetimeimport")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Donations)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Import()
        {
            try
            {
                var res = await _donationService.InitializeRecords();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// regenerate donations page
        /// </summary>
        /// <returns></returns>
        [HttpPut("page")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Donations)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RegenerateDonationsPage()
        {
            try
            {
                Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

                var res = await _donationService.RegenerateDonationsPage(userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// donation service
        /// </summary>
        private readonly IDonationService _donationService;

        public DonationAccountingController(IDonationService donationService)
        {
            _donationService = donationService;
        }
    }
}
