using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Audit.Db;
using Microsoft.Extensions.Configuration;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Authentication Service
    /// </summary>
    public class AppUserService : IAppUserService
    {    

        /// <summary>
        /// Login user, if failed return LoggedOnUserModel is null
        /// </summary>
        /// <param name="loginViewModel"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public async Task<RServiceResult<LoggedOnUserModel>> Login(LoginViewModel loginViewModel, string clientIPAddress)
        {
            try
            {

                //we ignore loginViewModel in automatic auditing to prevent logging password data, so we would add a manual auditing to have enough data on login intrusion and ...
                REvent log = new REvent()
                {
                    EventType = "AppUser/Login (POST)(Manual)",
                    StartDate = DateTime.UtcNow,
                    UserName = loginViewModel.Username,
                    IpAddress = clientIPAddress
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();

                RServiceResult<bool> checkUserExists = await EnsureDefaultUserExists();
                if(!checkUserExists.Result)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, checkUserExists.ExceptionString);
                }



                RAppUser appUser = await _userManager.FindByNameAsync(loginViewModel.Username);

                if (appUser == null)
                {
                    appUser = await _userManager.FindByEmailAsync(loginViewModel.Username);
                    if(appUser == null)
                    {
                        return new RServiceResult<LoggedOnUserModel>(null, "Invalid username.");
                    }                    
                }

                var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginViewModel.Password, true);
                if (!result.Succeeded)
                {                    
                    return new RServiceResult<LoggedOnUserModel>(null, "Invalid password or other error. Identity error details says: " + result.ToString());
                }

                if (appUser.Status == RAppUserStatus.Inactive)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "User is disabled by an admin.");
                }

                RServiceResult<SecurableItem[]> securableItems = await GetUserSecurableItemsStatus(appUser.Id);
                if (!string.IsNullOrEmpty(securableItems.ExceptionString))
                    return new RServiceResult<LoggedOnUserModel>(null, securableItems.ExceptionString);

                RTemporaryUserSession userSession =
                    new RTemporaryUserSession()
                    {
                        RAppUserId = appUser.Id,
                        ClientIPAddress = clientIPAddress,
                        ClientAppName = loginViewModel.ClientAppName,
                        Language = loginViewModel.Language,
                        LoginTime = DateTime.Now,
                        LastRenewal = DateTime.Now,
                        ValidUntil = DateTime.Now + TimeSpan.FromSeconds(DefaultTokenExpirationInSeconds),
                        Token = ""
                    };


                await _context.Sessions.AddAsync(userSession);

                await _context.SaveChangesAsync();

                RServiceResult<string> userToken = await GenerateToken(loginViewModel.Username, appUser.Id, userSession.Id);
                if(userToken.Result == null)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, userToken.ExceptionString);
                }
                userSession.Token = userToken.Result;
                _context.Sessions.Update(userSession);
                _context.SaveChanges();

                return
                    new RServiceResult<LoggedOnUserModel>(
                    new LoggedOnUserModel()
                    {
                        SessionId = userSession.Id,
                        User = new PublicRAppUser(appUser),
                        Token = userToken.Result,
                        SecurableItem = securableItems.Result
                    }                    
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<LoggedOnUserModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// replace a (probably expired session) with a new one
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public async Task<RServiceResult<LoggedOnUserModel>> ReLogin(Guid sessionId, string clientIPAddress)
        {
            try
            {
                RTemporaryUserSession oldSession = await _context.Sessions.Include(s => s.RAppUser).Where(s => s.Id == sessionId).SingleOrDefaultAsync();
                if (oldSession == null)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "Invalid session");
                }
                RAppUser appUser = oldSession.RAppUser;
                if (appUser.Status == RAppUserStatus.Inactive)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "User is disabled by an admin.");
                }
                RServiceResult<SecurableItem[]> securableItems = await GetUserSecurableItemsStatus(appUser.Id);
                if (!string.IsNullOrEmpty(securableItems.ExceptionString))
                    return new RServiceResult<LoggedOnUserModel>(null, securableItems.ExceptionString);

                RTemporaryUserSession newSession =
                    new RTemporaryUserSession()
                    {
                        RAppUserId = appUser.Id,
                        ClientIPAddress = clientIPAddress,
                        ClientAppName = oldSession.ClientAppName,
                        Language = oldSession.Language,
                        LoginTime = DateTime.Now,
                        LastRenewal = DateTime.Now,
                        ValidUntil = DateTime.Now + TimeSpan.FromSeconds(DefaultTokenExpirationInSeconds),
                        Token = ""
                    };


                await _context.Sessions.AddAsync(newSession);
                _context.Sessions.Remove(oldSession);

                await _context.SaveChangesAsync();

                RServiceResult<string> userToken = await GenerateToken(appUser.UserName, appUser.Id, newSession.Id);
                if (userToken.Result == null)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, userToken.ExceptionString);
                }
                newSession.Token = userToken.Result;
                _context.Sessions.Update(newSession);
                _context.SaveChanges();

                return
                    new RServiceResult<LoggedOnUserModel>(
                    new LoggedOnUserModel()
                    {
                        SessionId = newSession.Id,
                        User = new PublicRAppUser(appUser),
                        Token = userToken.Result,
                        SecurableItem = securableItems.Result
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<LoggedOnUserModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddUserToRole(Guid userId, string roleName)
        {
            try
            {
                RAppUser dbUserInfo =
                    await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();

                if (dbUserInfo == null)
                {
                    return new RServiceResult<bool>(false, $"کاربر مورد نظر با ایمیل {userId} پیدا نشد ");
                }

                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var identityResult = await _roleManager.CreateAsync(new RAppRole(roleName));
                    if (!identityResult.Succeeded)
                    {
                        return new RServiceResult<bool>(false, $"Error creating {roleName} role : " + ErrorsToString(identityResult.Errors));
                    }
                }

                var addToRoleResult = await _userManager.AddToRoleAsync(dbUserInfo, roleName);
                if (!addToRoleResult.Succeeded)
                {
                    return new RServiceResult<bool>(false, $"Error adding admin to {roleName} role : " + ErrorsToString(addToRoleResult.Errors));
                }
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// Logout
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> Logout(Guid userId, Guid sessionId)
        {
            try
            {
                RTemporaryUserSession session =
                    await _context.Sessions
                    .Where(s => s.Id == sessionId && s.RAppUserId == userId)
                    .FirstOrDefaultAsync();
                if (session == null)
                    return new RServiceResult<bool>(false, "session is invalid");
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// Does Session exist?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SessionExists(Guid userId, Guid sessionId)
        {
            try
            {
                return new RServiceResult<bool>(
                    await _context.Sessions
                    .Where(s => s.RAppUserId == userId && s.Id == sessionId)
                    .FirstOrDefaultAsync() != null
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }



        /// <summary>
        /// returns user information
        /// </summary>
        /// <remarks>
        /// PasswordHash becomes empty
        /// </remarks>
        /// <param name="userId"></param>        
        /// <returns></returns>
        public async Task<RServiceResult<PublicRAppUser>> GetUserInformation(Guid userId)
        {
          
            try
            {
                RAppUser dbUserInfo =
                    await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();
                return new RServiceResult<PublicRAppUser>(new PublicRAppUser(dbUserInfo));              
            }
            catch (Exception exp)
            {
                return new RServiceResult<PublicRAppUser>(null, exp.ToString());
               
            }
        }        


        /// <summary>
        /// all users informations
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRAppUser[]>> GetAllUsersInformation()
        {
            try
            {

                
                RAppUser[] usersInfo = await _userManager.Users.ToArrayAsync();
                List<PublicRAppUser> lstPublicUsersInfo = new List<PublicRAppUser>();

                
                foreach(RAppUser userInfo in usersInfo)
                {
                    lstPublicUsersInfo.Add(new PublicRAppUser(userInfo));
                }
                return new RServiceResult<PublicRAppUser[]>(lstPublicUsersInfo.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PublicRAppUser[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Sessions
        /// </summary>
        /// <param name="userId">if null is passed returns all sessions</param>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRUserSession[]>> GetUserSessions(Guid? userId)
        {
            try
            {
                List<PublicRUserSession> publicRUserSessions = new List<PublicRUserSession>();

                RTemporaryUserSession[] sessions = 
                    userId == null ?
                    await _context.Sessions.ToArrayAsync()
                    :
                    await _context.Sessions.Where(s => s.RAppUserId == userId).ToArrayAsync()
                    ;

                foreach (RTemporaryUserSession rUserSession in sessions)
                    publicRUserSessions.Add(new PublicRUserSession(rUserSession));

                return new RServiceResult<PublicRUserSession[]>(publicRUserSessions.ToArray());
                
            }
            catch (Exception exp)
            {
                return new RServiceResult<PublicRUserSession[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// is user admin?
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> IsAdmin(Guid userId)
        {
            try
            {
                RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (dbUserInfo == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
                }
          
                bool res = await _userManager.IsInRoleAsync(dbUserInfo, AdministratorRoleName);
                return new RServiceResult<bool>(res);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// is user in either of passed roles?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleNames"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> IsInRoles(Guid userId, string[] roleNames)
        {
            try
            {
                RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (dbUserInfo == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
                }

                foreach(string roleName in roleNames)
                {
                    if(await _userManager.IsInRoleAsync(dbUserInfo, roleName))
                    {
                        return new RServiceResult<bool>(true);
                    }
                }
                
                return new RServiceResult<bool>(false);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Roles
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<IList<string>>> GetUserRoles(Guid userId)
        {         

            try
            {
               
                RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (dbUserInfo == null)
                {
                    return new RServiceResult<IList<string>>(null, "کاربر مورد نظر یافت نشد");
                }                

                return new RServiceResult<IList<string>>(await _userManager.GetRolesAsync(dbUserInfo));
            }
            catch (Exception exp)
            {
                return new RServiceResult<IList<string>>(null, exp.ToString());
            }
        }

        /// <summary>
        /// remove user from role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RemoveFromRole(Guid id, string role)
        {
            try
            {
                RAppUser dbUserInfo = await _userManager.FindByIdAsync(id.ToString());
                if (dbUserInfo == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
                }

                IdentityResult result = await _userManager.RemoveFromRoleAsync(dbUserInfo, role);
                if (!result.Succeeded)
                {
                    return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddToRole(Guid id, string role)
        {
            try
            {
                RAppUser dbUserInfo = await _userManager.FindByIdAsync(id.ToString());
                if (dbUserInfo == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
                }

                IdentityResult result = await _userManager.AddToRoleAsync(dbUserInfo, role);
                if (!result.Succeeded)
                {
                    return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid userId)
        {
            try
            {
                SecurableItem[]  securableItems = _userRoleService.GetSecurableItems();
                RServiceResult<IList<string>> roles = await GetUserRoles(userId);
                if(!string.IsNullOrEmpty(roles.ExceptionString))
                    return new RServiceResult<SecurableItem[]>(null, roles.ExceptionString);

                bool isAdmin = (await IsAdmin(userId)).Result;

                foreach(SecurableItem securableItem in securableItems)
                {
                    foreach(SecurableItemOperation operation in securableItem.Operations)
                    {
                        foreach(string role in roles.Result)
                        {
                            RServiceResult<bool> hasPermission = await _userRoleService.HasPermission(role, securableItem.ShortName, operation.ShortName);
                            if(!string.IsNullOrEmpty(hasPermission.ExceptionString))
                                return new RServiceResult<SecurableItem[]>(null, hasPermission.ExceptionString);                          
                            if (isAdmin || hasPermission.Result)
                            {
                                operation.Status = true;
                            }
                        }
                    }
                }
                return new RServiceResult<SecurableItem[]>(securableItems);
            }
            catch (Exception exp)
            {
                return new RServiceResult<SecurableItem[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Has user specified permission
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> HasPermission(Guid userId, string securableItemShortName, string operationShortName)
        {
            try
            {
                RServiceResult<IList<string>> roles = await GetUserRoles(userId);
                if (!string.IsNullOrEmpty(roles.ExceptionString))
                    return new RServiceResult<bool>(false, roles.ExceptionString);

                foreach (string role in roles.Result)
                {
                    RServiceResult<bool> hasPermission = await _userRoleService.HasPermission(role, securableItemShortName, operationShortName);
                    if (!string.IsNullOrEmpty(hasPermission.ExceptionString))
                        return new RServiceResult<bool>(false, hasPermission.ExceptionString);
                    if (hasPermission.Result)
                    {
                        return new RServiceResult<bool>(true);
                    }
                }

                return
                    new RServiceResult<bool>
                    (
                        false
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// add a new user
        /// </summary>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RAppUser>> AddUser(RegisterRAppUser newUserInfo)
        {
            try
            {
                if(!newUserInfo.IsAdmin)
                {
                    RServiceResult<bool> checkUserExists = await EnsureDefaultUserExists();
                    if (!checkUserExists.Result)
                    {
                        return new RServiceResult<RAppUser>(null, checkUserExists.ExceptionString);
                    }
                }                

                RAppUser existingInfo = await _userManager.FindByNameAsync(newUserInfo.Username);
                if (existingInfo != null)
                {
                    return new RServiceResult<RAppUser>(null, "username is already taken");
                }

                if(string.IsNullOrEmpty(newUserInfo.Password))
                {
                    newUserInfo.Password = Guid.NewGuid().ToString();
                }


                RAppUser newDbUser =
                    new RAppUser()
                    {
                        UserName = newUserInfo.Username,
                        FirstName = newUserInfo.FirstName,
                        SureName = newUserInfo.SureName,
                        Email = newUserInfo.Email,
                        PhoneNumber = newUserInfo.PhoneNumber,
                        CreateDate = DateTime.Now,
                        Status = newUserInfo.Status
                        
                    };
                

                var result = await _userManager.CreateAsync(newDbUser, newUserInfo.Password);

                if(!result.Succeeded)
                {
                    return new RServiceResult<RAppUser>(null, ErrorsToString(result.Errors));
                }

                newUserInfo.Id = newDbUser.Id;

                if(newUserInfo.IsAdmin)
                {

                    if (!await _roleManager.RoleExistsAsync(AdministratorRoleName))
                    {
                        var roleCheckResult = await _roleManager.CreateAsync(new RAppRole(AdministratorRoleName));
                        if (!roleCheckResult.Succeeded)
                        {
                            return new RServiceResult<RAppUser>(null, "Error creating Administrator role : " + ErrorsToString(roleCheckResult.Errors));
                        }
                    }


                    var addToAdminRoleResult = await _userManager.AddToRoleAsync(newDbUser, AdministratorRoleName);
                    if (!addToAdminRoleResult.Succeeded)
                    {
                        return new RServiceResult<RAppUser>(null, $"Error adding {newDbUser.UserName} to Administrator role : " + ErrorsToString(addToAdminRoleResult.Errors));
                    }
                }
                

                return new RServiceResult<RAppUser>(newDbUser);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RAppUser>(null, exp.ToString());
            }
        }


        /// <summary>
        /// modify existing user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="updateUserInfo"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifyUser(Guid userId, RegisterRAppUser updateUserInfo)
        {
            try
            {
                RAppUser existingInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (existingInfo == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
                }

                RServiceResult<bool> isAdmin = await IsAdmin(userId);
                if(!string.IsNullOrEmpty(isAdmin.ExceptionString))
                {
                    return new RServiceResult<bool>(false, isAdmin.ExceptionString);
                }

                if(isAdmin.Result && !updateUserInfo.IsAdmin)
                {
                    List<RAppUser> adminUsers = new List<RAppUser>(await _userManager.GetUsersInRoleAsync(AdministratorRoleName));
                    int nActiveAdminUsers = 0;
                    foreach(RAppUser adminUser in adminUsers)
                    {
                        if (adminUser.Status == RAppUserStatus.Active)
                            nActiveAdminUsers++;
                    }
                    if(nActiveAdminUsers <= 1)
                    {
                        return new RServiceResult<bool>(false, "You cannot reduce number of active admin users to 0.");
                    }
                }


                if (existingInfo.UserName != updateUserInfo.Username)
                {

                    RAppUser anotheruserWithUserName = await _userManager.FindByNameAsync(updateUserInfo.Username);

                    if(anotheruserWithUserName != null)
                    {
                        return new RServiceResult<bool>(false, "کلمه عبور تکراری می باشد");
                    }

                    existingInfo.UserName = updateUserInfo.Username;
                }

                
                existingInfo.FirstName = updateUserInfo.FirstName;
                existingInfo.SureName = updateUserInfo.SureName;
                existingInfo.Email = updateUserInfo.Email;
                existingInfo.PhoneNumber = updateUserInfo.PhoneNumber;
                existingInfo.Status = updateUserInfo.Status;

                if (!string.IsNullOrEmpty(updateUserInfo.Password))
                {
                   foreach(var passwordValidator in _userManager.PasswordValidators)
                   {
                        var resPass = await passwordValidator.ValidateAsync(_userManager, existingInfo, updateUserInfo.Password);
                        if (!resPass.Succeeded)
                        {
                            return new RServiceResult<bool>(false, ErrorsToString(resPass.Errors));
                        }
                   }                    

                    existingInfo.PasswordHash = _userManager.PasswordHasher.HashPassword(existingInfo, updateUserInfo.Password);
                }


                var result = await _userManager.UpdateAsync(existingInfo);
                if(!result.Succeeded)
                {
                    return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
                }


                

                //updating admin status  is not supported               



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// change user password checking old password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ChangePassword(Guid userId, string oldPassword, string newPassword)
        {
            RAppUser appUser = await _userManager.FindByIdAsync(userId.ToString());

            if (appUser == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            var result = await _userManager.ChangePasswordAsync(appUser, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                return new RServiceResult<bool>(false, "Identity error details says: " + result.ToString());
            }          
            


            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// delete user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>true if succeeds</returns>
        public async Task<RServiceResult<bool>> DeleteUser(Guid userId)
        {
            try
            {
                RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (dbUserInfo != null)
                {
                    var result = await _userManager.DeleteAsync(dbUserInfo);
                    if(!result.Succeeded)
                    {
                        return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
                    }
                    return new RServiceResult<bool>(true);
                }
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Set User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Guid?>> SetUserImage(Guid userId, IFormFileCollection files)
        {
            try
            {

                RAppUser user = await _userManager.FindByIdAsync(userId.ToString());

                if(files.Count == 0)
                {
                    user.RImageId = null;
                }
                else
                {
                    if (files.Count != 1)
                    {
                        return new RServiceResult<Guid?>(null, "files.Count != 1");
                    }

                    IFormFile file = files[0];


                    int nImageWidth = 192;
                    using (Stream stream = file.OpenReadStream())
                    {
                        using (Image img = Image.FromStream(stream))
                        {
                            if (img.Width > nImageWidth)
                            {
                                using (Bitmap bmpPhase1 = new Bitmap(nImageWidth, nImageWidth))
                                {
                                    using (Graphics g = Graphics.FromImage(bmpPhase1))
                                    {
                                        g.DrawImage(img, new Rectangle(0, 0, nImageWidth, img.Height * nImageWidth / img.Height));
                                    }

                                    using (Brush brush = new TextureBrush(bmpPhase1))
                                    {
                                        using (Bitmap bmpPhase2 = new Bitmap(nImageWidth, nImageWidth))
                                        {
                                            using (Graphics g = Graphics.FromImage(bmpPhase2))
                                            {
                                                g.FillEllipse(brush, new Rectangle(0, 0, nImageWidth, nImageWidth));
                                            }
                                            bmpPhase2.MakeTransparent();

                                            using (MemoryStream ms = new MemoryStream())
                                            {
                                                bmpPhase2.Save(ms, ImageFormat.Png);

                                                ms.Position = 0;
                                                RServiceResult<RImage> image = await _imageFileService.Add(null, ms, file.FileName, "UserProfiles");

                                                if (!string.IsNullOrEmpty(image.ExceptionString))
                                                {
                                                    return new RServiceResult<Guid?>(null, image.ExceptionString);
                                                }
                                                user.RImage = image.Result;

                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                RServiceResult<RImage> image = await _imageFileService.Add(null, stream, file.FileName, "UserProfiles");

                                if (!string.IsNullOrEmpty(image.ExceptionString))
                                {
                                    return new RServiceResult<Guid?>(null, image.ExceptionString);
                                }
                                user.RImage = image.Result;
                            }
                        }

                    }
                }               

                var result = await _userManager.UpdateAsync(user);
                if(!result.Succeeded)
                {
                    return new RServiceResult<Guid?>(null, ErrorsToString(result.Errors));
                }

                return new RServiceResult<Guid?>((Guid?)user.RImageId);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Guid?>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> GetUserImage(Guid userId)
        {
            try
            {

                RAppUser user = await _userManager.FindByIdAsync(userId.ToString());

                if(user.RImageId != null)
                {
                    return await _imageFileService.GetImage((Guid)user.RImageId);
                }

                return new RServiceResult<RImage>(null);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RImage>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Start signup process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RVerifyQueueItem>> SignUp(string email, string clientIPAddress, string clientAppName, string langauge)
        {
            try
            {
                if(string.IsNullOrEmpty(clientIPAddress))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "client ip address is empty");
                }

                if (string.IsNullOrEmpty(clientAppName))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "client app name is empty");
                }

                RAppUser existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "این آدرس ایمیل قبلا استفاده شده است");
                }

                existingUser = await _userManager.FindByNameAsync(email);
                if (existingUser != null)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "این نام کاربری قبلا استفاده شده است");
                }

                //checking this queue for previous signup attempts is unnecessary and is not done intentionally
                RVerifyQueueItem item = new RVerifyQueueItem()
                {
                    QueueType = RVerifyQueueType.SignUp,
                    Email = email,
                    DateTime = DateTime.Now,
                    ClientIPAddress = clientIPAddress,
                    ClientAppName = clientAppName,
                    Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0'),
                    Language = langauge
                };

                while(
                    null != 
                    await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).SingleOrDefaultAsync()
                    )
                {
                    item.Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0');
                }


                await _context.VerifyQueueItems.AddAsync
                    (
                    item
                    );
                await _context.SaveChangesAsync();
                return new RServiceResult<RVerifyQueueItem>(item);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RVerifyQueueItem>(null, exp.ToString());
            }
        }

        /// <summary>
        /// verify signup / forgot password
        /// </summary>
        /// <param name="verifyQueueType"></param>
        /// <param name="secret"></param>
        /// <returns>associated email</returns>
        public async Task<RServiceResult<string>> RetrieveEmailFromQueueSecret(RVerifyQueueType verifyQueueType, string secret)
        {
            try
            {
                RVerifyQueueItem item = await _context.VerifyQueueItems.Where(i => i.QueueType == verifyQueueType && i.Secret == secret).SingleOrDefaultAsync();
                if(item == null)
                {
                    return new RServiceResult<string>("");
                }

                return new RServiceResult<string>(item.Email);
            }
            catch (Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }
        }

        /// <summary>
        /// finalize signup process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="sureName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> FinalizeSignUp(string email, string secret, string password, string firstName, string sureName)
        {
            try
            {

                RAppUser existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return new RServiceResult<bool>(false, "این آدرس ایمیل قبلا استفاده شده است");
                }

                existingUser = await _userManager.FindByNameAsync(email);
                if (existingUser != null)
                {
                    return new RServiceResult<bool>(false, "این نام کاربری قبلا استفاده شده است");
                }

                if(
                    email
                    !=
                    (await RetrieveEmailFromQueueSecret(RVerifyQueueType.SignUp, secret)).Result
                 )
                {
                    return new RServiceResult<bool>(false, "کلمه عبور اشتباه وارد شده است");
                }

                secret = secret.Replace(" ", "");//TODO: check this

                RegisterRAppUser newUserInfo = new RegisterRAppUser()
                {
                    Username = email,
                    Email = email,
                    Password = password,
                    Status = RAppUserStatus.Active,
                    IsAdmin = false,
                    FirstName = firstName,
                    SureName = sureName
                };

                RServiceResult<RAppUser> userAddResult = await AddUser(newUserInfo);

                if(userAddResult.Result == null)
                {
                    return new RServiceResult<bool>(false, userAddResult.ExceptionString);
                }

                userAddResult.Result.EmailConfirmed = true;
                await _userManager.UpdateAsync(userAddResult.Result);

                RVerifyQueueItem[] failedQueue = await _context.VerifyQueueItems.Where(i => i.Email == email && i.Secret != secret && i.QueueType == RVerifyQueueType.SignUp).ToArrayAsync();
                if(failedQueue.Length != 0)
                {
                    _context.VerifyQueueItems.RemoveRange(failedQueue);
                }                   

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Start forgot password process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RVerifyQueueItem>> ForgotPassword(string email, string clientIPAddress, string clientAppName, string langauge)
        {
            try
            {
                if (string.IsNullOrEmpty(clientIPAddress))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "client ip address is empty");
                }

                if (string.IsNullOrEmpty(clientAppName))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "client app name is empty");
                }

                RAppUser rAppUser = await _userManager.FindByEmailAsync(email);
                if (rAppUser == null)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "کاربر مورد نظر یافت نشد");
                }               

                //checking this queue for previous signup attempts is unnecessary and is not done intentionally
                RVerifyQueueItem item = new RVerifyQueueItem()
                {
                    QueueType = RVerifyQueueType.ForgotPassword,
                    Email = email,
                    DateTime = DateTime.Now,
                    ClientIPAddress = clientIPAddress,
                    ClientAppName = clientAppName,
                    Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0'),
                    Language = langauge
                };

                while (
                    null !=
                    await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).SingleOrDefaultAsync()
                    )
                {
                    item.Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0');
                }


                await _context.VerifyQueueItems.AddAsync
                    (
                    item
                    );
                await _context.SaveChangesAsync();
                return new RServiceResult<RVerifyQueueItem>(item);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RVerifyQueueItem>(null, exp.ToString());
            }
        }

        /// <summary>
        /// reset password using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="clientIPAddress"></param>       
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ResetPassword(string email, string secret, string password, string clientIPAddress)
        {
            try
            {
                //we ignore input model in automatic auditing to prevent loginng password data, so we would add a manual auditing to have enough data on login intrusion and ...
                REvent log = new REvent()
                {
                    EventType = "AppUser/ResetPassword (POST)(Manual)",
                    StartDate = DateTime.UtcNow,
                    UserName = email,
                    IpAddress = clientIPAddress
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();

                RAppUser existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر با این آدرس ایمیل یافت نشد");
                }
               

                if (
                    email
                    !=
                    (await RetrieveEmailFromQueueSecret(RVerifyQueueType.ForgotPassword, secret)).Result
                 )
                {
                    return new RServiceResult<bool>(false, "کلمه عبور اشتباه وارد شده است");
                }

                foreach (var passwordValidator in _userManager.PasswordValidators)
                {
                    var resPass = await passwordValidator.ValidateAsync(_userManager, existingUser, password);
                    if (!resPass.Succeeded)
                    {
                        return new RServiceResult<bool>(false, ErrorsToString(resPass.Errors));
                    }
                }

                existingUser.PasswordHash = _userManager.PasswordHasher.HashPassword(existingUser, password);

                await _userManager.UpdateAsync(existingUser);

                RVerifyQueueItem[] failedQueue = await _context.VerifyQueueItems.Where(i => i.Email == email && i.QueueType == RVerifyQueueType.ForgotPassword).ToArrayAsync();
                if (failedQueue.Length != 0)
                {
                    _context.VerifyQueueItems.RemoveRange(failedQueue);
                }

                

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// delete tenant
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> DeleteTenant()
        {
            try
            {
                _context.DeleteDb();
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// EnsureDefaultUserExists
        /// </summary>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> EnsureDefaultUserExists()
        {
            try
            {
                //If no user exists create default one                
                if (_userManager.Users.Count() == 0)
                {
                    RAppUser admin = new RAppUser()
                    {
                        UserName = "admin",
                        FirstName = "راهبر",
                        SureName = "سیستم",
                        Email = "admin@ganjoor.net",
                        PhoneNumber = "00989123456789",
                        CreateDate = DateTime.Now,
                        Status = RAppUserStatus.Active
                    };

                    var identityResult = await _userManager.CreateAsync(
                        admin, "Test!123"
                        );
                    if (!identityResult.Succeeded)
                    {
                        return new RServiceResult<bool>(false, "Error creating default user : " + ErrorsToString(identityResult.Errors));
                    }

                    if (!await _roleManager.RoleExistsAsync(AdministratorRoleName))
                    {
                        identityResult = await _roleManager.CreateAsync(new RAppRole(AdministratorRoleName));
                        if (!identityResult.Succeeded)
                        {
                            return new RServiceResult<bool>(false, "Error creating Administrator role : " + ErrorsToString(identityResult.Errors));
                        }
                    }

                    identityResult = await _userManager.AddToRoleAsync(admin, AdministratorRoleName);
                    if (!identityResult.Succeeded)
                    {
                        return new RServiceResult<bool>(false, "Error adding admin to Administrator role : " + ErrorsToString(identityResult.Errors));
                    }
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// Administrator role name
        /// </summary>
        public string AdministratorRoleName { get { return "Administrator"; } }


        /// <summary>
        /// secret used for generating Jwt token
        /// </summary>
        public string TokenSecret { get { return $"{Configuration.GetSection("Security")["Secret"]}";  } }

        /// <summary>
        /// JWT Tokens Expiration Time Out
        /// </summary>
        public int DefaultTokenExpirationInSeconds { get { return int.Parse( $"{Configuration.GetSection("Security")["DefaultTokenExpirationInSeconds"]}"); } }



        #region Internals

        #region Token Generation

        /// <summary>
        /// Token Generation
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private async Task<RServiceResult<string>> GenerateToken(string username, Guid userId, Guid sessionId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return new RServiceResult<string>(null, "کاربر مورد نظر یافت نشد");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("UserId", userId.ToString()),
                new Claim("SessionId", sessionId.ToString()),
            };

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (Claim roleClaim in roleClaims)
                    {
                        claims.Add(roleClaim);
                    }
                }
            }

            var token = new JwtSecurityToken(
                issuer: "Ganjoor",
                audience: "Everyone",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddSeconds(DefaultTokenExpirationInSeconds),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenSecret)), SecurityAlgorithms.HmacSha256)
                );

            return new RServiceResult<string>(new JwtSecurityTokenHandler().WriteToken(token));

        }

        /// <summary>
        /// Renew Expired token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<RServiceResult<string>> RegenerateToken(string token)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(token);

                //check refreshToken here to see if it is valid or not
                //check user here, if is not valid return BadRequest();
                ExtractTokenInfo(token, out string username, out Guid userId, out Guid sessionId);

                return await GenerateToken(username, userId, sessionId);
            }
            catch (Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }

        }


        /// <summary>
        /// Extract Information From Token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="username"></param>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        public void ExtractTokenInfo(string token, out string username, out Guid userId, out Guid sessionId)
        {

            var principal = GetPrincipalFromExpiredToken(token);
            username = principal.Identity.Name;
            userId = new Guid(principal.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            sessionId = new Guid(principal.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidAudience = "Everyone",
                ValidateIssuer = true,
                ValidIssuer = "Ganjoor",

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenSecret)),

                ValidateLifetime = false, //important

                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        #endregion

        #region Identity Errors conversion
        /// <summary>
        /// convert identity errors to string
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        protected static string ErrorsToString(IEnumerable<IdentityError> errors)
        {
            StringBuilder sb = new StringBuilder();
            foreach(IdentityError error in errors)
            {
                sb.AppendLine(error.Description);
            }
            return sb.ToString();
        }
        #endregion
        #endregion

        /// <summary>
        /// Main Database context
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// Image File Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;
        /// <summary>
        /// User Role Service
        /// </summary>
        protected readonly IUserRoleService _userRoleService;
        /// <summary>
        /// Identity User Manageer
        /// </summary>
        protected UserManager<RAppUser> _userManager = null;
        /// <summary>
        /// Identity SignIn Manager
        /// </summary>
        protected SignInManager<RAppUser> _signInManager = null;
        /// <summary>
        /// Identity Role Manager
        /// </summary>
        protected RoleManager<RAppRole> _roleManager = null;

        /// <summary>
        /// secret generator
        /// </summary>
        protected readonly ISecretGenerator _secretGenerator;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="secretGenerator"></param>
        /// <param name="imageFileService"></param>
        /// <param name="userRoleService"></param>
        /// <param name="configuration"></param>
        public AppUserService(
            RSecurityDbContext<RAppUser, RAppRole, Guid> context,
            UserManager<RAppUser> userManager,
            SignInManager<RAppUser> signInManager,
            RoleManager<RAppRole> roleManager,
            ISecretGenerator secretGenerator,
            IImageFileService imageFileService,
            IUserRoleService userRoleService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _secretGenerator = secretGenerator;
            _imageFileService = imageFileService;
            _userRoleService = userRoleService;
            Configuration = configuration;
        }
    }
}
