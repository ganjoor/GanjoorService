using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// User Roles Service Implementation
    /// </summary>
    public class RoleServiceBase : IUserRoleService
    {
        /// <summary>
        /// returns all user roles
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<RAppRole[]>> GetAllRoles()
        {
            try
            {
                RAppRole[] rolesInfo = await _roleManager.Roles.Include(r => r.Permissions).ToArrayAsync();
                return new RServiceResult<RAppRole[]>(rolesInfo);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RAppRole[]>(null, exp.ToString());
            }
        }

        
        /// <summary>
        /// returns user role information
        /// </summary>       
        /// <param name="roleName"></param>        
        /// <returns></returns>
        public async Task<RServiceResult<RAppRole>> GetRoleInformation(string roleName)
        {

            try
            {
                RAppRole dbUserRoleInfo =
                    await _roleManager.FindByNameAsync(roleName);
                return new RServiceResult<RAppRole>(dbUserRoleInfo);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RAppRole>(null, exp.ToString());

            }
        }


        /// <summary>
        /// modify existing user role
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="updateRoleInfo"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifyRole(string roleName, RAppRole updateRoleInfo)
        {
            try
            {
                RAppRole existingInfo = await _roleManager.FindByNameAsync(roleName);
                if (existingInfo == null)
                {
                    return new RServiceResult<bool>(false, "role not found");
                }

                if (existingInfo.Name != updateRoleInfo.Name)
                {

                    RAppRole anotherWithSameName = await _roleManager.Roles.Where(g => g.Name == updateRoleInfo.Name && g.Id != existingInfo.Id).SingleOrDefaultAsync();

                    if (anotherWithSameName != null)
                    {
                        return new RServiceResult<bool>(false, "duplicated role name");
                    }

                    existingInfo.Name = updateRoleInfo.Name;
                }


                existingInfo.Description = updateRoleInfo.Description;


                await _roleManager.UpdateAsync(existingInfo);

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete user role
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns>true if succeeds</returns>
        public async Task<RServiceResult<bool>> DeleteRole(string roleName)
        {
            try
            {
                RAppRole existingInfo = await _roleManager.FindByNameAsync(roleName); 
                if (existingInfo != null)
                {
                    await _roleManager.DeleteAsync(existingInfo);

                    return new RServiceResult<bool>(true);
                }
              
               
                return new RServiceResult<bool>(false, "role not found.");
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// adds a new user role
        /// </summary>
        /// <param name="newRoleInfo">new role info</param>
        /// <returns>update user role info (id)</returns>
        public async Task<RServiceResult<RAppRole>> AddRole(RAppRole newRoleInfo)
        {
            try
            {
                RAppRole existingRole = await _roleManager.Roles.Where(g => g.Name == newRoleInfo.Name).SingleOrDefaultAsync();

                if(existingRole != null)
                {
                    return new RServiceResult<RAppRole>(null, "Role name is in use");
                }

                await _roleManager.CreateAsync(newRoleInfo);
                return new RServiceResult<RAppRole>(newRoleInfo);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RAppRole>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Has role specified permission
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> HasPermission(string roleName, string securableItemShortName, string operationShortName)
        {
            try
            {
                RAppRole roleByName = await _roleManager.FindByNameAsync(roleName);
                if (roleByName == null)
                {
                 
                    return new RServiceResult<bool>(false, "role not found");
                }

                RAppRole role = await _roleManager.Roles.Include(g => g.Permissions)
                    .Where(g => g.Id == roleByName.Id)
                    .SingleOrDefaultAsync();

                return
                    new RServiceResult<bool>(
                    role.Permissions.Where(p => p.SecurableItemShortName == securableItemShortName && p.OperationShortName == operationShortName)
                    .SingleOrDefault() != null
                    );
                    
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// roles having specific permission
        /// </summary>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RAppRole[]>> GetRolesHavingPermission(string securableItemShortName, string operationShortName)
        {
            try
            {
                RAppRole[] rolesInfo = await _roleManager.Roles
                                                            .Include(r => r.Permissions)
                                                            .Where(r => r.Permissions.Any(p => p.SecurableItemShortName == securableItemShortName && p.OperationShortName == operationShortName) )
                                                            .ToArrayAsync();
                return new RServiceResult<RAppRole[]>(rolesInfo);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RAppRole[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// gets list of SecurableItem, should be reimplemented in end user applications
        /// </summary>
        /// <returns></returns>
        public virtual SecurableItem[] GetSecurableItems()
        {
            return SecurableItem.Items;
        }

        /// <summary>
        /// Lists role permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<SecurableItem[]>> GetRoleSecurableItemsStatus(string roleName)
        {
            try
            {
                RAppRole roleByName = await _roleManager.FindByNameAsync(roleName);
                if (roleByName == null)
                {

                    return new RServiceResult<SecurableItem[]>(null, "role not found");
                }
                RAppRole role = await _roleManager.Roles.Include(g => g.Permissions).Where(g => g.Id == roleByName.Id).SingleOrDefaultAsync();
                List<SecurableItem> securableItems = new List<SecurableItem>();
                foreach(SecurableItem templateItem in GetSecurableItems())
                {
                    SecurableItem item = new SecurableItem()
                    {
                        ShortName = templateItem.ShortName,
                        Description = templateItem.Description
                    };

                    List<SecurableItemOperation> operations = new List<SecurableItemOperation>();

                    foreach(SecurableItemOperation operation in templateItem.Operations)
                    {
                        operations.Add(
                            new SecurableItemOperation()
                            {
                                ShortName = operation.ShortName,
                                Description = operation.Description,
                                Prerequisites = operation.Prerequisites,
                                Status = role.Permissions.Where(p => p.SecurableItemShortName == templateItem.ShortName && p.OperationShortName == operation.ShortName).SingleOrDefault() != null
                            }
                            );
                    }

                    item.Operations = operations.ToArray();                    

                    securableItems.Add(item);
                }

                return new RServiceResult<SecurableItem[]>(securableItems.ToArray());

            }
            catch (Exception exp)
            {
                return new RServiceResult<SecurableItem[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Saves role permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="securableItems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetRoleSecurableItemsStatus(string roleName, SecurableItem[] securableItems)
        {
            try
            {
                RAppRole roleByName = await _roleManager.FindByNameAsync(roleName);
                if (roleByName == null)
                {

                    return new RServiceResult<bool>(false, "role not found");
                }

                RAppRole role = await _roleManager.Roles.Include(g => g.Permissions).Where(g => g.Id == roleByName.Id).SingleOrDefaultAsync();

                role.Permissions.Clear();
                await _roleManager.UpdateAsync(role);

                List<RPermission> newPermissionSet = new List<RPermission>();
                foreach(SecurableItem securableItem in securableItems)
                {
                    foreach (SecurableItemOperation operation in securableItem.Operations)
                    {
                        if(operation.Status)
                        {
                            newPermissionSet.Add(new RPermission()
                            {
                                SecurableItemShortName = securableItem.ShortName,
                                OperationShortName = operation.ShortName
                            });
                        }                       
                    }
                }

                role.Permissions = newPermissionSet;
                await _roleManager.UpdateAsync(role);

                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// Identity Role Manager
        /// </summary>
        protected RoleManager<RAppRole> _roleManager = null;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleManager"></param>       
        public RoleServiceBase(
            RoleManager<RAppRole> roleManager
            )
        {
            _roleManager = roleManager;
        }
    }
}
