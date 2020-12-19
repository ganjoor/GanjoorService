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
        public const string ModerateOperationShortName = "moderate";

        ///<summary>
        /// review suggested ganjoor links
        /// </summary>
        public const string ReviewGanjoorLinksOperationShortName = "ganjoor";

        /// <summary>
        /// audio narrations
        /// </summary>
        public const string AudioRecitationEntityShortName = "recitation";

        /// <summary>
        /// ganjoor contents
        /// </summary>
        public const string GanjoorEntityShortName = "ganjoor";

        ///<summary>
        /// reorder
        /// </summary>
        public const string ReOrderOperationShortName = "reorder";

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
                            new SecurableItemOperation(ImportOperationShortName, "ورود اطلاعات از منابع خارجی", false),
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
                            new SecurableItemOperation(ModerateOperationShortName, "بررسی", false)
                        }
                    },
                    new SecurableItem()
                    {
                        ShortName = AudioRecitationEntityShortName,
                        Description = "خوانشها",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(PublishOperationShortName, "انتشار خوانش خود", false),
                            new SecurableItemOperation(ModerateOperationShortName, "بررسی خوانش کاربران دیگر", false),
                            new SecurableItemOperation(ReOrderOperationShortName, "تغییر ترتیب خوانش‌ها", false),
                            new SecurableItemOperation(ImportOperationShortName, "ورود اطلاعات از منابع خارجی", false),
                        }
                    },
                    new SecurableItem()
                    {
                        ShortName = GanjoorEntityShortName,
                        Description = "محتوای گنجور",
                        Operations = new SecurableItemOperation[]
                        {
                            new SecurableItemOperation(ImportOperationShortName, "ورود اطلاعات از منابع خارجی", false),
                        }
                    },

                });
                return lst.ToArray();
            }
        }
    }
}
