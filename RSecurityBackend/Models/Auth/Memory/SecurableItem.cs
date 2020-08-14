namespace RSecurityBackend.Models.Auth.Memory
{
    /// <summary>
    /// securable itmes: forms and ...
    /// </summary>
    public class SecurableItem
    {
        /// <summary>
        /// user
        /// </summary>
        public const string UserEntityShortName = "user";
        /// <summary>
        /// role
        /// </summary>
        public const string RoleEntityShortName = "role";

        /// <summary>
        /// audit
        /// </summary>
        public const string AuditLogEntityShortName = "audit";


        /// <summary>
        /// view
        /// </summary>
        public const string ViewOperationShortName = "view";       
        /// <summary>
        /// add
        /// </summary>
        public const string AddOperationShortName = "add";
        /// <summary>
        /// modify
        /// </summary>
        public const string ModifyOperationShortName = "modify";
        /// <summary>
        /// delete
        /// </summary>
        public const string DeleteOperationShortName = "delete";
        /// <summary>
        /// sessions
        /// </summary>
        public const string SessionsOperationShortName = "sessions";
        /// <summary>
        /// delothersession
        /// </summary>
        public const string DelOtherUserSessionOperationShortName = "delothersession";
        /// <summary>
        /// view all
        /// </summary>
        public const string ViewAllOperationShortName = "viewall";



        /// <summary>
        /// list of forms and their permissions
        /// </summary>
        public static SecurableItem[] Items
        {
            get
            {
                return new SecurableItem[]
                {
                    new SecurableItem()
                    {
                        ShortName = UserEntityShortName,
                        Description = "کاربران",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false, null ),                           
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false,
                            new SecurableItemOperationPrerequisite[]
                            {
                                new SecurableItemOperationPrerequisite(  RoleEntityShortName, ViewOperationShortName)
                            }
                           ),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false,
                            new SecurableItemOperationPrerequisite[]
                            {
                                new SecurableItemOperationPrerequisite(  RoleEntityShortName, ViewOperationShortName)
                            }                            
                            ),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false),
                            new SecurableItemOperation(SessionsOperationShortName, "مشاهده جلسات همه کاربران", false, null),
                            new SecurableItemOperation(DelOtherUserSessionOperationShortName, "حذف جلسه سایر کاربران", false),
                            new SecurableItemOperation(ViewAllOperationShortName, "مشاهده اطلاعات کاربران دیگر", false, null ),
                        }
                    },
                    new SecurableItem()
                    {
                        ShortName = RoleEntityShortName,
                        Description = "نقش‌ها",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false),
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false),
                        }
                    },
                    new SecurableItem()
                    {
                        ShortName = AuditLogEntityShortName,
                        Description = "رویدادها",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false),
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Short Name
        /// </summary>
        /// <example>
        /// user
        /// </example>
        public string ShortName { get; set; }

        /// <summary>
        /// Descripttion
        /// </summary>
        /// <example>
        /// کاربران
        /// </example>
        public string Description { get; set; }

        /// <summary>
        /// Operations (short name + description + has permission)
        /// </summary>
        /// <example>
        /// [
        ///     [view, مشاهده, true],
        ///     [add, ایجاد, false]
        /// ]
        /// </example>
        public SecurableItemOperation[] Operations { get; set; }


    }
}
