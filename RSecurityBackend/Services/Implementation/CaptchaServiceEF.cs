using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Captcha Service
    /// </summary>

    public class CaptchaServiceEF : ICaptchaService
    {

        private const int _CaptchaWidth = 50;

        private const int _CaptchaHeight = 30;

        private const string _FontName = "Tahoma";

        private const float _FontSize = 12.0f;

        /// <summary>
        /// Generate Captcha
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> Generate()
        {
            try
            {
                string value = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(5, '0');
                using (Image img = new Bitmap(_CaptchaWidth, _CaptchaHeight))
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        g.FillRectangle(Brushes.White, 0, 0, _CaptchaWidth, _CaptchaHeight);
                        using (Font fnt = new Font(_FontName, _FontSize))
                        {
                            SizeF sz = g.MeasureString(value, fnt);

                            g.DrawString(value, fnt, Brushes.Black, new PointF((_CaptchaWidth - sz.Width) / 2, (_CaptchaHeight - sz.Height) / 2));
                        }
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, ImageFormat.Png);

                        ms.Position = 0;
                        RServiceResult<RImage> image = await _imageFileService.Add(null, ms, $"{$"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(5, '0')}-{Guid.NewGuid()}.png", "Captcha");

                        if (!string.IsNullOrEmpty(image.ExceptionString))
                        {
                            return new RServiceResult<RImage>(null, image.ExceptionString);
                        }

                        RCaptchaImage captcha = new RCaptchaImage()
                        {
                            Value = value,
                            DateTime = DateTime.Now,
                            RImage = image.Result
                        };

                        await _context.CaptchaImages.AddAsync(captcha);
                        await _context.SaveChangesAsync();

                        return image;
                    }
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<RImage>(null, exp.ToString());
            }
        }

        /// <summary>
        /// evaluate captcha
        /// </summary>
        /// <param name="imageId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> Evaluate(Guid imageId, string value)
        {
            try
            {
                RCaptchaImage captcha  = await _context.CaptchaImages.Where(c => c.RImageId == imageId).FirstOrDefaultAsync();
                if(captcha == null)
                    return new RServiceResult<bool>(false, "Captcha not found");
                bool bRes = captcha.Value.ToLower() == value.ToLower();
                if(bRes)
                {
                    _context.CaptchaImages.Remove(captcha);
                    await _context.SaveChangesAsync();

                    //we can clean up OLD captcha images using a background service later
                    //await _imageFileService.DeleteImage(imageId);//might fail, does not matter
                }
                return new RServiceResult<bool>(bRes);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

        }

        /// <summary>
        /// Image File Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="imageFileService"></param>
        public CaptchaServiceEF(RSecurityDbContext<RAppUser, RAppRole, Guid> context, IImageFileService imageFileService)
        {
            _context = context;
            _imageFileService = imageFileService;
        }
    }
}
