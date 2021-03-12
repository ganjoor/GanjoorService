using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// Ganjoor User Service
    /// </summary>
    public class GanjoorAppUserService : AppUserService
    {
        public GanjoorAppUserService(
            RMuseumDbContext context,
            UserManager<RAppUser> userManager,
            SignInManager<RAppUser> signInManager,
            RoleManager<RAppRole> roleManager,
            ISecretGenerator secretGenerator,
            IImageFileService imageFileService,
            IUserRoleService userRoleService,
            IConfiguration configuration)
            : base(context, userManager, signInManager, roleManager, secretGenerator, imageFileService, userRoleService, configuration)
        {
           
        }

        /// <summary>
        /// finalize signup and assign his or him comments to him or her
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="sureName"></param>
        /// <returns></returns>
        public override async Task<RServiceResult<bool>> FinalizeSignUp(string email, string secret, string password, string firstName, string sureName)
        {
            RServiceResult<bool> res = await base.FinalizeSignUp(email, secret, password, firstName, sureName);
            if(res.Result)
            {
                try
                {
                    RMuseumDbContext context = _context as RMuseumDbContext;
                    var comments = await context.GanjoorComments.Where(c => c.AuthorEmail.Equals(email, StringComparison.InvariantCultureIgnoreCase)).ToListAsync();
                    if (comments.Count > 0)
                    {
                        var user = (await FindUserByEmail(email)).Result;
                        foreach (var comment in comments)
                        {
                            comment.UserId = user.Id;
                        }
                        _context.UpdateRange(comments);
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    return new RServiceResult<bool>(true); //ignore this error! because signup was succesful
                }
            }
            return res;
        }
    }
}
