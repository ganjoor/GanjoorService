using RSecurityBackend.Models.Auth.Memory;
using System.Collections.Generic;

namespace RMuseum.Models.Auth.Memory
{
    /// <summary>
    /// specific forms and permission
    /// </summary>
    public class RMuseumSecurableItem : SecurableItem
    {
        /// <summary>
        /// artifact
        /// </summary>
        public const string ArtifactEntityShortName = "artifact";

        /// <summary>
        /// tag
        /// </summary>
        public const string TagEntityShortName = "tag";

        /// <summary>
        /// note
        /// </summary>
        public const string NoteEntityShortName = "note";

        ///<summary>
        /// view drafts
        /// </summary>
        public const string ViewDraftOperationShortName = "viewdraft";

        ///<summary>
        /// edittag
        /// </summary>
        public const string EditTagValueOperationShortName = "edittag";

        ///<summary>
        /// awaiting
        /// </summary>
        public const string ToAwaitingStatusOperationShortName = "awaiting";

        ///<summary>
        /// publish
        /// </summary>
        public const string PublishOperationShortName = "publish";

        ///<summary>
        /// import
        /// </summary>
        public const string ImportOperationShortName = "import";

        ///<summary>
        /// moderate
        /// </summary>
        public const string ModerateCommentsOperationShortName = "moderate";

        ///<summary>
        /// review suggested ganjoor links
        /// </summary>
        public const string ReviewGanjoorLinksOperationShortName = "ganjoor";

        /// <summary>
        /// list of forms and their permissions
        /// </summary>
        public new static SecurableItem[] Items
        {
            get
            {
                List<SecurableItem> lst = new List<SecurableItem>(SecurableItem.Items);
                lst.AddRange(
                new SecurableItem[]
                {
                    new SecurableItem()
                    {
                        ShortName = ArtifactEntityShortName,
                        Description = "اشیاء گنجینه",
                        Operations = new SecurableItemOperation[]
                        {                            
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false),
                            new SecurableItemOperation(ImportOperationShortName, "ورود اطلاعات", false),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false),
                            new SecurableItemOperation(ViewDraftOperationShortName, "مشاهدهٔ پیش‌نویس‌ها", false),
                            new SecurableItemOperation(EditTagValueOperationShortName, "اصلاح مقدار ویژگی", false),
                            new SecurableItemOperation(ToAwaitingStatusOperationShortName, "درخواست بازبینی", false),
                            new SecurableItemOperation(PublishOperationShortName, "انتشار", false),
                            new SecurableItemOperation(ReviewGanjoorLinksOperationShortName, "بررسی شعرهای پیشنهادی گنجور", false),
                        }
                    },
                    new SecurableItem()
                    {
                        ShortName = TagEntityShortName,
                        Description = "انواع ویژگیها",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false)
                        }
                    },
                    new SecurableItem()
                    {
                        ShortName = NoteEntityShortName,
                        Description = "یادداشتها",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(ModerateCommentsOperationShortName, "بررسی", false)
                        }
                    },

                });
                return lst.ToArray();
            }
        }
    }
}
