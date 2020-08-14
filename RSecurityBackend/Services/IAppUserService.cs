using Microsoft.AspNetCore.Http;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Authentication Service
    /// </summary>
    public interface IAppUserService
    {

        /// <summary>
        /// Login user, if failed return LoggedOnUserModel is null
        /// </summary>
        /// <param name="loginViewModel"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        Task<RServiceResult<LoggedOnUserModel>> Login(LoginViewModel loginViewModel, string clientIPAddress);

        /// <summary>
        /// Logout
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> Logout(Guid userId, Guid sessionId);


        /// <summary>
        /// Does Session exist?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SessionExists(Guid userId, Guid sessionId);

        /// <summary>
        /// is user admin?
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsAdmin(Guid userId);

        /// <summary>
        /// is user in either of passed roles?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleNames"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsInRoles(Guid userId, string[] roleNames);

        /// <summary>
        /// Get User Roles
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<IList<string>>> GetUserRoles(Guid userId);


        /// <summary>
        /// remove user from role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RemoveFromRole(Guid id, string role);

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddToRole(Guid id, string role);

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid userId);

        /// <summary>
        /// Has user specified permission
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> HasPermission(Guid userId, string securableItemShortName, string operationShortName);

        /// <summary>
        /// returns user information
        /// </summary>
        /// <remarks>
        /// PasswordHash becomes empty
        /// </remarks>
        /// <param name="userId"></param>        
        /// <returns></returns>
        Task<RServiceResult<PublicRAppUser>> GetUserInformation(Guid userId);


        /// <summary>
        /// all users informations
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<PublicRAppUser[]>> GetAllUsersInformation();

        /// <summary>
        /// add a new user
        /// </summary>
        /// <param name="newUserInfo"></param>
        /// <returns></returns>
        Task<RServiceResult<RAppUser>> AddUser(RegisterRAppUser newUserInfo);

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddUserToRole(Guid userId, string roleName);

        /// <summary>
        /// modify existing user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="updateUserInfo"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifyUser(Guid userId, RegisterRAppUser updateUserInfo);

        /// <summary>
        /// change user password checking old password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ChangePassword(Guid userId, string oldPassword, string newPassword);

        /// <summary>
        /// delete user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>true if succeeds</returns>
        Task<RServiceResult<bool>> DeleteUser(Guid userId);

        /// <summary>
        /// Set User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="files"></param>
        /// <returns>new user image id</returns>
        Task<RServiceResult<Guid?>> SetUserImage(Guid userId, IFormFileCollection files);


        /// <summary>
        /// Get User Sessions
        /// </summary>
        /// <param name="userId">if null is passed returns all sessions</param>
        /// <returns></returns>
        Task<RServiceResult<PublicRUserSession[]>> GetUserSessions(Guid? userId);

        /// <summary>
        /// Get User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RImage>> GetUserImage(Guid userId);

         /// <summary>
        /// Start signup process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        Task<RServiceResult<RVerifyQueueItem>> SignUp(string email, string clientIPAddress, string clientAppName, string langauge);

        /// <summary>
        /// verify signup / forgot password
        /// </summary>
        /// <param name="verifyQueueType"></param>
        /// <param name="secret"></param>
        /// <returns>associated email</returns>
        Task<RServiceResult<string>> RetrieveEmailFromQueueSecret(RVerifyQueueType verifyQueueType,  string secret);


        /// <summary>
        /// finalize signup process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="sureName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> FinalizeSignUp(string email, string secret, string password, string firstName, string sureName);

        /// <summary>
        /// Start forgot password process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        Task<RServiceResult<RVerifyQueueItem>> ForgotPassword(string email, string clientIPAddress, string clientAppName, string langauge);

        /// <summary>
        /// reset password using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="clientIPAddress"></param>       
        /// <returns></returns>
        Task<RServiceResult<bool>> ResetPassword(string email, string secret, string password, string clientIPAddress);

        /// <summary>
        /// delete tenant
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> DeleteTenant();


        /// <summary>
        /// Administrator role name
        /// </summary>
        string AdministratorRoleName { get; }  

        /// <summary>
        /// secret used for generating Jwt token
        /// </summary>
        string TokenSecret { get; }

        /// <summary>
        /// JWT Tokens Expiration Time Out
        /// </summary>
        int DefaultTokenExpirationInSeconds { get; }


    }
}
