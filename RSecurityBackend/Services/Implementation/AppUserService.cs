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
        public virtual async Task<RServiceResult<LoggedOnUserModel>> Login(LoginViewModel loginViewModel, string clientIPAddress)
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
            if (!checkUserExists.Result)
            {
                return new RServiceResult<LoggedOnUserModel>(null, checkUserExists.ExceptionString);
            }

            RAppUser appUser = await _userManager.FindByNameAsync(loginViewModel.Username);

            if (appUser == null)
            {
                appUser = await _userManager.FindByEmailAsync(loginViewModel.Username);
                if (appUser == null)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "نام کاربری و/یا رمز نادرست است.");
                }
            }

            var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginViewModel.Password, true);
            if (result.IsLockedOut)
            {
                return new RServiceResult<LoggedOnUserModel>(null, "نام کاربری شما به دلیل ورود متوالی ۵ بارهٔ رمزهای اشتباه قفل شده است. لطفاً ۵ دقیقهٔ‌دیگر مجدداً تلاش کنید.");
            }

            if (result.IsNotAllowed || result.RequiresTwoFactor)
            {
                return new RServiceResult<LoggedOnUserModel>(null, result.ToString());
            }

            if (!result.Succeeded)
            {
                return new RServiceResult<LoggedOnUserModel>(null, "نام کاربری و/یا رمز نادرست است.");
            }

            if (appUser.Status == RAppUserStatus.Inactive)
            {
                return new RServiceResult<LoggedOnUserModel>(null, "نام کاربری شما غیرفعال شده است.");
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
            if (userToken.Result == null)
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
                    User = new PublicRAppUser()
                    {
                        Id = appUser.Id,
                        Username = appUser.UserName,
                        Email = appUser.Email,
                        FirstName = appUser.FirstName,
                        SureName = appUser.SureName,
                        PhoneNumber = appUser.PhoneNumber,
                        RImageId = appUser.RImageId,
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed
                    },
                    Token = userToken.Result,
                    SecurableItem = securableItems.Result
                }
                );
        }

        /// <summary>
        /// replace a (probably expired session) with a new one
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<LoggedOnUserModel>> ReLogin(Guid sessionId, string clientIPAddress)
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
                    User = new PublicRAppUser()
                    {
                        Id = appUser.Id,
                        Username = appUser.UserName,
                        Email = appUser.Email,
                        FirstName = appUser.FirstName,
                        SureName = appUser.SureName,
                        PhoneNumber = appUser.PhoneNumber,
                        RImageId = appUser.RImageId,
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed
                    },
                    Token = userToken.Result,
                    SecurableItem = securableItems.Result
                }
                );

        }

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> AddUserToRole(Guid userId, string roleName)
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


        /// <summary>
        /// Logout
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> Logout(Guid userId, Guid sessionId)
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


        /// <summary>
        /// Does Session exist?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> SessionExists(Guid userId, Guid sessionId)
        {
            return new RServiceResult<bool>(
                await _context.Sessions
                .Where(s => s.RAppUserId == userId && s.Id == sessionId)
                .FirstOrDefaultAsync() != null
                );
        }



        /// <summary>
        /// returns user information
        /// </summary>
        /// <remarks>
        /// PasswordHash becomes empty
        /// </remarks>
        /// <param name="userId"></param>        
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRAppUser>> GetUserInformation(Guid userId)
        {
            RAppUser appUser =
                await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();
            if (appUser == null)
                return new RServiceResult<PublicRAppUser>(null);
            return new RServiceResult<PublicRAppUser>(
                new PublicRAppUser()
                {
                    Id = appUser.Id,
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    FirstName = appUser.FirstName,
                    SureName = appUser.SureName,
                    PhoneNumber = appUser.PhoneNumber,
                    RImageId = appUser.RImageId,
                    Status = appUser.Status,
                    NickName = appUser.NickName,
                    Website = appUser.Website,
                    Bio = appUser.Bio,
                    EmailConfirmed = appUser.EmailConfirmed
                });
        }


        /// <summary>
        /// all users informations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filterByEmail"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRAppUser[] Items)>>
            GetAllUsersInformation(PagingParameterModel paging, string filterByEmail)
        {
            var source = _userManager.Users
                .Where(appUser => string.IsNullOrEmpty(filterByEmail) || (!string.IsNullOrEmpty(filterByEmail) && appUser.Email.Contains(filterByEmail)))
                .Select(appUser => new PublicRAppUser()
                {
                    Id = appUser.Id,
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    FirstName = appUser.FirstName,
                    SureName = appUser.SureName,
                    PhoneNumber = appUser.PhoneNumber,
                    RImageId = appUser.RImageId,
                    Status = appUser.Status,
                    NickName = appUser.NickName,
                    Website = appUser.Website,
                    Bio = appUser.Bio,
                    EmailConfirmed = appUser.EmailConfirmed
                });

            return new RServiceResult<(PaginationMetadata PagingMeta, PublicRAppUser[] Items)>(
                await QueryablePaginator<PublicRAppUser>.Paginate(source, paging));


        }

        /// <summary>
        /// Get User Sessions
        /// </summary>
        /// <param name="userId">if null is passed returns all sessions</param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRUserSession[]>> GetUserSessions(Guid? userId)
        {

            List<PublicRUserSession> publicRUserSessions = new List<PublicRUserSession>();

            RTemporaryUserSession[] sessions =
                userId == null ?
                await _context.Sessions.ToArrayAsync()
                :
                await _context.Sessions.Where(s => s.RAppUserId == userId).ToArrayAsync()
                ;

            foreach (RTemporaryUserSession rUserSession in sessions)
                publicRUserSessions.Add(
                    new PublicRUserSession()
                    {
                        Id = rUserSession.Id,
                        RAppUser = new PublicRAppUser()
                        {
                            Id = rUserSession.RAppUser.Id,
                            Username = rUserSession.RAppUser.UserName,
                            Email = rUserSession.RAppUser.Email,
                            FirstName = rUserSession.RAppUser.FirstName,
                            SureName = rUserSession.RAppUser.SureName,
                            PhoneNumber = rUserSession.RAppUser.PhoneNumber,
                            RImageId = rUserSession.RAppUser.RImageId,
                            Status = rUserSession.RAppUser.Status,
                            NickName = rUserSession.RAppUser.NickName,
                            Website = rUserSession.RAppUser.Website,
                            Bio = rUserSession.RAppUser.Bio,
                            EmailConfirmed = rUserSession.RAppUser.EmailConfirmed
                        },
                        ClientAppName = rUserSession.ClientAppName,
                        ClientIPAddress = rUserSession.ClientIPAddress,
                        Language = rUserSession.Language,
                        LastRenewal = rUserSession.LastRenewal,
                        LoginTime = rUserSession.LoginTime
                    }
                    );

            return new RServiceResult<PublicRUserSession[]>(publicRUserSessions.ToArray());


        }

        /// <summary>
        /// Get User Session
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRUserSession>> GetUserSession(Guid userId, Guid sessionId)
        {
            RTemporaryUserSession rUserSession =
                await _context.Sessions.Where(s => s.RAppUserId == userId && s.Id == sessionId).SingleAsync();
            return new RServiceResult<PublicRUserSession>
                (
                    new PublicRUserSession()
                    {
                        Id = rUserSession.Id,
                        RAppUser = new PublicRAppUser()
                        {
                            Id = rUserSession.RAppUser.Id,
                            Username = rUserSession.RAppUser.UserName,
                            Email = rUserSession.RAppUser.Email,
                            FirstName = rUserSession.RAppUser.FirstName,
                            SureName = rUserSession.RAppUser.SureName,
                            PhoneNumber = rUserSession.RAppUser.PhoneNumber,
                            RImageId = rUserSession.RAppUser.RImageId,
                            Status = rUserSession.RAppUser.Status,
                            NickName = rUserSession.RAppUser.NickName,
                            Website = rUserSession.RAppUser.Website,
                            Bio = rUserSession.RAppUser.Bio,
                            EmailConfirmed = rUserSession.RAppUser.EmailConfirmed
                        },
                        ClientAppName = rUserSession.ClientAppName,
                        ClientIPAddress = rUserSession.ClientIPAddress,
                        Language = rUserSession.Language,
                        LastRenewal = rUserSession.LastRenewal,
                        LoginTime = rUserSession.LoginTime
                    }

            );
        }

        /// <summary>
        /// is user admin?
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> IsAdmin(Guid userId)
        {

            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            bool res = await _userManager.IsInRoleAsync(dbUserInfo, _userRoleService.AdministratorRoleName);
            return new RServiceResult<bool>(res);

        }

        /// <summary>
        /// is user in either of passed roles?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleNames"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> IsInRoles(Guid userId, string[] roleNames)
        {

            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            foreach (string roleName in roleNames)
            {
                if (await _userManager.IsInRoleAsync(dbUserInfo, roleName))
                {
                    return new RServiceResult<bool>(true);
                }
            }

            return new RServiceResult<bool>(false);

        }

        /// <summary>
        /// Get User Roles
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<IList<string>>> GetUserRoles(Guid userId)
        {
            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<IList<string>>(null, "کاربر مورد نظر یافت نشد");
            }

            return new RServiceResult<IList<string>>(await _userManager.GetRolesAsync(dbUserInfo));

        }

        /// <summary>
        /// remove user from role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RemoveFromRole(Guid id, string role)
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

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> AddToRole(Guid id, string role)
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

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid userId)
        {
            SecurableItem[] securableItems = _userRoleService.GetSecurableItems();
            RServiceResult<IList<string>> roles = await GetUserRoles(userId);
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return new RServiceResult<SecurableItem[]>(null, roles.ExceptionString);

            bool isAdmin = (await IsAdmin(userId)).Result;

            foreach (SecurableItem securableItem in securableItems)
            {
                foreach (SecurableItemOperation operation in securableItem.Operations)
                {
                    foreach (string role in roles.Result)
                    {
                        RServiceResult<bool> hasPermission = await _userRoleService.HasPermission(role, securableItem.ShortName, operation.ShortName);
                        if (!string.IsNullOrEmpty(hasPermission.ExceptionString))
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

        /// <summary>
        /// Has user specified permission
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> HasPermission(Guid userId, string securableItemShortName, string operationShortName)
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

        /// <summary>
        /// all users having a certain permission
        /// </summary>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRAppUser[]>> GetUsersHavingPermission(string securableItemShortName, string operationShortName)
        {
            var rolesResult = await _userRoleService.GetRolesHavingPermission(securableItemShortName, operationShortName);
            if (!string.IsNullOrEmpty(rolesResult.ExceptionString))
            {
                return new RServiceResult<PublicRAppUser[]>(null, rolesResult.ExceptionString);
            }
            List<PublicRAppUser> lstPublicUsersInfo = new List<PublicRAppUser>();

            RAppRole[] roles = rolesResult.Result;
            foreach (RAppRole role in roles)
            {
                var usersInRole = _userManager.GetUsersInRoleAsync(role.Name);
                foreach (var appUser in usersInRole.Result)
                {
                    if (lstPublicUsersInfo.Where(u => u.Id == appUser.Id).FirstOrDefault() == null)
                    {
                        lstPublicUsersInfo.Add(
                            new PublicRAppUser()
                            {
                                Id = appUser.Id,
                                Username = appUser.UserName,
                                Email = appUser.Email,
                                FirstName = appUser.FirstName,
                                SureName = appUser.SureName,
                                PhoneNumber = appUser.PhoneNumber,
                                RImageId = appUser.RImageId,
                                Status = appUser.Status,
                                NickName = appUser.NickName,
                                Website = appUser.Website,
                                Bio = appUser.Bio,
                                EmailConfirmed = appUser.EmailConfirmed
                            });
                    }
                }
            }

            return new RServiceResult<PublicRAppUser[]>(lstPublicUsersInfo.ToArray());
        }


        /// <summary>
        /// add a new user
        /// </summary>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RAppUser>> AddUser(RegisterRAppUser newUserInfo)
        {
            if (!newUserInfo.IsAdmin)
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

            if (string.IsNullOrEmpty(newUserInfo.Password))
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

            if (!result.Succeeded)
            {
                return new RServiceResult<RAppUser>(null, ErrorsToString(result.Errors));
            }

            newUserInfo.Id = newDbUser.Id;

            if (newUserInfo.IsAdmin)
            {

                if (!await _roleManager.RoleExistsAsync(_userRoleService.AdministratorRoleName))
                {
                    var roleCheckResult = await _roleManager.CreateAsync(new RAppRole(_userRoleService.AdministratorRoleName));
                    if (!roleCheckResult.Succeeded)
                    {
                        return new RServiceResult<RAppUser>(null, "Error creating Administrator role : " + ErrorsToString(roleCheckResult.Errors));
                    }
                }


                var addToAdminRoleResult = await _userManager.AddToRoleAsync(newDbUser, _userRoleService.AdministratorRoleName);
                if (!addToAdminRoleResult.Succeeded)
                {
                    return new RServiceResult<RAppUser>(null, $"Error adding {newDbUser.UserName} to Administrator role : " + ErrorsToString(addToAdminRoleResult.Errors));
                }
            }


            return new RServiceResult<RAppUser>(newDbUser);
        }


        /// <summary>
        /// modify existing user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="updateUserInfo"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ModifyUser(Guid userId, RegisterRAppUser updateUserInfo)
        {
            RAppUser existingInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (existingInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            RServiceResult<bool> isAdmin = await IsAdmin(userId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
            {
                return new RServiceResult<bool>(false, isAdmin.ExceptionString);
            }

            if (isAdmin.Result && !updateUserInfo.IsAdmin)
            {
                List<RAppUser> adminUsers = new List<RAppUser>(await _userManager.GetUsersInRoleAsync(_userRoleService.AdministratorRoleName));
                int nActiveAdminUsers = 0;
                foreach (RAppUser adminUser in adminUsers)
                {
                    if (adminUser.Status == RAppUserStatus.Active)
                        nActiveAdminUsers++;
                }
                if (nActiveAdminUsers <= 1)
                {
                    return new RServiceResult<bool>(false, "You cannot reduce number of active admin users to 0.");
                }
            }

            if (isAdmin.Result != updateUserInfo.IsAdmin)
            {
                return new RServiceResult<bool>(false, "امکان تغییر وضعیت مدیر کاربر از طریق این تابع وجود ندارد.");
            }




            if (existingInfo.UserName != updateUserInfo.Username)
            {

                RAppUser anotheruserWithUserName = await _userManager.FindByNameAsync(updateUserInfo.Username);

                if (anotheruserWithUserName != null)
                {
                    return new RServiceResult<bool>(false, "کلمهٔ عبور تکراری می باشد");
                }

                existingInfo.UserName = updateUserInfo.Username;
            }

            if (updateUserInfo.RImageId != null && updateUserInfo.RImageId != Guid.Empty && updateUserInfo.RImageId != existingInfo.RImageId)
            {
                return new RServiceResult<bool>(false, "برای تغییر تصویر کاربر از تابع اختصاصی این کار استفاده کنید");
            }




            existingInfo.FirstName = updateUserInfo.FirstName;
            existingInfo.SureName = updateUserInfo.SureName;
            existingInfo.Email = updateUserInfo.Email;
            existingInfo.PhoneNumber = updateUserInfo.PhoneNumber;
            existingInfo.Status = updateUserInfo.Status;
            existingInfo.Bio = updateUserInfo.Bio;
            existingInfo.NickName = updateUserInfo.NickName;
            existingInfo.Website = updateUserInfo.Website;

            if (!string.IsNullOrEmpty(updateUserInfo.Password))
            {
                foreach (var passwordValidator in _userManager.PasswordValidators)
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
            if (!result.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
            }




            //updating admin status  is not supported               



            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// change user password checking old password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ChangePassword(Guid userId, string oldPassword, string newPassword)
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
        /// remove user data
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RemoveUserData(Guid userId)
        {
            var notications = await _context.Notifications.Where(n => n.UserId == userId).ToArrayAsync();
            _context.RemoveRange(notications);

            var options = await _context.Options.Where(o => o.RAppUserId == userId).ToListAsync();
            _context.RemoveRange(options);

            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>true if succeeds</returns>
        public virtual async Task<RServiceResult<bool>> DeleteUser(Guid userId)
        {
            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo != null)
            {
                var resDelData = await RemoveUserData(userId);
                if (!string.IsNullOrEmpty(resDelData.ExceptionString))
                    return new RServiceResult<bool>(false, resDelData.ExceptionString);
                if (!resDelData.Result)
                    return new RServiceResult<bool>(false, "حذف اطلاعات کاربر با خطا مواجه شد.");
                var result = await _userManager.DeleteAsync(dbUserInfo);
                if (!result.Succeeded)
                {
                    return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
                }
                return new RServiceResult<bool>(true);
            }
            return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
        }

        /// <summary>
        /// start leaving
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> StartLeaving(Guid userId, Guid sessionId, string clientIPAddress)
        {
            var session = await GetUserSession(userId, sessionId);
            var user = (await GetUserInformation(userId)).Result;

            //checking this queue for previous signup attempts is unnecessary and is not done intentionally
            RVerifyQueueItem item = new RVerifyQueueItem()
            {
                QueueType = RVerifyQueueType.UserSelfDelete,
                Email = user.Email,
                DateTime = DateTime.Now,
                ClientIPAddress = clientIPAddress,
                ClientAppName = session.Result.ClientAppName,
                Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0'),
                Language = session.Result.Language
            };

            var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
            if (existingSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(existingSecrets);
                await _context.SaveChangesAsync();
            }

            await _context.VerifyQueueItems.AddAsync
                (
                item
                );
            await _context.SaveChangesAsync();
            return new RServiceResult<RVerifyQueueItem>(item);

        }

        /// <summary>
        /// Set User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<Guid?>> SetUserImage(Guid userId, IFormFileCollection files)
        {

            RAppUser user = await _userManager.FindByIdAsync(userId.ToString());

            if (files.Count == 0)
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
                using Stream stream = file.OpenReadStream();
                using Image img = Image.FromStream(stream);
                if (img.Width > nImageWidth)
                {
                    using Bitmap bmpPhase1 = new Bitmap(nImageWidth, nImageWidth);
                    using (Graphics g = Graphics.FromImage(bmpPhase1))
                    {
                        g.DrawImage(img, new Rectangle(0, 0, nImageWidth, img.Height * nImageWidth / img.Height));
                    }

                    using Brush brush = new TextureBrush(bmpPhase1);
                    using Bitmap bmpPhase2 = new Bitmap(nImageWidth, nImageWidth);
                    using (Graphics g = Graphics.FromImage(bmpPhase2))
                    {
                        g.FillEllipse(brush, new Rectangle(0, 0, nImageWidth, nImageWidth));
                    }
                    bmpPhase2.MakeTransparent();

                    using MemoryStream ms = new MemoryStream();
                    bmpPhase2.Save(ms, ImageFormat.Png);

                    ms.Position = 0;
                    RServiceResult<RImage> image = await _imageFileService.Add(null, ms, file.FileName, "UserProfiles");

                    if (!string.IsNullOrEmpty(image.ExceptionString))
                    {
                        return new RServiceResult<Guid?>(null, image.ExceptionString);
                    }
                    user.RImage = image.Result;
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

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new RServiceResult<Guid?>(null, ErrorsToString(result.Errors));
            }

            return new RServiceResult<Guid?>((Guid?)user.RImageId);

        }

        /// <summary>
        /// Get User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RImage>> GetUserImage(Guid userId)
        {
            RAppUser user = await _userManager.FindByIdAsync(userId.ToString());

            if (user.RImageId != null)
            {
                return await _imageFileService.GetImage((Guid)user.RImageId);
            }

            return new RServiceResult<RImage>(null);

        }

        /// <summary>
        /// Start signup process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> SignUp(string email, string clientIPAddress, string clientAppName, string langauge)
        {

            if (string.IsNullOrEmpty(clientIPAddress))
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
                return new RServiceResult<RVerifyQueueItem>(null, "شما قبلا ثبت نام کرده‌اید. توجه بفرمایید که کاربران گنجینهٔ گنجور و پیشخان خوانشگران یکسانند و می‌توانید با همان نام کاربری اینجا وارد شوید.");
            }

            existingUser = await _userManager.FindByNameAsync(email);
            if (existingUser != null)
            {
                return new RServiceResult<RVerifyQueueItem>(null, "این نام کاربری قبلا استفاده شده است");
            }

            var oldSecrets = await _context.VerifyQueueItems.Where(i => i.DateTime < DateTime.Now.AddDays(-1)).ToListAsync();
            if (oldSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(oldSecrets);
                await _context.SaveChangesAsync();
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

            var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
            if (existingSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(existingSecrets);
                await _context.SaveChangesAsync();
            }

            await _context.VerifyQueueItems.AddAsync
                (
                item
                );
            await _context.SaveChangesAsync();
            return new RServiceResult<RVerifyQueueItem>(item);

        }

        /// <summary>
        /// verify signup / forgot password
        /// </summary>
        /// <param name="verifyQueueType"></param>
        /// <param name="secret"></param>
        /// <returns>associated email</returns>
        public virtual async Task<RServiceResult<string>> RetrieveEmailFromQueueSecret(RVerifyQueueType verifyQueueType, string secret)
        {

            secret = secret.Trim();
            RVerifyQueueItem item = await _context.VerifyQueueItems.Where(i => i.QueueType == verifyQueueType && i.Secret == secret).SingleOrDefaultAsync();
            if (item == null)
            {
                return new RServiceResult<string>("");
            }

            return new RServiceResult<string>(item.Email);

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
        public virtual async Task<RServiceResult<bool>> FinalizeSignUp(string email, string secret, string password, string firstName, string sureName)
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

            if (
                   email
                   !=
                   (await RetrieveEmailFromQueueSecret(RVerifyQueueType.SignUp, secret)).Result
                  )
            {
                return new RServiceResult<bool>(false, "کد ارسالی به ایمیل اشتباه وارد شده است");
            }

            secret = secret.Trim();

            firstName = firstName.Trim();
            sureName = sureName.Trim();

            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(sureName))
            {
                return new RServiceResult<bool>(false, "لطفاً حداقل یکی از اطلاعات نام یا نام خانوادگی را وارد کنید.");
            }

            RegisterRAppUser newUserInfo = new RegisterRAppUser()
            {
                Username = email,
                Email = email,
                Password = password,
                Status = RAppUserStatus.Active,
                IsAdmin = false,
                FirstName = firstName,
                SureName = sureName,
                NickName = $"{firstName} {sureName}".Trim()
            };

            RServiceResult<RAppUser> userAddResult = await AddUser(newUserInfo);

            if (userAddResult.Result == null)
            {
                return new RServiceResult<bool>(false, userAddResult.ExceptionString);
            }

            userAddResult.Result.EmailConfirmed = true;
            await _userManager.UpdateAsync(userAddResult.Result);

            RVerifyQueueItem[] failedQueue = await _context.VerifyQueueItems.Where(i => i.Email == email && i.Secret != secret && i.QueueType == RVerifyQueueType.SignUp).ToArrayAsync();
            if (failedQueue.Length != 0)
            {
                _context.VerifyQueueItems.RemoveRange(failedQueue);
            }

            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// Start forgot password process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> ForgotPassword(string email, string clientIPAddress, string clientAppName, string langauge)
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

            var oldSecrets = await _context.VerifyQueueItems.Where(i => i.DateTime < DateTime.Now.AddDays(-1)).ToListAsync();
            if (oldSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(oldSecrets);
                await _context.SaveChangesAsync();
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

            var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
            if (existingSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(existingSecrets);
                await _context.SaveChangesAsync();
            }



            await _context.VerifyQueueItems.AddAsync
                (
                item
                );
            await _context.SaveChangesAsync();
            return new RServiceResult<RVerifyQueueItem>(item);
        }

        /// <summary>
        /// reset password using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="clientIPAddress"></param>       
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ResetPassword(string email, string secret, string password, string clientIPAddress)
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

        /// <summary>
        /// Find User By Email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRAppUser>> FindUserByEmail(string email)
        {
            RAppUser appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
                return new RServiceResult<PublicRAppUser>(null);
            return new RServiceResult<PublicRAppUser>(
                new PublicRAppUser()
                {
                    Id = appUser.Id,
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    FirstName = appUser.FirstName,
                    SureName = appUser.SureName,
                    PhoneNumber = appUser.PhoneNumber,
                    RImageId = appUser.RImageId,
                    Status = appUser.Status,
                    NickName = appUser.NickName,
                    Website = appUser.Website,
                    Bio = appUser.Bio,
                    EmailConfirmed = appUser.EmailConfirmed
                });

        }


        /// <summary>
        /// delete tenant
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> DeleteTenant()
        {
            _context.DeleteDb();
            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// EnsureDefaultUserExists
        /// </summary>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> EnsureDefaultUserExists()
        {

            //If no user exists create default one                
            if (_userManager.Users.Count() == 0)
            {
                RAppUser admin = new RAppUser()
                {
                    UserName = "admin@ganjoor.net",
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

                if (!await _roleManager.RoleExistsAsync(_userRoleService.AdministratorRoleName))
                {
                    identityResult = await _roleManager.CreateAsync(new RAppRole(_userRoleService.AdministratorRoleName));
                    if (!identityResult.Succeeded)
                    {
                        return new RServiceResult<bool>(false, "Error creating Administrator role : " + ErrorsToString(identityResult.Errors));
                    }
                }

                identityResult = await _userManager.AddToRoleAsync(admin, _userRoleService.AdministratorRoleName);
                if (!identityResult.Succeeded)
                {
                    return new RServiceResult<bool>(false, "Error adding admin to Administrator role : " + ErrorsToString(identityResult.Errors));
                }
            }
            return new RServiceResult<bool>(true);

        }





        /// <summary>
        /// secret used for generating Jwt token
        /// </summary>
        public string TokenSecret { get { return $"{Configuration.GetSection("Security")["Secret"]}"; } }

        /// <summary>
        /// JWT Tokens Expiration Time Out
        /// </summary>
        public int DefaultTokenExpirationInSeconds { get { return int.Parse($"{Configuration.GetSection("Security")["DefaultTokenExpirationInSeconds"]}"); } }



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
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
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
            foreach (IdentityError error in errors)
            {
                sb.AppendLine(error.Description);
            }
            return sb.ToString();
        }
        #endregion
        #endregion


        #region Signup/Forget password email related overridables
        /// <summary>
        /// Sign Up/forget passsword/delete Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        public virtual string GetEmailSubject(RVerifyQueueType op, string secretCode)
        {
            string opString = op == RVerifyQueueType.SignUp ? "SignUp" : op == RVerifyQueueType.ForgotPassword ? "Forgot Password" : "Self Delete User";
            return $"Ganjoor {opString} Code:{secretCode}";
        }

        /// <summary>
        /// Sign Up/forget passsword/delete Email Html Content
        /// </summary>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        /// <param name="signupCallbackUrl"></param>
        /// <returns>html content</returns>
        public virtual string GetEmailHtmlContent(RVerifyQueueType op, string secretCode, string signupCallbackUrl)
        {
            if (string.IsNullOrEmpty(signupCallbackUrl))
                return $"{signupCallbackUrl}?secret={secretCode}";
            string opString = op == RVerifyQueueType.SignUp ? "ثبت نام" : op == RVerifyQueueType.ForgotPassword ? "فراموشی رمز" : "حذف کاربر";
            return $"لطفا {secretCode} را در صفحهٔ {opString} وارد کنید.";
        }

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
