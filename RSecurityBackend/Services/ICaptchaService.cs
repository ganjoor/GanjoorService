using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Captcha Service
    /// </summary>
    public interface ICaptchaService
    {
        /// <summary>
        /// Generate Captcha
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<RImage>> Generate();

        /// <summary>
        /// evaluate captcha
        /// </summary>
        /// <param name="imageId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> Evaluate(Guid imageId, string value);


    }
}
